using Microsoft.Data.Sqlite;

namespace JMBackup.Infrastructure.Scheduling;

/// <summary>
/// Aplica el esquema de tablas <c>QRTZ_*</c> a la misma base SQLite si todavía no
/// existen (ADR-011). EF Core no las gestiona: son de Quartz, no del dominio de la
/// aplicación, así que no tienen migración propia.
/// </summary>
public static class QuartzSchemaInitializer
{
    public static void EnsureSchema(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'QRTZ_JOB_DETAILS';";
            if (checkCommand.ExecuteScalar() is not null)
            {
                return;
            }
        }

        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = ReadEmbeddedScript();
        createCommand.ExecuteNonQuery();
    }

    private static string ReadEmbeddedScript()
    {
        var assembly = typeof(QuartzSchemaInitializer).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("QuartzSqliteSchema.sql", StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"No se encontró el recurso incrustado \"{resourceName}\".");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
