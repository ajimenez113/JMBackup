using JMBackup.Domain.Enums;

namespace JMBackup.Storage.Remote;

/// <summary>
/// Traduce el código de error de una <c>AmazonS3Exception</c> a un motivo comprensible
/// (RF-22). Sin estado y sin el SDK de AWS en la firma (recibe el código como texto
/// plano) — a propósito, para poder probarlo con xUnit corriente: LocalStack no
/// necesariamente reproduce con fidelidad todos los casos reales (en particular
/// "región incorrecta", que en AWS real depende de un redirect entre endpoints
/// regionales que LocalStack no simula).
/// </summary>
public static class S3ConnectionErrorMapper
{
    public static (StorageErrorReason Reason, string Detail) Map(string? errorCode, string bucket, string fallbackMessage) => errorCode switch
    {
        "InvalidAccessKeyId" or "SignatureDoesNotMatch" =>
            (StorageErrorReason.InvalidCredentials, "La clave de acceso o la clave secreta son incorrectas."),
        "NoSuchBucket" =>
            (StorageErrorReason.PathNotFound, $"El bucket \"{bucket}\" no existe."),
        "AccessDenied" =>
            (StorageErrorReason.PermissionDenied, $"Permiso denegado al acceder al bucket \"{bucket}\"."),
        "PermanentRedirect" or "AuthorizationHeaderMalformed" =>
            (StorageErrorReason.WrongRegion, $"El bucket \"{bucket}\" existe, pero en una región distinta de la configurada."),
        _ => (StorageErrorReason.Unknown, fallbackMessage),
    };
}
