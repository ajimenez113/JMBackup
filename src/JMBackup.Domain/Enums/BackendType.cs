namespace JMBackup.Domain.Enums;

/// <summary>
/// Tipo de backend de una ruta o credencial. En el hito 1 solo se usa
/// <see cref="Local"/> (disco local/UNC); los demás valores existen desde ahora para
/// no migrar destructivamente cuando lleguen en el hito 2, igual que
/// <see cref="JMBackup.Domain.Enums.BackupMode"/>.
/// </summary>
public enum BackendType
{
    Local = 0,

    /// <summary>[H2]</summary>
    Ftp = 1,

    /// <summary>[H2]</summary>
    Sftp = 2,

    /// <summary>[H2]</summary>
    S3 = 3,
}
