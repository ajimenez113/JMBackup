using JMBackup.Infrastructure.Options;
using JMBackup.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// %ProgramData%\JMBackup\config.json tiene prioridad sobre appsettings.json: se agrega
// después, y en Microsoft.Extensions.Configuration la última fuente agregada gana.
var machineConfigPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "JMBackup",
    "config.json");
builder.Configuration.AddJsonFile(machineConfigPath, optional: true, reloadOnChange: true);

var pathsSection = builder.Configuration.GetSection(JMBackupPathsOptions.SectionName);
var paths = pathsSection.Get<JMBackupPathsOptions>() ?? new JMBackupPathsOptions();

builder.Services
    .AddOptions<JMBackupPathsOptions>()
    .Bind(pathsSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Host.UseSerilog((_, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.File(
        new CompactJsonFormatter(),
        Path.Combine(paths.DataDirectory, "logs", "jmbackup-.json"),
        rollingInterval: RollingInterval.Day));

builder.Services.AddJMBackupPersistence(paths);

var app = builder.Build();

using (var startupScope = app.Services.CreateScope())
{
    startupScope.ServiceProvider.GetRequiredService<JMBackupDbContext>().MigrateAndEnableWalMode();
}

app.Run();

// Expone la clase Program generada por las sentencias de nivel superior para que
// WebApplicationFactory<Program> pueda hospedar esta aplicación en las pruebas de integración.
public partial class Program;
