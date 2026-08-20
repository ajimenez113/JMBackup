namespace JMBackup.Domain.Entities;

/// <summary>
/// Un par clave/valor de configuración persistida. El valor se guarda serializado
/// como JSON en <see cref="ValueJson"/>, para no atar el esquema de la tabla a cada
/// tipo de ajuste que se agregue con el tiempo.
/// </summary>
public sealed class Setting
{
    public required string Key { get; init; }

    public required string ValueJson { get; init; }
}
