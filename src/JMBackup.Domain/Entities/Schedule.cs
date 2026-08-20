using JMBackup.Domain.Enums;

namespace JMBackup.Domain.Entities;

/// <summary>
/// Un horario de ejecución (RF-30 a RF-32). <see cref="Weekdays"/> (0=domingo..6=sábado),
/// <see cref="MonthDays"/> (1-31) y <see cref="Times"/> ("HH:mm") se guardan como texto
/// separado por comas: son listas cortas y de solo lectura para Quartz, no hace falta
/// una tabla hija para esto.
/// </summary>
public sealed class Schedule
{
    public int Id { get; init; }

    public int TaskId { get; set; }

    public ScheduleFrequency Frequency { get; set; }

    /// <summary>CSV de días de la semana (0-6), solo con <see cref="ScheduleFrequency.Custom"/>.</summary>
    public string? Weekdays { get; set; }

    /// <summary>CSV de días del mes (1-31), solo con <see cref="ScheduleFrequency.Custom"/>.</summary>
    public string? MonthDays { get; set; }

    /// <summary>CSV de horas "HH:mm". Una tarea puede ejecutarse varias veces por día (RF-32).</summary>
    public required string Times { get; set; }

    /// <summary>[H2] Ventana de respaldo (RF-33): duración máxima. Columna creada, sin usar.</summary>
    public int? WindowMinutes { get; set; }

    /// <summary>[H2] Ventana de respaldo (RF-33): hora límite. Columna creada, sin usar.</summary>
    public TimeOnly? WindowEndTime { get; set; }

    /// <summary>[H2] Recuperación de ejecuciones perdidas (RF-34). Columna creada, sin usar.</summary>
    public string? CatchupPolicy { get; set; }
}
