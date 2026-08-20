namespace JMBackup.Domain.Enums;

/// <summary>Verificación posterior a la copia de una tarea (RF-75). No confundir con
/// los tres niveles de la comparación tricolor (RF-140s), que son [H2] completo.</summary>
public enum VerifyLevel
{
    SizeOnly = 0,
    SizeAndHash = 1,
}
