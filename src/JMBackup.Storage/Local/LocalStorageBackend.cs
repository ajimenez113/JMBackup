using System.Runtime.CompilerServices;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Storage.Local;

/// <summary>
/// Implementación de <see cref="IStorageBackend"/> para disco local y recursos UNC
/// (<c>\\SERVIDOR\recurso</c>). Cada instancia está atada a una raíz fija, pasada al
/// construirla (ver <see cref="IStorageBackend"/>). Soporta rutas de más de 260
/// caracteres a través del manifiesto <c>longPathAware</c> del proceso host, combinado
/// con <c>LongPathsEnabled</c> a nivel de máquina (lo activa el script de instalación
/// de la fase 2) — no hace falta anteponer <c>\\?\</c> a mano en .NET moderno.
/// </summary>
public sealed class LocalStorageBackend : IStorageBackend
{
    private const string TemporaryFileExtension = ".jmtmp";
    private const int CopyBufferSize = 81920;

    // ERROR_SHARING_VIOLATION y ERROR_LOCK_VIOLATION de Win32, tal como los reporta IOException.HResult.
    private const int ErrorSharingViolation = unchecked((int)0x80070020);
    private const int ErrorLockViolation = unchecked((int)0x80070021);

    private readonly string _root;
    private readonly TimeProvider _timeProvider;

    public LocalStorageBackend(string root, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _root = root.Replace('/', '\\').TrimEnd('\\');
        _timeProvider = timeProvider;
    }

    public Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (Directory.Exists(_root) || File.Exists(_root))
            {
                return Task.FromResult(ConnectionStatus.Connected());
            }

            return Task.FromResult(ConnectionStatus.Failed(StorageErrorReason.PathNotFound, $"La ruta \"{_root}\" no existe."));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(ConnectionStatus.Failed(StorageErrorReason.PermissionDenied, $"Permiso denegado al acceder a \"{_root}\"."));
        }
        catch (IOException ex)
        {
            return Task.FromResult(ConnectionStatus.Failed(StorageErrorReason.HostUnreachable, ex.Message));
        }
    }

    public async IAsyncEnumerable<FileEntry> ListAsync(string path, bool recursive, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var native = LocalPathTranslator.ToNative(_root, path);
        if (!Directory.Exists(native))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(native);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = pending.Pop();

            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(currentDirectory);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
            {
                continue;
            }

            foreach (var entryPath in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entryPath.EndsWith(TemporaryFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // Temporal de escritura atómica huérfano de una corrida cancelada: se
                    // ignora, es como si no existiera (ver RF-163, reanudación).
                    continue;
                }

                var relative = LocalPathTranslator.GetRelativeNormalized(native, entryPath);

                if (Directory.Exists(entryPath))
                {
                    yield return new FileEntry(relative, true, 0, DateTimeOffset.MinValue);
                    if (recursive)
                    {
                        pending.Push(entryPath);
                    }

                    continue;
                }

                FileInfo info;
                try
                {
                    info = new FileInfo(entryPath);
                }
                catch (IOException)
                {
                    continue;
                }

                yield return new FileEntry(relative, false, info.Length, info.LastWriteTimeUtc);
            }
        }
    }

    public Task<FileEntry?> StatAsync(string path, CancellationToken cancellationToken)
    {
        var native = LocalPathTranslator.ToNative(_root, path);

        if (File.Exists(native))
        {
            var info = new FileInfo(native);
            return Task.FromResult<FileEntry?>(new FileEntry(path, false, info.Length, info.LastWriteTimeUtc));
        }

        if (Directory.Exists(native))
        {
            return Task.FromResult<FileEntry?>(new FileEntry(path, true, 0, DateTimeOffset.MinValue));
        }

        return Task.FromResult<FileEntry?>(null);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        var native = LocalPathTranslator.ToNative(_root, path);

        try
        {
            Stream stream = new FileStream(native, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, useAsync: true);
            return Task.FromResult(stream);
        }
        catch (IOException ex) when (IsLockedFileException(ex))
        {
            throw new StorageOperationException(
                StorageErrorReason.FileLocked, path, $"El archivo \"{path}\" está en uso por otro proceso.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new StorageOperationException(StorageErrorReason.PermissionDenied, path, $"Permiso denegado al leer \"{path}\".", ex);
        }
        catch (FileNotFoundException ex)
        {
            throw new StorageOperationException(StorageErrorReason.PathNotFound, path, $"No se encontró \"{path}\".", ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new StorageOperationException(StorageErrorReason.PathNotFound, path, $"No se encontró \"{path}\".", ex);
        }
    }

    // sourceModifiedUtc es para reanudación (ADR-027): este backend siempre trunca
    // con el truco .jmtmp de más abajo, nunca reanuda un parcial, así que no la usa.
    public async Task WriteAsync(
        string path, Stream content, long size, DateTimeOffset sourceModifiedUtc, IProgress<TransferProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(progress);

        var native = LocalPathTranslator.ToNative(_root, path);
        var directory = Path.GetDirectoryName(native);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempNative = native + TemporaryFileExtension;

        try
        {
            try
            {
                var destinationStream = new FileStream(tempNative, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
                await using (destinationStream.ConfigureAwait(false))
                {
                    await CopyWithProgressAsync(content, destinationStream, path, size, progress, cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempNative, native, overwrite: true);
            }
            catch
            {
                TryDeleteTempFile(tempNative);
                throw;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new StorageOperationException(StorageErrorReason.PermissionDenied, path, $"Permiso denegado al escribir \"{path}\".", ex);
        }
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var native = LocalPathTranslator.ToNative(_root, path);

        if (File.Exists(native))
        {
            File.Delete(native);
        }
        else if (Directory.Exists(native))
        {
            Directory.Delete(native, recursive: false);
        }

        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(LocalPathTranslator.ToNative(_root, path));
        return Task.CompletedTask;
    }

    public Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        var nativeSource = LocalPathTranslator.ToNative(_root, sourcePath);
        var nativeDestination = LocalPathTranslator.ToNative(_root, destinationPath);

        var directory = Path.GetDirectoryName(nativeDestination);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(nativeSource))
        {
            File.Move(nativeSource, nativeDestination, overwrite: true);
        }
        else if (Directory.Exists(nativeSource))
        {
            Directory.Move(nativeSource, nativeDestination);
        }

        return Task.CompletedTask;
    }

    public Task<long?> GetFreeSpaceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var drive = new DriveInfo(_root);
            return Task.FromResult<long?>(drive.AvailableFreeSpace);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return Task.FromResult<long?>(null);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task CopyWithProgressAsync(
        Stream source, Stream destination, string path, long totalBytes, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        var buffer = new byte[CopyBufferSize];
        long totalRead = 0;
        var startedAt = _timeProvider.GetTimestamp();

        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalRead += bytesRead;

            var elapsed = _timeProvider.GetElapsedTime(startedAt);
            var bytesPerSecond = elapsed.TotalSeconds > 0 ? totalRead / elapsed.TotalSeconds : 0;
            var remainingBytes = totalBytes - totalRead;
            TimeSpan? estimatedTimeRemaining = bytesPerSecond > 0
                ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond)
                : null;

            progress.Report(new TransferProgress(path, totalRead, totalBytes, bytesPerSecond, estimatedTimeRemaining));
        }
    }

    private static void TryDeleteTempFile(string tempNative)
    {
        try
        {
            File.Delete(tempNative);
        }
        catch (IOException)
        {
            // Se prioriza propagar la excepción original de la copia.
        }
    }

    private static bool IsLockedFileException(IOException ex) =>
        ex.HResult is ErrorSharingViolation or ErrorLockViolation;
}
