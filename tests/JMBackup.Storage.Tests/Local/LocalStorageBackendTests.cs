using FluentAssertions;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using JMBackup.Storage.Local;

namespace JMBackup.Storage.Tests.Local;

public sealed class LocalStorageBackendTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("jmbackup-tests-");
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public void Dispose()
    {
        try
        {
            _tempRoot.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Un handle todavía abierto en el proceso de pruebas no debe romper la limpieza.
        }
    }

    [Fact]
    public async Task WriteAsync_WritesTheFileAtomically_NoTemporaryFileLeftBehind()
    {
        var backend = CreateBackend();
        var content = "hola mundo"u8.ToArray();

        await WriteAsync(backend, "archivo.txt", content);

        File.Exists(Path.Combine(_tempRoot.FullName, "archivo.txt")).Should().BeTrue();
        File.Exists(Path.Combine(_tempRoot.FullName, "archivo.txt.jmtmp")).Should().BeFalse();
        (await File.ReadAllBytesAsync(Path.Combine(_tempRoot.FullName, "archivo.txt"))).Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task ListAsync_IgnoresOrphanedTemporaryFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempRoot.FullName, "huerfano.txt.jmtmp"), "incompleto");
        var backend = CreateBackend();

        var entries = await CollectAsync(backend, string.Empty, true);

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_Recursive_ReturnsNestedFiles()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot.FullName, "sub"));
        await File.WriteAllTextAsync(Path.Combine(_tempRoot.FullName, "raiz.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot.FullName, "sub", "anidado.txt"), "b");

        var backend = CreateBackend();
        var entries = await CollectAsync(backend, string.Empty, true);

        entries.Select(e => e.Path).Should().Contain(["raiz.txt", "sub/anidado.txt"]);
    }

    [Fact]
    public async Task ListAsync_NonRecursive_OnlyReturnsTopLevelEntries()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot.FullName, "sub"));
        await File.WriteAllTextAsync(Path.Combine(_tempRoot.FullName, "raiz.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(_tempRoot.FullName, "sub", "anidado.txt"), "b");

        var backend = CreateBackend();
        var entries = await CollectAsync(backend, string.Empty, false);

        entries.Select(e => e.Path).Should().BeEquivalentTo(["raiz.txt", "sub"]);
    }

    [Fact]
    public async Task StatAsync_ExistingFile_ReturnsSizeAndDate()
    {
        var filePath = Path.Combine(_tempRoot.FullName, "archivo.txt");
        await File.WriteAllTextAsync(filePath, "contenido");
        var backend = CreateBackend();

        var entry = await backend.StatAsync("archivo.txt", CancellationToken.None);

        entry.Should().NotBeNull();
        entry!.Size.Should().Be(new FileInfo(filePath).Length);
        entry.IsDirectory.Should().BeFalse();
    }

    [Fact]
    public async Task StatAsync_MissingPath_ReturnsNull()
    {
        var backend = CreateBackend();

        var entry = await backend.StatAsync("no-existe.txt", CancellationToken.None);

        entry.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheFile()
    {
        var filePath = Path.Combine(_tempRoot.FullName, "archivo.txt");
        await File.WriteAllTextAsync(filePath, "contenido");
        var backend = CreateBackend();

        await backend.DeleteAsync("archivo.txt", CancellationToken.None);

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task CreateDirectoryAsync_CreatesNestedDirectories()
    {
        var backend = CreateBackend();

        await backend.CreateDirectoryAsync("a/b/c", CancellationToken.None);

        Directory.Exists(Path.Combine(_tempRoot.FullName, "a", "b", "c")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_MovesAFileToANewLocation()
    {
        var sourcePath = Path.Combine(_tempRoot.FullName, "origen.txt");
        await File.WriteAllTextAsync(sourcePath, "contenido");
        var backend = CreateBackend();

        await backend.MoveAsync("origen.txt", "papelera/origen.txt", CancellationToken.None);

        File.Exists(sourcePath).Should().BeFalse();
        File.Exists(Path.Combine(_tempRoot.FullName, "papelera", "origen.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task GetFreeSpaceAsync_ReturnsAPositiveNumber()
    {
        var backend = CreateBackend();

        var freeSpace = await backend.GetFreeSpaceAsync(CancellationToken.None);

        freeSpace.Should().NotBeNull();
        freeSpace!.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TestConnectionAsync_ExistingRoot_IsConnected()
    {
        var backend = CreateBackend();

        var status = await backend.TestConnectionAsync(CancellationToken.None);

        status.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_MissingRoot_ReportsPathNotFound()
    {
        var backend = new LocalStorageBackend(Path.Combine(_tempRoot.FullName, "no-existe"), _timeProvider);

        var status = await backend.TestConnectionAsync(CancellationToken.None);

        status.IsConnected.Should().BeFalse();
        status.Reason.Should().Be(StorageErrorReason.PathNotFound);
    }

    [Fact]
    public async Task OpenReadAsync_FileLockedByAnotherHandle_ThrowsWithFileLockedReason()
    {
        var filePath = Path.Combine(_tempRoot.FullName, "bloqueado.txt");
        await File.WriteAllTextAsync(filePath, "contenido");
        var backend = CreateBackend();

        using var exclusiveHandle = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var act = () => backend.OpenReadAsync("bloqueado.txt", CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<StorageOperationException>();
        assertion.Which.Reason.Should().Be(StorageErrorReason.FileLocked);
    }

    [Fact]
    public async Task ListAsync_MissingDirectory_ReturnsNoEntries()
    {
        var backend = CreateBackend();

        var entries = await CollectAsync(backend, "no-existe", true);

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task StatAsync_ExistingDirectory_ReturnsADirectoryEntry()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot.FullName, "sub"));
        var backend = CreateBackend();

        var entry = await backend.StatAsync("sub", CancellationToken.None);

        entry.Should().NotBeNull();
        entry!.IsDirectory.Should().BeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_ExistingFile_ReturnsItsContent()
    {
        var filePath = Path.Combine(_tempRoot.FullName, "archivo.txt");
        await File.WriteAllTextAsync(filePath, "contenido");
        var backend = CreateBackend();

        await using var stream = await backend.OpenReadAsync("archivo.txt", CancellationToken.None);
        using var reader = new StreamReader(stream);

        (await reader.ReadToEndAsync()).Should().Be("contenido");
    }

    [Fact]
    public async Task OpenReadAsync_MissingFile_ThrowsWithPathNotFoundReason()
    {
        var backend = CreateBackend();

        var act = () => backend.OpenReadAsync("no-existe.txt", CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<StorageOperationException>();
        assertion.Which.Reason.Should().Be(StorageErrorReason.PathNotFound);
    }

    [Fact]
    public async Task OpenReadAsync_MissingParentDirectory_ThrowsWithPathNotFoundReason()
    {
        var backend = CreateBackend();

        var act = () => backend.OpenReadAsync("carpeta-inexistente/archivo.txt", CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<StorageOperationException>();
        assertion.Which.Reason.Should().Be(StorageErrorReason.PathNotFound);
    }

    [Fact]
    public async Task WriteAsync_CancelledMidCopy_DeletesTheOrphanedTemporaryFile()
    {
        var backend = CreateBackend();
        using var cts = new CancellationTokenSource();
        var content = new ThrowingStream(cts);

        var act = () => backend.WriteAsync("archivo.txt", content, 100, new Progress<TransferProgress>(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        File.Exists(Path.Combine(_tempRoot.FullName, "archivo.txt.jmtmp")).Should().BeFalse();
        File.Exists(Path.Combine(_tempRoot.FullName, "archivo.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesAnEmptyDirectory()
    {
        var directoryPath = Path.Combine(_tempRoot.FullName, "vacia");
        Directory.CreateDirectory(directoryPath);
        var backend = CreateBackend();

        await backend.DeleteAsync("vacia", CancellationToken.None);

        Directory.Exists(directoryPath).Should().BeFalse();
    }

    [Fact]
    public async Task MoveAsync_MovesADirectory()
    {
        var sourcePath = Path.Combine(_tempRoot.FullName, "origen");
        Directory.CreateDirectory(sourcePath);
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "archivo.txt"), "contenido");
        var backend = CreateBackend();

        await backend.MoveAsync("origen", "destino", CancellationToken.None);

        Directory.Exists(sourcePath).Should().BeFalse();
        File.Exists(Path.Combine(_tempRoot.FullName, "destino", "archivo.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task GetFreeSpaceAsync_InvalidRoot_ReturnsNull()
    {
        var backend = new LocalStorageBackend("no es una ruta valida::::", _timeProvider);

        var freeSpace = await backend.GetFreeSpaceAsync(CancellationToken.None);

        freeSpace.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_CompletesWithoutError()
    {
        var backend = CreateBackend();

        var act = () => backend.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LongPath_MoreThan260Characters_IsSupported()
    {
        var backend = CreateBackend();
        var longSegment = new string('a', 50);
        var relativePath = string.Join('/', Enumerable.Repeat(longSegment, 6)) + "/archivo.txt";
        var content = "contenido de ruta larga"u8.ToArray();

        await WriteAsync(backend, relativePath, content);

        var entry = await backend.StatAsync(relativePath, CancellationToken.None);

        entry.Should().NotBeNull();
        entry!.Size.Should().Be(content.Length);
    }

    private LocalStorageBackend CreateBackend() => new(_tempRoot.FullName, _timeProvider);

    private static async Task WriteAsync(LocalStorageBackend backend, string path, byte[] content)
    {
        using var stream = new MemoryStream(content);
        await backend.WriteAsync(path, stream, content.Length, new Progress<TransferProgress>(), CancellationToken.None);
    }

    private static async Task<List<FileEntry>> CollectAsync(LocalStorageBackend backend, string path, bool recursive)
    {
        var results = new List<FileEntry>();
        await foreach (var entry in backend.ListAsync(path, recursive, CancellationToken.None))
        {
            results.Add(entry);
        }

        return results;
    }

    /// <summary>Cancela el token apenas se le pide el primer bloque, para probar la limpieza del temporal en <c>WriteAsync</c>.</summary>
    private sealed class ThrowingStream(CancellationTokenSource cancellationTokenSource) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await cancellationTokenSource.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
