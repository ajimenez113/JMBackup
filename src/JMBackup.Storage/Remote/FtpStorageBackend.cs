using System.Runtime.CompilerServices;
using FluentFTP;
using FluentFTP.Exceptions;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Storage.Remote;

/// <summary>
/// FTP y FTPS explícito (fase 5, hito 2), con FluentFTP. Una instancia es una sesión
/// persistente atada a una raíz (ADR-014): se conecta en el primer uso y reutiliza la
/// misma conexión para todas las operaciones — nunca una sesión nueva por archivo.
///
/// Reanuda transferencias parciales con <c>REST</c> puertas adentro, validando
/// identidad del origen antes de reanudar (ADR-027): un archivo <c>.jmtmp</c> a medio
/// subir solo se retoma si el tamaño y la fecha de modificación del origen coinciden
/// con los que tenía cuando se creó ese parcial; si no, se descarta y arranca de cero.
/// </summary>
public sealed class FtpStorageBackend : IStorageBackend
{
    private const string TemporaryFileSuffix = ".jmtmp";

    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _encrypted;
    private readonly string _root;
    private readonly IReadOnlyCollection<string> _asciiExtensions;
    private readonly TimeProvider _timeProvider;

    // Vive mientras viva esta instancia (un run completo, ADR-014): alcanza para
    // reanudar entre pasadas de reintento de la misma ejecución (ADR-027), que es la
    // misma garantía que ya tiene LocalStorageBackend con su .jmtmp.
    private readonly Dictionary<string, (long Size, DateTimeOffset SourceModifiedUtc)> _partialUploads =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _confirmedDirectories = new(StringComparer.OrdinalIgnoreCase);

    private AsyncFtpClient? _client;

    public FtpStorageBackend(
        string host,
        int port,
        string remoteBasePath,
        string username,
        string password,
        bool encrypted,
        IReadOnlyCollection<string> asciiExtensions,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentNullException.ThrowIfNull(remoteBasePath);
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(asciiExtensions);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _encrypted = encrypted;
        _root = "/" + remoteBasePath.Trim('/');
        _asciiExtensions = asciiExtensions;
        _timeProvider = timeProvider;
    }

    public async Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
            var exists = await client.DirectoryExists(_root, cancellationToken).ConfigureAwait(false);
            return exists
                ? ConnectionStatus.Connected()
                : ConnectionStatus.Failed(StorageErrorReason.PathNotFound, $"La ruta \"{_root}\" no existe en el servidor.");
        }
        catch (FtpAuthenticationException)
        {
            return ConnectionStatus.Failed(StorageErrorReason.InvalidCredentials, "Usuario o contraseña incorrectos.");
        }
        catch (FtpInvalidCertificateException)
        {
            return ConnectionStatus.Failed(
                StorageErrorReason.UntrustedCertificate, "El certificado TLS del servidor no es de confianza.");
        }
        catch (FtpSecurityNotAvailableException)
        {
            return ConnectionStatus.Failed(
                StorageErrorReason.UntrustedCertificate, "El servidor no admite el cifrado FTPS pedido.");
        }
        catch (FtpCommandException ex) when (IsPermissionDenied(ex))
        {
            return ConnectionStatus.Failed(StorageErrorReason.PermissionDenied, $"Permiso denegado al acceder a \"{_root}\".");
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            return ConnectionStatus.Failed(StorageErrorReason.HostUnreachable, $"No se pudo conectar a \"{_host}:{_port}\".");
        }
    }

    public async IAsyncEnumerable<FileEntry> ListAsync(string path, bool recursive, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var remotePath = CombineRemote(path);
        var options = recursive ? FtpListOption.Recursive : FtpListOption.Auto;

        FtpListItem[] items;
        try
        {
            items = await client.GetListing(remotePath, options, cancellationToken).ConfigureAwait(false);
        }
        catch (FtpCommandException)
        {
            yield break;
        }

        foreach (var item in items)
        {
            if (item.Name.EndsWith(TemporaryFileSuffix, StringComparison.OrdinalIgnoreCase))
            {
                // Huérfano de una corrida cancelada (ver LocalStorageBackend): se
                // ignora, es como si no existiera.
                continue;
            }

            var relative = GetRelativeTo(remotePath, item.FullName);

            // Ojo acá: la hora de un listado FTP no siempre es UTC — depende de si el
            // servidor responde vía MLSD (RFC 3659 exige UTC) o vía LIST clásico
            // (server-local, sin zona horaria confiable). FtpConfig.ServerTimeZone
            // existe para calibrar esto contra un servidor real; no hay forma de
            // verificarlo sin uno.
            yield return new FileEntry(
                relative, item.Type == FtpObjectType.Directory, item.Size, new DateTimeOffset(DateTime.SpecifyKind(item.Modified, DateTimeKind.Utc)));
        }
    }

    public async Task<FileEntry?> StatAsync(string path, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var remotePath = CombineRemote(path);

        // GetFileSize (SIZE) y GetModifiedTime (MDTM) en vez de GetObjectInfo: ese
        // último exige que el servidor soporte listados MLST, algo que muchos
        // servidores FTP más viejos no tienen — SIZE/MDTM son universales.
        var size = await client.GetFileSize(remotePath, -1, cancellationToken).ConfigureAwait(false);
        if (size < 0)
        {
            var isDirectory = await client.DirectoryExists(remotePath, cancellationToken).ConfigureAwait(false);
            return isDirectory ? new FileEntry(path, IsDirectory: true, 0, DateTimeOffset.MinValue) : null;
        }

        var modified = await client.GetModifiedTime(remotePath, cancellationToken).ConfigureAwait(false);

        // MDTM (RFC 3959) siempre responde en UTC.
        return new FileEntry(path, IsDirectory: false, size, new DateTimeOffset(DateTime.SpecifyKind(modified, DateTimeKind.Utc)));
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var remotePath = CombineRemote(path);

        try
        {
            return await client
                .OpenRead(remotePath, GetDataType(path), restart: 0, checkIfFileExists: true, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (FtpMissingObjectException ex)
        {
            throw new StorageOperationException(StorageErrorReason.PathNotFound, path, $"No se encontró \"{path}\" en el servidor.", ex);
        }
        catch (FtpCommandException ex) when (IsPermissionDenied(ex))
        {
            throw new StorageOperationException(StorageErrorReason.PermissionDenied, path, $"Permiso denegado al leer \"{path}\".", ex);
        }
    }

    public async Task WriteAsync(
        string path, Stream content, long size, DateTimeOffset sourceModifiedUtc, IProgress<TransferProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(progress);

        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var remotePath = CombineRemote(path);
        var tempRemotePath = remotePath + TemporaryFileSuffix;

        var existingPartialSize = await client.GetFileSize(tempRemotePath, -1, cancellationToken).ConfigureAwait(false);
        PartialUpload? existingPartial = null;
        if (existingPartialSize >= 0 && _partialUploads.TryGetValue(path, out var recorded))
        {
            existingPartial = new PartialUpload(recorded.Size, recorded.SourceModifiedUtc, existingPartialSize);
        }

        var resumeFromBytes = 0L;
        if (ResumeDecision.ShouldResume(existingPartial, size, sourceModifiedUtc))
        {
            resumeFromBytes = existingPartial!.BytesTransferred;
            progress.Report(new TransferProgress(path, resumeFromBytes, size, BytesPerSecond: 0, EstimatedTimeRemaining: null));
        }
        else
        {
            if (existingPartialSize >= 0)
            {
                await client.DeleteFile(tempRemotePath, cancellationToken).ConfigureAwait(false);
            }

            _partialUploads[path] = (size, sourceModifiedUtc);
        }

        try
        {
            client.Config.UploadDataType = GetDataType(path);
            var startedAt = _timeProvider.GetTimestamp();
            var ftpProgress = new Progress<FtpProgress>(reported => ReportUploadProgress(reported, path, size, resumeFromBytes, startedAt, progress));

            // Resume (no AddToEnd): AddToEnd simplemente apila el stream local entero al
            // final del remoto, sin descontar lo ya subido — probado contra un servidor
            // real, duplicaba los bytes ya transferidos. Resume sí calcula el tamaño
            // remoto y salta esa cantidad en el stream local, que es lo que
            // corresponde: ya validamos la identidad del origen con ResumeDecision antes
            // de llegar acá, así que confiar en que Resume salte hasta el tamaño remoto
            // (idéntico a resumeFromBytes, que viene de esa misma consulta) es seguro.
            var existsMode = resumeFromBytes > 0 ? FtpRemoteExists.Resume : FtpRemoteExists.Overwrite;
            var status = await client
                .UploadStream(content, tempRemotePath, existsMode, createRemoteDir: false, ftpProgress, cancellationToken)
                .ConfigureAwait(false);

            if (status == FtpStatus.Failed)
            {
                throw new StorageOperationException(StorageErrorReason.Unknown, path, $"No se pudo escribir \"{path}\" en el servidor.");
            }

            await client.MoveFile(tempRemotePath, remotePath, FtpRemoteExists.Overwrite, cancellationToken).ConfigureAwait(false);
            _partialUploads.Remove(path);
        }
        catch (FtpCommandException ex) when (IsPermissionDenied(ex))
        {
            ResetConnection();
            throw new StorageOperationException(StorageErrorReason.PermissionDenied, path, $"Permiso denegado al escribir \"{path}\".", ex);
        }
        catch
        {
            // Una subida que se corta a mitad de camino (la fuente falla, no el
            // servidor) puede dejar el canal de control desincronizado: el servidor
            // queda con una respuesta pendiente que nunca se leyó. Confirmado contra
            // un servidor real (fauria/vsftpd): reusar la misma conexión para el
            // siguiente intento rompía el parseo de la respuesta EPSV del intento
            // posterior ("Failed to get the EPSV port from: Delete operation
            // successful"). Se fuerza reconexión en el próximo uso en vez de confiar
            // en que IsConnected (solo verifica el socket, no el estado del protocolo)
            // detecte esto.
            ResetConnection();
            throw;
        }
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var remotePath = CombineRemote(path);

        if (await client.FileExists(remotePath, cancellationToken).ConfigureAwait(false))
        {
            await client.DeleteFile(remotePath, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (await client.DirectoryExists(remotePath, cancellationToken).ConfigureAwait(false))
        {
            await client.DeleteDirectory(remotePath, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        var remotePath = CombineRemote(path);
        if (!_confirmedDirectories.Add(remotePath))
        {
            // Ya se confirmó/creó en esta sesión: evita un MKD por archivo (FTP no
            // tiene listado recursivo barato ni operaciones gratis — cada una es un
            // viaje de red, así que esto importa de verdad).
            return;
        }

        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        await client.CreateDirectory(remotePath, force: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(false);
        var remoteSource = CombineRemote(sourcePath);
        var remoteDestination = CombineRemote(destinationPath);

        var parent = GetParentPath(remoteDestination);
        if (!string.IsNullOrEmpty(parent) && _confirmedDirectories.Add(parent))
        {
            await client.CreateDirectory(parent, force: true, cancellationToken).ConfigureAwait(false);
        }

        if (await client.FileExists(remoteSource, cancellationToken).ConfigureAwait(false))
        {
            await client.MoveFile(remoteSource, remoteDestination, FtpRemoteExists.Overwrite, cancellationToken).ConfigureAwait(false);
        }
        else if (await client.DirectoryExists(remoteSource, cancellationToken).ConfigureAwait(false))
        {
            await client.MoveDirectory(remoteSource, remoteDestination, FtpRemoteExists.Overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>FTP no tiene forma estándar de consultar espacio libre — igual que S3 (ver IStorageBackend).</summary>
    public Task<long?> GetFreeSpaceAsync(CancellationToken cancellationToken) => Task.FromResult<long?>(null);

    public async ValueTask DisposeAsync()
    {
        if (_client is null)
        {
            return;
        }

        await _client.Disconnect(CancellationToken.None).ConfigureAwait(false);
        _client.Dispose();
    }

    private async Task<AsyncFtpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_client is { IsConnected: true })
        {
            return _client;
        }

        _client?.Dispose();

        var config = new FtpConfig
        {
            EncryptionMode = _encrypted ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None,

            // Nunca se acepta cualquier certificado en silencio: dejar esto en su
            // default (false) es lo que hace que un certificado inválido tire
            // FtpInvalidCertificateException en vez de conectar igual.
            ValidateAnyCertificate = false,
            ValidateCertificateRevocation = true,
        };

        var client = new AsyncFtpClient(_host, _username, _password, _port, config);
        await client.Connect(cancellationToken).ConfigureAwait(false);
        _client = client;
        return client;
    }

    /// <summary>
    /// Fuerza una reconexión en el próximo uso: IsConnected solo confirma que el
    /// socket sigue abierto, no que el canal de control esté en un estado consistente
    /// para el próximo comando.
    /// </summary>
    private void ResetConnection()
    {
        _client?.Dispose();
        _client = null;
    }

    private void ReportUploadProgress(
        FtpProgress reported, string path, long totalSize, long resumeFromBytes, long startedAtTimestamp, IProgress<TransferProgress> progress)
    {
        var totalTransferred = resumeFromBytes + reported.TransferredBytes;
        var elapsed = _timeProvider.GetElapsedTime(startedAtTimestamp);
        var bytesPerSecond = elapsed.TotalSeconds > 0 ? reported.TransferredBytes / elapsed.TotalSeconds : 0;
        var remainingBytes = totalSize - totalTransferred;
        TimeSpan? estimatedTimeRemaining = bytesPerSecond > 0 ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond) : null;

        progress.Report(new TransferProgress(path, totalTransferred, totalSize, bytesPerSecond, estimatedTimeRemaining));
    }

    private FtpDataType GetDataType(string path)
    {
        var extension = Path.GetExtension(path).TrimStart('.');
        return _asciiExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ? FtpDataType.ASCII : FtpDataType.Binary;
    }

    private string CombineRemote(string relativePath) =>
        string.IsNullOrEmpty(relativePath) ? _root : $"{_root.TrimEnd('/')}/{relativePath}";

    private static string GetRelativeTo(string queriedRemotePath, string fullRemotePath)
    {
        var prefix = queriedRemotePath.TrimEnd('/') + "/";
        return fullRemotePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? fullRemotePath[prefix.Length..]
            : fullRemotePath.TrimStart('/');
    }

    private static string? GetParentPath(string remotePath)
    {
        var lastSlash = remotePath.LastIndexOf('/');
        return lastSlash <= 0 ? null : remotePath[..lastSlash];
    }

    /// <summary>
    /// 550 es la respuesta genérica de FTP para "no se puede hacer": DirectoryExists y
    /// GetObjectInfo ya la interpretan como "no existe" sin tirar excepción, así que
    /// una FtpCommandException que sí llega hasta acá, en una operación de
    /// lectura/escritura sobre una ruta cuya existencia ya se asumía, es permiso
    /// denegado con más probabilidad que ruta inexistente.
    /// </summary>
    private static bool IsPermissionDenied(FtpCommandException ex) =>
        ex.CompletionCode is "550" or "553" or "530";

    private static bool IsConnectivityException(Exception ex) =>
        ex is System.Net.Sockets.SocketException or TimeoutException or FtpMissingSocketException or IOException;
}
