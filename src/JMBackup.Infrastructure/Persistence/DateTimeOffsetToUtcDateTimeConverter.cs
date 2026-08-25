using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JMBackup.Infrastructure.Persistence;

/// <summary>
/// El proveedor de SQLite de EF Core tiene soporte muy limitado para
/// <see cref="DateTimeOffset"/>: ni las comparaciones en <c>WHERE</c> ni el
/// <c>ORDER BY</c> se traducen a SQL (fallan en tiempo de ejecución, no de
/// compilación). Como toda fecha en JMBackup ya se genera en UTC vía
/// <c>TimeProvider.GetUtcNow()</c> (nunca se necesita preservar un offset
/// distinto), se convierte a <see cref="DateTime"/> UTC para guardarla: ese tipo sí
/// tiene soporte completo de comparación y orden en SQLite.
/// </summary>
public sealed class DateTimeOffsetToUtcDateTimeConverter() : ValueConverter<DateTimeOffset, DateTime>(
    toProvider => toProvider.UtcDateTime,
    fromProvider => new DateTimeOffset(DateTime.SpecifyKind(fromProvider, DateTimeKind.Utc)));
