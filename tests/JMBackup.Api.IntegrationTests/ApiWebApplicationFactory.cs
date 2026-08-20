using JMBackup.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JMBackup.Api.IntegrationTests;

/// <summary>
/// Hospeda JMBackup.Api en memoria para las pruebas de integración, reemplazando el
/// SQLite en disco por una conexión SQLite en memoria (ver ADR-005 y CLAUDE.md §7).
/// La conexión se mantiene abierta durante toda la vida de la fábrica: SQLite en
/// memoria solo existe mientras la conexión que la creó siga abierta.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextFactory<JMBackupDbContext>>();
            services.AddDbContextFactory<JMBackupDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}
