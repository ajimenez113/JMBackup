namespace JMBackup.Domain.Enums;

/// <summary>Orden de procesamiento de archivos dentro de una tarea (RF-15).</summary>
public enum OrderStrategy
{
    NameAscending = 0,
    NameDescending = 1,
    SizeDescending = 2,
    SizeAscending = 3,
    ModifiedOldestFirst = 4,
    ModifiedNewestFirst = 5,
}
