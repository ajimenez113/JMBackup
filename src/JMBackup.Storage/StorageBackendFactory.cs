using JMBackup.Application.Abstractions;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using JMBackup.Storage.Local;
using JMBackup.Storage.Remote;

namespace JMBackup.Storage;

/// <summary>
/// Construye el <see cref="IStorageBackend"/> correcto según <see cref="TaskPath.BackendType"/>
/// (ADR-006/ADR-014). Local no necesita credencial (ADR-027: la conexión UNC se
/// resuelve a nivel de sistema operativo, antes de llegar acá); los backends remotos
/// sí, y la reciben ya resuelta de la base — acá se descifra recién en el último
/// momento, no antes.
/// </summary>
public sealed class StorageBackendFactory(TimeProvider timeProvider, INetworkCredentialProtector credentialProtector) : IStorageBackendFactory
{
    // TODO(fase 5): reemplazar por la lista configurable de RF-92 (pestaña
    // Transferencia) cuando esa pestaña exista. Por ahora, extensiones de texto
    // comunes que casi siempre conviene transferir en modo ASCII por FTP.
    private static readonly string[] DefaultAsciiExtensions = ["txt", "htm", "html", "xml", "csv", "ini", "log", "json", "md"];

    public IStorageBackend Create(TaskPath path, Credential? credential)
    {
        return path.BackendType switch
        {
            BackendType.Local => new LocalStorageBackend(path.Path, timeProvider),
            BackendType.Ftp => CreateFtp(path, credential),
            BackendType.S3 => CreateS3(path, credential),
            _ => throw new NotSupportedException($"El backend {path.BackendType} todavía no está implementado."),
        };
    }

    private FtpStorageBackend CreateFtp(TaskPath path, Credential? credential)
    {
        if (credential is null)
        {
            throw new InvalidOperationException("Una ruta FTP necesita una credencial configurada.");
        }

        var (host, port, remoteBasePath) = ParseFtpPath(path.Path);
        var password = credentialProtector.Unprotect(credential.EncryptedSecret);

        return new FtpStorageBackend(
            host, port, remoteBasePath, credential.Username ?? string.Empty, password, path.Encrypted, DefaultAsciiExtensions, timeProvider);
    }

    private S3StorageBackend CreateS3(TaskPath path, Credential? credential)
    {
        if (credential is null)
        {
            throw new InvalidOperationException("Una ruta S3 necesita una credencial configurada.");
        }

        if (string.IsNullOrWhiteSpace(path.Region))
        {
            throw new InvalidOperationException("Una ruta S3 necesita una región configurada.");
        }

        var (bucket, prefix) = ParseS3Path(path.Path);
        var accessKeyId = credential.Username ?? throw new InvalidOperationException("La credencial de S3 necesita el Access Key ID como usuario.");
        var secretAccessKey = credentialProtector.Unprotect(credential.EncryptedSecret);

        return new S3StorageBackend(
            bucket, prefix, accessKeyId, secretAccessKey, path.Region, path.StorageClass, path.ServerSideEncryption, timeProvider);
    }

    /// <summary>"bucket/prefijo/opcional" → ("bucket", "prefijo/opcional"). El prefijo es opcional.</summary>
    private static (string Bucket, string Prefix) ParseS3Path(string path)
    {
        var trimmed = path.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        return firstSlash < 0 ? (trimmed, string.Empty) : (trimmed[..firstSlash], trimmed[(firstSlash + 1)..]);
    }

    /// <summary>"host:puerto/ruta/remota" → (host, puerto, "ruta/remota"). El puerto es opcional (21 por defecto).</summary>
    private static (string Host, int Port, string RemoteBasePath) ParseFtpPath(string path)
    {
        var trimmed = path.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        var hostAndPort = firstSlash < 0 ? trimmed : trimmed[..firstSlash];
        var remoteBasePath = firstSlash < 0 ? string.Empty : trimmed[(firstSlash + 1)..];

        var colonIndex = hostAndPort.IndexOf(':');
        if (colonIndex < 0)
        {
            return (hostAndPort, 21, remoteBasePath);
        }

        var host = hostAndPort[..colonIndex];
        var port = int.Parse(hostAndPort[(colonIndex + 1)..], System.Globalization.CultureInfo.InvariantCulture);
        return (host, port, remoteBasePath);
    }
}
