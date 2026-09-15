using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using JMBackup.Storage.Remote;
using Testcontainers.LocalStack;

namespace JMBackup.Storage.Tests.Remote;

/// <summary>
/// Contra un LocalStack real en contenedor, no un mock — igual que
/// <see cref="FtpStorageBackendTests"/>, por la misma razón: hay demasiado detalle real
/// del protocolo (paginación, multipart, clases de almacenamiento) como para confiar en
/// que un doble de prueba lo reproduzca fielmente.
///
/// Etiquetada "Docker" y excluida del "dotnet test" normal (ver README). Correr
/// explícitamente con "dotnet test --filter Category=Docker".
/// </summary>
[Trait("Category", "Docker")]
public sealed class S3StorageBackendTests : IAsyncLifetime
{
    private const string BucketName = "jmbackup-test-bucket";
    // LocalStack solo reconoce "cuentas" para las credenciales de prueba documentadas
    // oficialmente ("test"/"test"); un access key id con forma distinta se rechaza con
    // "The AWS Access Key Id you provided does not exist in our records" (confirmado
    // corriendo esta prueba contra un contenedor real).
    private const string AccessKeyId = "test";
    private const string SecretAccessKey = "test";
    private const string Region = "us-east-1";

    private LocalStackContainer _container = null!;
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public async Task InitializeAsync()
    {
        // Pinneado a la última versión "community" antes de que LocalStack exigiera
        // LOCALSTACK_AUTH_TOKEN (desde la 4.15, confirmado corriendo esta prueba real):
        // ":latest" hoy resuelve a una versión que requiere una cuenta/token pago, algo
        // que no corresponde pedirle al usuario solo para correr pruebas locales.
        _container = new LocalStackBuilder("localstack/localstack:3.8.1")
            .WithEnvironment("SERVICES", "s3")
            .Build();
        await _container.StartAsync();

        // El bucket se crea una sola vez acá, con un cliente directo del SDK — el
        // propio backend bajo prueba no crea buckets (no es su responsabilidad; un
        // bucket es un recurso que el operador provisiona de antemano, como una
        // carpeta de destino ya tiene que existir para FTP).
        using var setupClient = CreateRawClient();
        await setupClient.PutBucketAsync(new PutBucketRequest { BucketName = BucketName });
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    // Sin RegionEndpoint acá junto con ServiceURL: confirmado contra un servidor real,
    // esa combinación hace que el SDK v4 ignore ServiceURL para us-east-1 (su endpoint
    // "global" legado) y mande la petición a AWS real en vez de al contenedor.
    private AmazonS3Client CreateRawClient() => new(
        AccessKeyId, SecretAccessKey,
        new AmazonS3Config { ServiceURL = _container.GetConnectionString(), ForcePathStyle = true, AuthenticationRegion = Region });

    private S3StorageBackend CreateBackend(string basePrefix = "", string? storageClass = null, bool serverSideEncryption = false) =>
        new(BucketName, basePrefix, AccessKeyId, SecretAccessKey, Region, storageClass, serverSideEncryption, _timeProvider,
            _container.GetConnectionString(), forcePathStyle: true);

    [Fact]
    public async Task TestConnectionAsync_ValidBucket_ReturnsConnected()
    {
        await using var backend = CreateBackend();

        var status = await backend.TestConnectionAsync(CancellationToken.None);

        status.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_BucketDoesNotExist_ReturnsPathNotFound()
    {
        await using var backend = new S3StorageBackend(
            "bucket-que-no-existe", string.Empty, AccessKeyId, SecretAccessKey, Region, null, false, _timeProvider,
            _container.GetConnectionString(), forcePathStyle: true);

        var status = await backend.TestConnectionAsync(CancellationToken.None);

        status.IsConnected.Should().BeFalse();
        status.Reason.Should().Be(StorageErrorReason.PathNotFound);
    }

    [Fact]
    public async Task WriteAsync_ThenOpenReadAsync_RoundTripsSmallContent()
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
    public async Task DeleteAsync_RemovesTheObject()
    {
        await using var backend = CreateBackend();
        await WriteAsync(backend, "borrar.txt", "x"u8.ToArray());

        await backend.DeleteAsync("borrar.txt", CancellationToken.None);
        var entry = await backend.StatAsync("borrar.txt", CancellationToken.None);

        entry.Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_PathWithWindowsBackslashes_NormalizesToForwardSlashKeys()
    {
        // El pedido explícito: S3 acepta "\" como carácter literal en una clave. Si
        // esto se colara sin normalizar, "carpeta\archivo.txt" crearía una clave
        // literal con una barra invertida adentro del nombre, no un archivo dentro de
        // "carpeta/" — que después ListAsync/StatAsync con rutas "/" nunca encontraría.
        await using var backend = CreateBackend();

        await WriteAsync(backend, @"carpeta\archivo.txt", "contenido"u8.ToArray());

        var viaForwardSlash = await backend.StatAsync("carpeta/archivo.txt", CancellationToken.None);
        viaForwardSlash.Should().NotBeNull("la ruta con \"\\\" tiene que haberse guardado bajo la clave con \"/\"");

        using var rawClient = CreateRawClient();
        var listing = await rawClient.ListObjectsV2Async(new ListObjectsV2Request { BucketName = BucketName, Prefix = "carpeta" });
        listing.S3Objects.Should().OnlyContain(
            s3Object => !s3Object.Key.Contains('\\'), "ninguna clave real en el bucket debe contener una barra invertida literal");
    }

    [Fact]
    public async Task ListAsync_MoreThan1000Objects_EnumeratesAllOfThem()
    {
        // ListObjectsV2 devuelve máximo 1000 objetos por llamada. Si el backend no
        // sigue el ContinuationToken hasta agotarlo, esta prueba ve solo una página y
        // falla — es exactamente el fallo silencioso que se pidió cubrir: un respaldo
        // con más de 1000 archivos reportaría éxito habiendo ignorado el resto.
        const int totalObjects = 1005;
        await using var backend = CreateBackend("muchos-objetos");

        using var rawClient = CreateRawClient();
        for (var i = 0; i < totalObjects; i++)
        {
            await rawClient.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = $"muchos-objetos/archivo-{i:D5}.txt",
                ContentBody = "x",
            });
        }

        var seen = new HashSet<string>();
        await foreach (var entry in backend.ListAsync(string.Empty, recursive: false, CancellationToken.None))
        {
            seen.Add(entry.Path);
        }

        seen.Should().HaveCount(totalObjects);
    }

    [Fact]
    public async Task OpenReadAsync_ObjectInGlacierNotRestored_ThrowsWithObjectArchivedReason()
    {
        await using var backend = CreateBackend();

        using var rawClient = CreateRawClient();
        await rawClient.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = "archivado.bin",
            ContentBody = "contenido archivado",
            StorageClass = S3StorageClass.Glacier,
        });

        var act = async () => await backend.OpenReadAsync("archivado.bin", CancellationToken.None);

        var exception = await act.Should().ThrowAsync<StorageOperationException>();
        exception.Which.Reason.Should().Be(StorageErrorReason.ObjectArchived);
    }

    /// <summary>
    /// Reproduce la reanudación real (ADR-027/ADR-031): la primera pasada corta la
    /// conexión a mitad de un multipart, dejando un UploadId en curso, y una segunda
    /// pasada con el mismo backend —misma identidad de origen— tiene que completarlo
    /// sin volver a subir las partes ya confirmadas.
    /// </summary>
    [Fact]
    public async Task WriteAsync_InterruptedMultipartThenRetried_ResumesAndProducesTheCompleteFile()
    {
        await using var backend = CreateBackend();
        var content = new byte[20 * 1024 * 1024]; // por encima del umbral de multipart
        new Random(42).NextBytes(content);
        var modifiedUtc = _timeProvider.GetUtcNow();

        using (var interruptingStream = new InterruptingStream(content, failAfterBytes: 9 * 1024 * 1024))
        {
            var act = async () => await backend.WriteAsync(
                "grande.bin", interruptingStream, content.Length, modifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);
            await act.Should().ThrowAsync<IOException>();
        }

        using var fullStream = new MemoryStream(content);
        await backend.WriteAsync("grande.bin", fullStream, content.Length, modifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);

        var entry = await backend.StatAsync("grande.bin", CancellationToken.None);
        entry.Should().NotBeNull();
        entry!.Size.Should().Be(content.Length);
    }

    /// <summary>
    /// El caso de seguridad (ADR-027): si el origen cambió entre el intento que dejó el
    /// multipart abierto y este, no se reanuda — se aborta el multipart huérfano
    /// explícitamente (se verifica contra ListMultipartUploadsAsync, no solo por
    /// resultado) y arranca de cero.
    /// </summary>
    [Fact]
    public async Task WriteAsync_SourceChangedAfterAPartialMultipart_AbortsTheOrphanedUploadAndWritesFromScratch()
    {
        await using var backend = CreateBackend();
        var firstContent = new byte[15 * 1024 * 1024];
        new Random(1).NextBytes(firstContent);
        var firstModifiedUtc = _timeProvider.GetUtcNow();

        using (var interruptingStream = new InterruptingStream(firstContent, failAfterBytes: 9 * 1024 * 1024))
        {
            var act = async () => await backend.WriteAsync(
                "cambia.bin", interruptingStream, firstContent.Length, firstModifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);
            await act.Should().ThrowAsync<IOException>();
        }

        using var rawClient = CreateRawClient();
        var pendingBefore = await rawClient.ListMultipartUploadsAsync(new ListMultipartUploadsRequest { BucketName = BucketName, Prefix = "cambia.bin" });
        pendingBefore.MultipartUploads.Should().HaveCount(1, "el intento cortado tiene que haber dejado un multipart en curso");

        var secondContent = new byte[9 * 1024 * 1024];
        new Random(2).NextBytes(secondContent);
        var secondModifiedUtc = firstModifiedUtc.AddMinutes(10);

        using var secondStream = new MemoryStream(secondContent);
        await backend.WriteAsync("cambia.bin", secondStream, secondContent.Length, secondModifiedUtc, new Progress<TransferProgress>(), CancellationToken.None);

        // El SDK deja MultipartUploads en null (no en una lista vacía) cuando no hay
        // ningún multipart pendiente — el mismo comportamiento que S3Objects/Parts.
        var pendingAfter = await rawClient.ListMultipartUploadsAsync(new ListMultipartUploadsRequest { BucketName = BucketName, Prefix = "cambia.bin" });
        (pendingAfter.MultipartUploads ?? []).Should().BeEmpty("el multipart huérfano tiene que haberse abortado explícitamente, no quedar colgado");

        var entry = await backend.StatAsync("cambia.bin", CancellationToken.None);
        entry.Should().NotBeNull();
        entry!.Size.Should().Be(secondContent.Length);
    }

    private static async Task WriteAsync(S3StorageBackend backend, string path, byte[] content)
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

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromResult(Read(buffer, offset, count));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var array = new byte[buffer.Length];
            var read = Read(array, 0, array.Length);
            array.AsMemory(0, read).CopyTo(buffer);
            return ValueTask.FromResult(read);
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
