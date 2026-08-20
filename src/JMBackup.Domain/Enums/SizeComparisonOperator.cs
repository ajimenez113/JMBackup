namespace JMBackup.Domain.Enums;

/// <summary>
/// Comparador usado por las exclusiones de tamaño y antigüedad (RF-46): "mayor que" o
/// "menor que" el valor de la regla.
/// </summary>
public enum SizeComparisonOperator
{
    GreaterThan = 0,
    LessThan = 1,
}
