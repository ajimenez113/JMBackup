namespace JMBackup.Domain.Enums;

/// <summary>Tipo de una regla de exclusión (RF-40 a RF-46).</summary>
public enum ExclusionKind
{
    Extension = 0,
    FileName = 1,
    Folder = 2,
    Contains = 3,
    Size = 4,
    Age = 5,
}
