using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using JMBackup.Storage.Remote;

namespace JMBackup.Storage.Tests.Remote;

/// <summary>
/// Contra un servidor FTP real en contenedor, no un mock — FluentFTP tiene demasiadas
/// particularidades de protocolo (modo pasivo, respuestas de servidor, REST) como para
/// confiar en que un doble de prueba las reproduzca fielmente.
///
/// El modo pasivo de FTP dentro de Docker es notoriamente delicado: el servidor tiene
/// que anunciar una dirección alcanzable desde fuera del contenedor para el rango de
/// puertos pasivos. Esta clase fija ese rango vía variables de entorno de la imagen
/// (`fauria/vsftpd`) y lo publica con Testcontainers; si el entorno donde se corre
/// difiere (otra plataforma de contenedores, una red distinta), puede hacer falta
/// ajustar <see cref="PassiveAddress"/> o el rango de puertos.
///
/// Etiquetada "Docker" y excluida del "dotnet test" normal (ver README): sin Docker
/// corriendo, Testcontainers no falla rápido al intentar levantar el contenedor — se
/// queda esperando indefinidamente, colgando toda la corrida en vez de solo esta
/// clase. Correr explícitamente con "dotnet test --filter Category=Docker".
/// </summary>
[Trait("Category", "Docker")]
public sealed class FtpStorageBackendTests : IAsyncLifetime
{
    private const int ControlPort = 21;
    private const int PassiveMinPort = 21100;
    private const int PassiveMaxPort = 21110;
    private const string Username = "jmbackup";

    // ASCII puro a propósito: una contraseña con "ñ" llegaba corrompida al servidor
    // real (confirmado con curl en crudo, no era un bug de FluentFTP ni de
    // FtpStorageBackend) — la codificación de una variable de entorno pasada a un
    // contenedor Docker no está garantizada a preservar caracteres fuera de ASCII.
    private const string Password = "una-contrasena-de-prueba-123";

    // Necesario para que el servidor anuncie una dirección alcanzable desde fuera del
    // contenedor en modo pasivo. host.docker.internal funciona en Docker Desktop
    // (Windows/Mac); en Docker sobre Linux nativo puede hacer falta la IP real del
    // host en su lugar.
    private const string PassiveAddress = "host.docker.internal";

    private IContainer _container = null!;
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public async Task InitializeAsync()
    {
        var builder = new ContainerBuilder("fauria/vsftpd:latest")
            .WithEnvironment("FTP_USER", Username)
            .WithEnvironment("FTP_PASS", Password)
            .WithEnvironment("PASV_ADDRESS", PassiveAddress)
            .WithEnvironment("PASV_MIN_PORT", PassiveMinPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithEnvironment("PASV_MAX_PORT", PassiveMaxPort.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .WithPortBinding(ControlPort, ControlPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(ControlPort));

        // Cada puerto pasivo se publica 1:1 (mismo número adentro y afuera del
        // contenedor): el servidor anuncia estos números exactos vía PASV_ADDRESS, así
        // que un mapeo a un puerto de host distinto rompería el modo pasivo.
        for (var port = PassiveMinPort; port <= PassiveMaxPort; port++)
        {
            builder = builder.WithPortBinding(port, port);
        }

        _container = builder.Build();
        await _container.StartAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private FtpStorageBackend CreateBackend(string remoteBasePath = "", string? password = null) =>
        new("127.0.0.1", ControlPort, remoteBasePath, Username, password ?? Password, encrypted: false, [], _timeProvider);

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsConnected()
    {
        await using var backend = CreateBackend();

        var status = await backend.TestConnectionAsync(CancellationToken.None);

        status.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidPassword_ReturnsInvalidCredentials()
    {
        await using var backend = CreateBackend(password: "contrasena-incorrecta");

        var status = await backend.TestConnectionAsync(CancellationToken.None);

        status.IsConnected.Should().BeFalse();
        status.Reason.Should().Be(StorageErrorReason.InvalidCredentials);
    }

    [Fact]
    public async Task WriteAsync_ThenOpenReadAsync_RoundTripsTheContent()
    {
        await using var backend = CreateBackend();
        var content = "contenido de prueba"u8.ToArray();

        await WriteAsync(backend, "archivo.txt", content);

        await using var readStream = await backend.OpenReadAsync("archivo.txt", CancellationToken.None);
        using var reader = new StreamReader(readStream, Encoding.UTF8);
        var readBack = await reader.ReadToEndAsync();

        readBack.Should().Be(Encoding.UTF8.GetString(content));
    }

    [Fact]
    public async Task StatAsync_AfterWrite_ReturnsTheCorrectSize()
    {
        await using var backend = CreateBackend();
        var content = new byte[12345];

        await WriteAsync(backend, "tamaño.bin", content);
        var entry = await backend.StatAsync("tamaño.bin", CancellationToken.None);

        entry.Should().NotBeNull();
        entry!.Size.Should().Be(content.Length);
    }

    [Fact]
    public async Task StatAsync_NonExistentFile_ReturnsNull()
    {
        await using var backend = CreateBackend();

        var entry = await backend.StatAsync("no-existe.txt", CancellationToken.None);

        entry.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_ReturnsFilesWrittenToTheRoot()
    {
        await using var backend = CreateBackend();

        // FluentFTP no crea directorios padre al subir (createRemoteDir: false):
        // en producción es BackupEngine quien llama CreateDirectoryAsync antes de
        // escribir cada archivo, así que la prueba tiene que hacer lo mismo.
        await backend.CreateDirectoryAsync("listado", CancellationToken.None);
        await WriteAsync(backend, "listado/uno.txt", "1"u8.ToArray());
        await WriteAsync(backend, "listado/dos.txt", "2"u8.ToArray());

        var entries = new List<string>();
        await foreach (var entry in backend.ListAsync("listado", recursive: false, CancellationToken.None))
        {
            entries.Add(entry.Path);
        }

        entries.Should().BeEquivalentTo("uno.txt", "dos.txt");
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFile()
    {
        await using var backend = CreateBackend();
        await WriteAsync(backend, "borrar.txt", "x"u8.ToArray());

        await backend.DeleteAsync("borrar.txt", CancellationToken.None);
        var entry = await backend.StatAsync("borrar.txt", CancellationToken.None);

        entry.Should().BeNull();
    }

    /// <summary>
    /// Reproduce la reanudación real (ADR-027): la primera pasada corta la conexión a
    /// mitad de la subida (dejando un .jmtmp parcial en el servidor), y una segunda
    /// pasada con el mismo backend —misma identidad de origen— tiene que completar el
    /// archivo sin volver a mandar los bytes ya subidos. Se verifica por resultado
    /// (el archivo final es correcto) más que por bytes exactos retransmitidos, que
    /// FluentFTP no expone directamente.
    ///
    /// Contra un servidor real, FluentFTP intenta su propia reanudación interna ante
    /// un IOException de la fuente (confirmado corriendo esta prueba con Docker) y
    /// solo después de agotar esos reintentos internos devuelve FtpStatus.Failed, que
    /// FtpStorageBackend traduce en StorageOperationException — nunca ve la
    /// IOException cruda porque FluentFTP la absorbe puertas adentro.
    /// </summary>
    [Fact]
    public async Task WriteAsync_InterruptedThenRetried_ResumesAndProducesTheCompleteFile()
    {
        await using var backend = CreateBackend();
        var content = new byte[500_000];
        new Random(42).NextBytes(content);
        var modifiedUtc = _timeProvider.GetUtcNow();

        using (var interruptingStream = new InterruptingStream(content, failAfterBytes: 200_000))
        {
            var act = async () => await backend.WriteAsync(
                "grande.bin", interruptingStream, content.Length, modifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);
            await act.Should().ThrowAsync<StorageOperationException>();
        }

        using var fullStream = new MemoryStream(content);
        await backend.WriteAsync("grande.bin", fullStream, content.Length, modifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);

        var entry = await backend.StatAsync("grande.bin", CancellationToken.None);
        entry.Should().NotBeNull();
        entry!.Size.Should().Be(content.Length);
    }

    /// <summary>
    /// El caso de seguridad (ADR-027): si el origen cambió entre el intento que dejó
    /// el parcial y este, no se reanuda — se descarta y arranca de cero. Se prueba acá
    /// además de en <c>ResumeDecisionTests</c> porque acá se ejercita el camino real
    /// contra un servidor, no solo la función de decisión aislada.
    ///
    /// Igual que en <see cref="WriteAsync_InterruptedThenRetried_ResumesAndProducesTheCompleteFile"/>,
    /// FluentFTP absorbe la IOException de la fuente con sus propios reintentos
    /// internos y el fallo que finalmente ve FtpStorageBackend es un FtpStatus.Failed
    /// traducido a StorageOperationException.
    /// </summary>
    [Fact]
    public async Task WriteAsync_SourceChangedAfterAPartialUpload_DoesNotResumeAndWritesTheNewContentCompletely()
    {
        await using var backend = CreateBackend();
        var firstContent = new byte[300_000];
        new Random(1).NextBytes(firstContent);
        var firstModifiedUtc = _timeProvider.GetUtcNow();

        using (var interruptingStream = new InterruptingStream(firstContent, failAfterBytes: 100_000))
        {
            var act = async () => await backend.WriteAsync(
                "cambia.bin", interruptingStream, firstContent.Length, firstModifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);
            await act.Should().ThrowAsync<StorageOperationException>();
        }

        var secondContent = new byte[50_000];
        new Random(2).NextBytes(secondContent);
        var secondModifiedUtc = firstModifiedUtc.AddMinutes(10);

        using var secondStream = new MemoryStream(secondContent);
        await backend.WriteAsync(
            "cambia.bin", secondStream, secondContent.Length, secondModifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);

        var entry = await backend.StatAsync("cambia.bin", CancellationToken.None);
        entry.Should().NotBeNull();
        entry!.Size.Should().Be(secondContent.Length);
    }

    private static async Task WriteAsync(FtpStorageBackend backend, string path, byte[] content)
    {
        using var stream = new MemoryStream(content);
        await backend.WriteAsync(path, stream, content.Length, DateTimeOffset.UtcNow, new Progress<TransferProgress>(), CancellationToken.None);
    }

    /// <summary>Tira una IOException a mitad de lectura, para simular una conexión que se corta durante la subida.</summary>
    private sealed class InterruptingStream(byte[] content, int failAfterBytes) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => content.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_inner.Position >= failAfterBytes)
            {
                throw new IOException("Conexión interrumpida (simulada).");
            }

            var allowed = (int)Math.Min(count, failAfterBytes - _inner.Position);
            return _inner.Read(buffer, offset, allowed);
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
