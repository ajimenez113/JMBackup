using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Application.Tests.TestDoubles;

/// <summary>
/// Backend en memoria para probar <c>BackupEngine</c> y <c>FileScanner</c> sin tocar
/// disco ni red (CLAUDE.md §7). No es un mock generado: es una implementación real y
/// pequeña de <see cref="IStorageBackend"/>, más clara que encadenar configuraciones de
/// NSubstitute para un escenario con varias llamadas relacionadas entre sí.
/// </summary>
public sealed class InMemoryStorageBackend(TimeProvider timeProvider) : IStorageBackend
{
    private readonly Dictionary<string, InMemoryFile> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Queue<Exception>> _writeFailures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _stallingPaths = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected { get; set; } = true;

    public long? FreeSpace { get; set; } = long.MaxValue;

    public IReadOnlyDictionary<string, InMemoryFile> Files => _files;

    public List<string> CreateDirectoryCalls { get; } = [];

    public void AddFile(string path, byte[] content, DateTimeOffset modifiedUtc) =>
        _files[Normalize(path)] = new InMemoryFile(content, modifiedUtc);

    public void EnqueueWriteFailure(string path, Exception exception)
    {
        var key = Normalize(path);
        if (!_writeFailures.TryGetValue(key, out var queue))
        {
            queue = new Queue<Exception>();
            _writeFailures[key] = queue;
        }

        queue.Enqueue(exception);
    }

    /// <summary>Toda escritura a esta ruta reporta progreso que nunca avanza, hasta que se cancele.</summary>
    public void SimulateStallOn(string path) => _stallingPaths.Add(Normalize(path));

    /// <summary>Deja de simular estancamiento en esta ruta: la próxima escritura se completa con normalidad.</summary>
    public void ClearStall(string path) => _stallingPaths.Remove(Normalize(path));

    public Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken) => Task.FromResult(
        IsConnected ? ConnectionStatus.Connected() : ConnectionStatus.Failed(StorageErrorReason.HostUnreachable, "Desconectado (prueba)."));

    public async IAsyncEnumerable<FileEntry> ListAsync(string path, bool recursive, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var prefix = Normalize(path);
        var prefixWithSlash = prefix.Length == 0 ? string.Empty : prefix + "/";
        var yieldedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in _files.Keys.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!filePath.StartsWith(prefixWithSlash, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = filePath[prefixWithSlash.Length..];
            var slashIndex = relative.IndexOf('/', StringComparison.Ordinal);
            var file = _files[filePath];

            if (slashIndex < 0)
            {
                yield return new FileEntry(relative, false, file.Content.Length, file.ModifiedUtc);
                continue;
            }

            if (!recursive)
            {
                var directoryName = relative[..slashIndex];
                if (yieldedDirectories.Add(directoryName))
                {
                    yield return new FileEntry(directoryName, true, 0, DateTimeOffset.MinValue);
                }

                continue;
            }

            yield return new FileEntry(relative, false, file.Content.Length, file.ModifiedUtc);
        }

        await Task.CompletedTask;
    }

    public Task<FileEntry?> StatAsync(string path, CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(Normalize(path), out var file))
        {
            return Task.FromResult<FileEntry?>(new FileEntry(path, false, file.Content.Length, file.ModifiedUtc));
        }

        return Task.FromResult<FileEntry?>(null);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!_files.TryGetValue(Normalize(path), out var file))
        {
            throw new StorageOperationException(StorageErrorReason.PathNotFound, path, $"No se encontró \"{path}\" (prueba).");
        }

        return Task.FromResult<Stream>(new MemoryStream(file.Content, writable: false));
    }

    public async Task WriteAsync(
        string path, Stream content, long size, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        var key = Normalize(path);

        if (_writeFailures.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            throw queue.Dequeue();
        }

        if (_stallingPaths.Contains(key))
        {
            progress.Report(new TransferProgress(path, 1, size, 0, null));
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(new TransferProgress(path, 1, size, 0, null));
                await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
            }
        }

        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        progress.Report(new TransferProgress(path, memoryStream.Length, size, 0, null));

        _files[key] = new InMemoryFile(memoryStream.ToArray(), timeProvider.GetUtcNow());
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        _files.Remove(Normalize(path));
        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        CreateDirectoryCalls.Add(path);
        return Task.CompletedTask;
    }

    public Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        var sourceKey = Normalize(sourcePath);
        if (_files.TryGetValue(sourceKey, out var file))
        {
            _files.Remove(sourceKey);
            _files[Normalize(destinationPath)] = file;
        }

        return Task.CompletedTask;
    }

    public Task<long?> GetFreeSpaceAsync(CancellationToken cancellationToken) => Task.FromResult(FreeSpace);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string Normalize(string path) => path.Trim('/');
}

public sealed record InMemoryFile(byte[] Content, DateTimeOffset ModifiedUtc);
