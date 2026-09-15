using Amazon.S3;
using Amazon.S3.Model;
using JMBackup.Application.Abstractions;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;

namespace JMBackup.Storage.Remote;

/// <summary>
/// Amazon S3 (fase 5, hito 2), con AWSSDK.S3. Una instancia es un bucket + prefijo raíz
/// (ADR-014), igual que <see cref="FtpStorageBackend"/>.
///
/// S3 no es un sistema de archivos — no tiene directorios reales (solo prefijos de
/// clave), no tiene renombrado atómico, y no tiene concepto de espacio libre. Ver
/// ADR-031 para el detalle de cómo se resuelve cada diferencia.
///
/// Reanuda *multipart uploads* interrumpidos, validando identidad del origen antes de
/// reanudar (ADR-027): un multipart en curso solo se retoma si el tamaño y la fecha de
/// modificación del origen coinciden con los que tenía cuando se inició; si no, se
/// aborta explícitamente y arranca uno nuevo desde cero.
/// </summary>
public sealed class S3StorageBackend : IStorageBackend
{
    // Umbral y tamaño de parte: por debajo del umbral, un PutObject directo (no tiene
    // sentido "reanudar" algo tan chico — reintentarlo entero es más simple y barato
    // que la contabilidad de un multipart). El tamaño de parte es generoso frente al
    // mínimo real de S3 (5 MiB, salvo la última parte) para no multiplicar llamadas.
    private const long MultipartThresholdBytes = 8L * 1024 * 1024;
    private const long PartSizeBytes = 8L * 1024 * 1024;
    private const int CopyBufferSize = 81920;

    private readonly string _bucket;
    private readonly string _basePrefix;
    private readonly string _accessKeyId;
    private readonly string _secretAccessKey;
    private readonly string _region;
    private readonly string? _storageClass;
    private readonly bool _serverSideEncryption;
    private readonly TimeProvider _timeProvider;
    private readonly string? _serviceUrl;
    private readonly bool _forcePathStyle;

    // Vive mientras viva esta instancia (un run completo, ADR-014) — misma garantía de
    // reanudación "entre pasadas de reintento de la misma ejecución" que ya tiene
    // FtpStorageBackend, no reanudación entre corridas separadas (ADR-027).
    private readonly Dictionary<string, (long Size, DateTimeOffset SourceModifiedUtc, string UploadId)> _partialUploads =
        new(StringComparer.OrdinalIgnoreCase);

    private AmazonS3Client? _client;

    public S3StorageBackend(
        string bucket,
        string basePrefix,
        string accessKeyId,
        string secretAccessKey,
        string region,
        string? storageClass,
        bool serverSideEncryption,
        TimeProvider timeProvider,
        string? serviceUrl = null,
        bool forcePathStyle = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentNullException.ThrowIfNull(basePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretAccessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _bucket = bucket;
        _basePrefix = NormalizeKey(basePrefix).Trim('/');
        _accessKeyId = accessKeyId;
        _secretAccessKey = secretAccessKey;
        _region = region;
        _storageClass = string.IsNullOrWhiteSpace(storageClass) ? null : storageClass;
        _serverSideEncryption = serverSideEncryption;
        _timeProvider = timeProvider;
        _serviceUrl = serviceUrl;
        _forcePathStyle = forcePathStyle;
    }

    public async Task<ConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = GetClient();
            await client.HeadBucketAsync(new HeadBucketRequest { BucketName = _bucket }, cancellationToken).ConfigureAwait(false);
            return ConnectionStatus.Connected();
        }
        catch (AmazonS3Exception ex)
        {
            var (reason, detail) = S3ConnectionErrorMapper.Map(ex.ErrorCode, _bucket, ex.Message);
            return ConnectionStatus.Failed(reason, detail);
        }
        catch (Exception ex) when (IsConnectivityException(ex))
        {
            return ConnectionStatus.Failed(StorageErrorReason.HostUnreachable, $"No se pudo conectar al bucket \"{_bucket}\".");
        }
    }

    public async IAsyncEnumerable<FileEntry> ListAsync(
        string path, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = GetClient();
        var prefix = CombineKey(path);
        var queryPrefix = prefix.Length == 0 ? string.Empty : prefix.TrimEnd('/') + "/";

        string? continuationToken = null;
        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = queryPrefix,
                ContinuationToken = continuationToken,
            };
            if (!recursive)
            {
                request.Delimiter = "/";
            }

            var response = await client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);

            if (!recursive)
            {
                foreach (var commonPrefix in response.CommonPrefixes)
                {
                    var relative = GetRelativeTo(queryPrefix, commonPrefix).TrimEnd('/');
                    if (relative.Length > 0)
                    {
                        yield return new FileEntry(relative, IsDirectory: true, 0, DateTimeOffset.MinValue);
                    }
                }
            }

            foreach (var s3Object in response.S3Objects)
            {
                // El propio "prefijo/" a veces existe como objeto de cero bytes (lo crea
                // la consola de AWS al mostrar una "carpeta" vacía) — no es una entrada
                // real, es como si no existiera.
                if (s3Object.Key == queryPrefix)
                {
                    continue;
                }

                var relative = GetRelativeTo(queryPrefix, s3Object.Key);
                yield return new FileEntry(relative, IsDirectory: false, s3Object.Size ?? 0, ToUtcOffset(s3Object.LastModified));
            }

            continuationToken = response.NextContinuationToken;
        }
        while (continuationToken is not null);
    }

    public async Task<FileEntry?> StatAsync(string path, CancellationToken cancellationToken)
    {
        var client = GetClient();
        var key = CombineKey(path);

        try
        {
            var response = await client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = _bucket, Key = key }, cancellationToken).ConfigureAwait(false);
            return new FileEntry(path, IsDirectory: false, response.ContentLength, ToUtcOffset(response.LastModified));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        var client = GetClient();
        var key = CombineKey(path);

        GetObjectMetadataResponse metadata;
        try
        {
            metadata = await client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = _bucket, Key = key }, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new StorageOperationException(StorageErrorReason.PathNotFound, path, $"No se encontró \"{path}\" en el bucket.", ex);
        }

        if (IsArchivedAndUnavailable(metadata))
        {
            throw new StorageOperationException(
                StorageErrorReason.ObjectArchived, path,
                $"\"{path}\" está en {metadata.StorageClass} y no fue restaurado — hay que pedir la restauración " +
                "primero (puede tardar horas) antes de poder leerlo.");
        }

        try
        {
            var response = await client.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = key }, cancellationToken)
                .ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "AccessDenied")
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

        var key = CombineKey(path);

        if (size < MultipartThresholdBytes)
        {
            await WriteSmallObjectAsync(key, path, content, size, progress, cancellationToken).ConfigureAwait(false);
            return;
        }

        await WriteMultipartAsync(key, path, content, size, sourceModifiedUtc, progress, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var client = GetClient();
        var key = CombineKey(path);

        try
        {
            await client.DeleteObjectAsync(_bucket, key, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Puede ser un "directorio" (prefijo), no un objeto — se maneja abajo.
        }

        await DeleteByPrefixAsync(client, key.TrimEnd('/') + "/", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>No-op real, no simulado: S3 no necesita ningún objeto "carpeta" para que un PutObject a una clave anidada funcione (ver ADR-031).</summary>
    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        var client = GetClient();
        var sourceKey = CombineKey(sourcePath);
        var destinationKey = CombineKey(destinationPath);

        var isObject = await StatAsync(sourcePath, cancellationToken).ConfigureAwait(false) is { IsDirectory: false };
        if (isObject)
        {
            await CopyThenDeleteAsync(client, sourceKey, destinationKey, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Prefijo ("directorio"): mover cada objeto de abajo, uno por uno — S3 no tiene
        // una operación de "renombrar carpeta" ni siquiera para esto.
        var sourcePrefix = sourceKey.TrimEnd('/') + "/";
        await foreach (var entry in ListAsync(sourcePath, recursive: true, cancellationToken).ConfigureAwait(false))
        {
            if (entry.IsDirectory)
            {
                continue;
            }

            var relativeDestinationKey = CombineKey(destinationPath.TrimEnd('/') + "/" + entry.Path);
            await CopyThenDeleteAsync(client, sourcePrefix + entry.Path, relativeDestinationKey, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>S3 no tiene concepto de espacio libre — igual que FTP (ver IStorageBackend).</summary>
    public Task<long?> GetFreeSpaceAsync(CancellationToken cancellationToken) => Task.FromResult<long?>(null);

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task WriteSmallObjectAsync(
        string key, string path, Stream content, long size, IProgress<TransferProgress> progress, CancellationToken cancellationToken)
    {
        var client = GetClient();
        var startedAt = _timeProvider.GetTimestamp();

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            StreamTransferProgress = (_, args) =>
                ReportProgress(progress, path, size, resumeFromBytes: 0, args.TransferredBytes, startedAt),
        };
        ApplyStorageOptions(request);

        try
        {
            await client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "AccessDenied")
        {
            throw new StorageOperationException(StorageErrorReason.PermissionDenied, path, $"Permiso denegado al escribir \"{path}\".", ex);
        }
    }

    private async Task WriteMultipartAsync(
        string key, string path, Stream content, long size, DateTimeOffset sourceModifiedUtc, IProgress<TransferProgress> progress,
        CancellationToken cancellationToken)
    {
        var client = GetClient();
        string uploadId;
        var uploadedParts = new List<PartETag>();
        var resumeFromBytes = 0L;

        if (_partialUploads.TryGetValue(path, out var recorded))
        {
            var listPartsResponse = await client
                .ListPartsAsync(new ListPartsRequest { BucketName = _bucket, Key = key, UploadId = recorded.UploadId }, cancellationToken)
                .ConfigureAwait(false);
            var bytesTransferred = listPartsResponse.Parts.Sum(part => part.Size ?? 0);
            var existingPartial = new PartialUpload(recorded.Size, recorded.SourceModifiedUtc, bytesTransferred);

            if (ResumeDecision.ShouldResume(existingPartial, size, sourceModifiedUtc))
            {
                uploadId = recorded.UploadId;
                resumeFromBytes = bytesTransferred;
                uploadedParts.AddRange(listPartsResponse.Parts.Select(part => new PartETag(part.PartNumber!.Value, part.ETag)));
                SkipBytes(content, resumeFromBytes);
                progress.Report(new TransferProgress(path, resumeFromBytes, size, BytesPerSecond: 0, EstimatedTimeRemaining: null));
            }
            else
            {
                // El origen cambió: el multipart anterior queda huérfano si no se aborta
                // a mano — una parte huérfana se sigue facturando y no aparece en el
                // listado normal del bucket (ADR-031).
                await client
                    .AbortMultipartUploadAsync(new AbortMultipartUploadRequest { BucketName = _bucket, Key = key, UploadId = recorded.UploadId }, cancellationToken)
                    .ConfigureAwait(false);
                _partialUploads.Remove(path);
                uploadId = await InitiateMultipartUploadAsync(client, key, cancellationToken).ConfigureAwait(false);
                _partialUploads[path] = (size, sourceModifiedUtc, uploadId);
            }
        }
        else
        {
            uploadId = await InitiateMultipartUploadAsync(client, key, cancellationToken).ConfigureAwait(false);
            _partialUploads[path] = (size, sourceModifiedUtc, uploadId);
        }

        var nextPartNumber = uploadedParts.Count + 1;
        var startedAt = _timeProvider.GetTimestamp();
        var buffer = new byte[PartSizeBytes];
        var totalTransferred = resumeFromBytes;

        while (totalTransferred < size)
        {
            var bytesToRead = (int)Math.Min(PartSizeBytes, size - totalTransferred);
            var read = await ReadExactAsync(content, buffer, bytesToRead, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            using var partStream = new MemoryStream(buffer, 0, read);
            var uploadPartResponse = await client.UploadPartAsync(
                new UploadPartRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    UploadId = uploadId,
                    PartNumber = nextPartNumber,
                    InputStream = partStream,
                    PartSize = read,
                    IsLastPart = totalTransferred + read >= size,
                },
                cancellationToken).ConfigureAwait(false);

            uploadedParts.Add(new PartETag(nextPartNumber, uploadPartResponse.ETag));
            totalTransferred += read;
            nextPartNumber++;

            ReportProgress(progress, path, size, resumeFromBytes, totalTransferred - resumeFromBytes, startedAt);
        }

        await client.CompleteMultipartUploadAsync(
            new CompleteMultipartUploadRequest { BucketName = _bucket, Key = key, UploadId = uploadId, PartETags = uploadedParts },
            cancellationToken).ConfigureAwait(false);

        _partialUploads.Remove(path);
    }

    private async Task<string> InitiateMultipartUploadAsync(AmazonS3Client client, string key, CancellationToken cancellationToken)
    {
        var request = new InitiateMultipartUploadRequest { BucketName = _bucket, Key = key };
        ApplyStorageOptions(request);
        var response = await client.InitiateMultipartUploadAsync(request, cancellationToken).ConfigureAwait(false);
        return response.UploadId;
    }

    private void ApplyStorageOptions(PutObjectRequest request)
    {
        if (_storageClass is not null)
        {
            request.StorageClass = new S3StorageClass(_storageClass);
        }

        if (_serverSideEncryption)
        {
            request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
        }
    }

    private void ApplyStorageOptions(InitiateMultipartUploadRequest request)
    {
        if (_storageClass is not null)
        {
            request.StorageClass = new S3StorageClass(_storageClass);
        }

        if (_serverSideEncryption)
        {
            request.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
        }
    }

    private async Task DeleteByPrefixAsync(AmazonS3Client client, string prefix, CancellationToken cancellationToken)
    {
        string? continuationToken = null;
        do
        {
            var listResponse = await client.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix, ContinuationToken = continuationToken },
                cancellationToken).ConfigureAwait(false);

            if (listResponse.S3Objects.Count > 0)
            {
                await client.DeleteObjectsAsync(
                    new DeleteObjectsRequest
                    {
                        BucketName = _bucket,
                        Objects = listResponse.S3Objects.Select(s3Object => new KeyVersion { Key = s3Object.Key }).ToList(),
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            continuationToken = listResponse.NextContinuationToken;
        }
        while (continuationToken is not null);
    }

    private async Task CopyThenDeleteAsync(AmazonS3Client client, string sourceKey, string destinationKey, CancellationToken cancellationToken)
    {
        await client.CopyObjectAsync(
            new CopyObjectRequest { SourceBucket = _bucket, SourceKey = sourceKey, DestinationBucket = _bucket, DestinationKey = destinationKey },
            cancellationToken).ConfigureAwait(false);
        await client.DeleteObjectAsync(_bucket, sourceKey, cancellationToken).ConfigureAwait(false);
    }

    private AmazonS3Client GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region) };
        if (_serviceUrl is not null)
        {
            // Solo lo usan las pruebas contra LocalStack: un endpoint de AWS real nunca
            // necesita esto, ya lo resuelve RegionEndpoint.
            config.ServiceURL = _serviceUrl;
            config.ForcePathStyle = _forcePathStyle;
        }

        _client = new AmazonS3Client(_accessKeyId, _secretAccessKey, config);
        return _client;
    }

    private void ReportProgress(
        IProgress<TransferProgress> progress, string path, long totalSize, long resumeFromBytes, long transferredSinceStart, long startedAtTimestamp)
    {
        var totalTransferred = resumeFromBytes + transferredSinceStart;
        var elapsed = _timeProvider.GetElapsedTime(startedAtTimestamp);
        var bytesPerSecond = elapsed.TotalSeconds > 0 ? transferredSinceStart / elapsed.TotalSeconds : 0;
        var remainingBytes = totalSize - totalTransferred;
        TimeSpan? estimatedTimeRemaining = bytesPerSecond > 0 ? TimeSpan.FromSeconds(remainingBytes / bytesPerSecond) : null;

        progress.Report(new TransferProgress(path, totalTransferred, totalSize, bytesPerSecond, estimatedTimeRemaining));
    }

    /// <summary>
    /// GlacierInstantRetrieval se lee como Standard pese al nombre — solo Glacier y
    /// Deep Archive necesitan restauración primero. "Restaurado" es tener
    /// RestoreExpiration con una fecha futura; RestoreInProgress=true significa que la
    /// restauración está en curso pero todavía no se puede leer.
    /// </summary>
    private static bool IsArchivedAndUnavailable(GetObjectMetadataResponse metadata)
    {
        var isColdStorage = metadata.StorageClass == S3StorageClass.Glacier || metadata.StorageClass == S3StorageClass.DeepArchive;
        if (!isColdStorage)
        {
            return false;
        }

        var restoreInProgress = metadata.RestoreInProgress ?? false;
        var isRestored = !restoreInProgress && metadata.RestoreExpiration is { } expiration && expiration > DateTime.UtcNow;
        return !isRestored;
    }

    private string CombineKey(string relativePath)
    {
        var normalized = NormalizeKey(relativePath);
        return _basePrefix.Length == 0 ? normalized : normalized.Length == 0 ? _basePrefix : $"{_basePrefix}/{normalized}";
    }

    /// <summary>
    /// S3 acepta "\" como carácter literal en una clave — una ruta de Windows que se
    /// cuele acá (en vez de la "/" que exige IStorageBackend) crearía una clave
    /// corrupta que después no coincide con nada al listar o comparar. Se normaliza en
    /// el borde del backend, no se confía en que quien llama ya lo haya hecho.
    /// </summary>
    private static string NormalizeKey(string path) => path.Replace('\\', '/').TrimStart('/');

    private static DateTimeOffset ToUtcOffset(DateTime? value) =>
        value is { } dateTime ? new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)) : DateTimeOffset.MinValue;

    private static string GetRelativeTo(string queryPrefix, string fullKey) =>
        fullKey.StartsWith(queryPrefix, StringComparison.Ordinal) ? fullKey[queryPrefix.Length..] : fullKey;

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    private static void SkipBytes(Stream stream, long count)
    {
        if (stream.CanSeek)
        {
            stream.Seek(count, SeekOrigin.Begin);
            return;
        }

        var buffer = new byte[CopyBufferSize];
        var remaining = count;
        while (remaining > 0)
        {
            var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0)
            {
                break;
            }

            remaining -= read;
        }
    }

    private static bool IsConnectivityException(Exception ex) =>
        ex is System.Net.Sockets.SocketException or TimeoutException or IOException or TaskCanceledException;
}
