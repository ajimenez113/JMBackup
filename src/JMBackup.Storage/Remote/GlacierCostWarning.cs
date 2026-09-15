namespace JMBackup.Storage.Remote;

/// <summary>
/// Aviso de costo al elegir una clase de almacenamiento fría de S3. Sin estado y sin
/// dependencias del SDK de AWS — recibe el nombre de la clase como texto plano (el
/// mismo que <see cref="JMBackup.Domain.Entities.TaskPath.StorageClass"/> guarda), para
/// poder probarlo con xUnit corriente sin un bucket real de por medio.
///
/// No es un cálculo de costo en dinero (los precios de AWS cambian y varían por
/// región) — es la advertencia cualitativa que RF-90s pide: esto es más barato de
/// guardar, pero mucho más lento y con costo de recuperar.
/// </summary>
public static class GlacierCostWarning
{
    public static string? ForStorageClass(string? storageClass)
    {
        if (string.IsNullOrWhiteSpace(storageClass))
        {
            return null;
        }

        return storageClass.Trim().ToUpperInvariant() switch
        {
            "GLACIER" =>
                "Glacier es mucho más barato de guardar, pero un archivo ahí no se puede leer directamente: " +
                "primero hay que pedir su restauración, que tarda entre varios minutos y varias horas según la " +
                "urgencia elegida, y esa restauración también tiene costo. No conviene para nada que necesites " +
                "recuperar rápido.",
            "DEEP_ARCHIVE" =>
                "Glacier Deep Archive es la clase más barata de guardar que existe, pero también la más lenta de " +
                "recuperar: una restauración puede tardar hasta 12 horas, y tiene costo aparte del de " +
                "almacenamiento. Pensala solo para respaldos que casi con certeza nunca vas a necesitar recuperar.",
            _ => null,
        };
    }
}
