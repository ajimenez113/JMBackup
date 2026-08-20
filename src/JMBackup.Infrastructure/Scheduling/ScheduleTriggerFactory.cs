using System.Globalization;
using JMBackup.Domain.Entities;
using JMBackup.Domain.Enums;
using Quartz;

namespace JMBackup.Infrastructure.Scheduling;

/// <summary>
/// Traduce un <see cref="Schedule"/> (RF-30 a RF-32) a disparadores de Quartz.
/// Semanal y personalizada con días de semana usan el mismo mecanismo (cron por día de
/// semana); mensual y personalizada con días del mes, también (cron por día del mes).
/// Personalizada con ambos produce la unión de los dos conjuntos de disparadores — cron
/// estándar no permite combinar día-de-mes y día-de-semana en una sola expresión, así
/// que "y/o" (RF-31) se resuelve con más de un disparador para el mismo trabajo.
/// Quincenal no tiene equivalente directo en cron: usa un intervalo de calendario de
/// Quartz de 2 semanas, anclado al primer día de semana configurado.
/// </summary>
public static class ScheduleTriggerFactory
{
    public static IReadOnlyList<ITrigger> BuildTriggers(Schedule schedule, JobKey jobKey, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(jobKey);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var times = ParseTimes(schedule.Times);
        var triggers = new List<ITrigger>();

        switch (schedule.Frequency)
        {
            case ScheduleFrequency.Daily:
                triggers.AddRange(times.Select(time => BuildCronTrigger(jobKey, DailyCron(time))));
                break;

            case ScheduleFrequency.Weekly:
                triggers.AddRange(BuildWeekdayTriggers(schedule, jobKey, times));
                break;

            case ScheduleFrequency.Biweekly:
                triggers.AddRange(BuildBiweeklyTriggers(schedule, jobKey, times, timeProvider));
                break;

            case ScheduleFrequency.Monthly:
                triggers.AddRange(BuildMonthDayTriggers(schedule, jobKey, times));
                break;

            case ScheduleFrequency.Custom:
                triggers.AddRange(BuildWeekdayTriggers(schedule, jobKey, times));
                triggers.AddRange(BuildMonthDayTriggers(schedule, jobKey, times));
                break;
        }

        return triggers;
    }

    private static IEnumerable<ITrigger> BuildWeekdayTriggers(Schedule schedule, JobKey jobKey, IReadOnlyList<TimeOnly> times) =>
        from day in ParseWeekdays(schedule.Weekdays)
        from time in times
        select BuildCronTrigger(jobKey, WeeklyCron(day, time));

    private static IEnumerable<ITrigger> BuildMonthDayTriggers(Schedule schedule, JobKey jobKey, IReadOnlyList<TimeOnly> times) =>
        from day in ParseMonthDays(schedule.MonthDays)
        from time in times
        select BuildCronTrigger(jobKey, MonthlyCron(day, time));

    private static IEnumerable<ITrigger> BuildBiweeklyTriggers(
        Schedule schedule, JobKey jobKey, IReadOnlyList<TimeOnly> times, TimeProvider timeProvider)
    {
        var anchorDay = ParseWeekdays(schedule.Weekdays).FirstOrDefault(DayOfWeek.Sunday);

        return times.Select(time =>
        {
            var startAt = NextOccurrence(anchorDay, time, timeProvider);
            return (ITrigger)TriggerBuilder.Create()
                .ForJob(jobKey)
                .StartAt(startAt)
                .WithCalendarIntervalSchedule(schedule => schedule.WithIntervalInWeeks(2))
                .Build();
        });
    }

    private static DateTimeOffset NextOccurrence(DayOfWeek day, TimeOnly time, TimeProvider timeProvider)
    {
        var now = timeProvider.GetLocalNow();
        var candidate = new DateTimeOffset(now.Date, now.Offset) + time.ToTimeSpan();

        var daysUntil = ((int)day - (int)candidate.DayOfWeek + 7) % 7;
        candidate = candidate.AddDays(daysUntil);

        return candidate <= now ? candidate.AddDays(7) : candidate;
    }

    private static ITrigger BuildCronTrigger(JobKey jobKey, string cronExpression) =>
        TriggerBuilder.Create()
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression)
            .Build();

    private static string DailyCron(TimeOnly time) =>
        FormattableString.Invariant($"{time.Second} {time.Minute} {time.Hour} * * ?");

    private static string WeeklyCron(DayOfWeek day, TimeOnly time) =>
        FormattableString.Invariant($"{time.Second} {time.Minute} {time.Hour} ? * {QuartzDayOfWeek(day)}");

    private static string MonthlyCron(int dayOfMonth, TimeOnly time) =>
        FormattableString.Invariant($"{time.Second} {time.Minute} {time.Hour} {dayOfMonth} * ?");

    private static string QuartzDayOfWeek(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => "SUN",
        DayOfWeek.Monday => "MON",
        DayOfWeek.Tuesday => "TUE",
        DayOfWeek.Wednesday => "WED",
        DayOfWeek.Thursday => "THU",
        DayOfWeek.Friday => "FRI",
        DayOfWeek.Saturday => "SAT",
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };

    private static List<TimeOnly> ParseTimes(string times) =>
        times.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => TimeOnly.ParseExact(text, "HH:mm", CultureInfo.InvariantCulture))
            .ToList();

    private static List<DayOfWeek> ParseWeekdays(string? weekdays) =>
        (weekdays ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => (DayOfWeek)int.Parse(text, CultureInfo.InvariantCulture))
            .ToList();

    private static List<int> ParseMonthDays(string? monthDays) =>
        (monthDays ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => int.Parse(text, CultureInfo.InvariantCulture))
            .ToList();
}
