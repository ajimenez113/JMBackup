using System.Globalization;
using System.Net;
using FluentValidation;
using JMBackup.Api.Authentication;
using JMBackup.Api.Contracts;
using JMBackup.Api.Endpoints;
using JMBackup.Api.Hubs;
using JMBackup.Api.Security;
using JMBackup.Api.Validators;
using JMBackup.Application;
using JMBackup.Application.Abstractions;
using JMBackup.Application.Settings;
using JMBackup.Infrastructure;
using JMBackup.Infrastructure.Options;
using JMBackup.Infrastructure.Persistence;
using JMBackup.Infrastructure.Persistence.Repositories;
using JMBackup.Infrastructure.Scheduling;
using JMBackup.Infrastructure.Security;
using JMBackup.Storage.Local;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// %ProgramData%\JMBackup\config.json tiene prioridad sobre appsettings.json: se agrega
// después, y en Microsoft.Extensions.Configuration la última fuente agregada gana.
var machineConfigPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JMBackup", "config.json");
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
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File(
        new CompactJsonFormatter(),
        Path.Combine(paths.DataDirectory, "logs", "jmbackup-.json"),
        rollingInterval: RollingInterval.Day));

// Arranque en dos tiempos: hace falta saber si hay credencial configurada y en qué
// dirección/puerto escuchar ANTES de construir Kestrel (ADR-007, RF-111), y esa
// configuración vive en Settings, no en appsettings.json. Se migra y se lee acá, con
// un DbContext de arranque fuera del contenedor de DI, que todavía no existe.
Directory.CreateDirectory(paths.DataDirectory);
var databasePath = Path.Combine(paths.DataDirectory, "jmbackup.db");
var bootstrapDbOptions = new DbContextOptionsBuilder<JMBackupDbContext>().UseSqlite($"Data Source={databasePath}").Options;

await using (var bootstrapContext = new JMBackupDbContext(bootstrapDbOptions))
{
    bootstrapContext.MigrateAndEnableWalMode();
}

var bootstrapSettingsService = new SettingsService(new EfSettingsStore(new BootstrapDbContextFactory(bootstrapDbOptions)));
var security = await bootstrapSettingsService.GetSecurityAsync(CancellationToken.None);
var web = await bootstrapSettingsService.GetWebAsync(CancellationToken.None);

var canExposeToLan = !string.IsNullOrEmpty(security.Username) || security.AllowUnauthenticatedLan;
var listenAddress = canExposeToLan ? IPAddress.Parse(web.ListenAddress) : IPAddress.Loopback;

var certificateProvider = new SelfSignedCertificateProvider(paths);
var certificate = certificateProvider.GetOrCreateCertificate();

builder.WebHost.ConfigureKestrel(options =>
    options.Listen(listenAddress, web.Port, listenOptions => listenOptions.UseHttps(certificate)));

builder.Host.UseWindowsService(options => options.ServiceName = "JMBackup");

builder.Services.AddJMBackupPersistence(paths);
builder.Services.AddJMBackupRepositories();
builder.Services.AddJMBackupSecurity();
builder.Services.AddJMBackupApplication();
builder.Services.AddJMBackupScheduling(paths);
builder.Services.AddSingleton<IStorageBackendFactory, LocalStorageBackendFactory>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProgressPublisher, SignalRProgressPublisher>();
builder.Services.AddSingleton<LoginAttemptThrottle>();
builder.Services.AddScoped<IAuthorizationHandler, CredentialRequirementHandler>();
builder.Services.AddSingleton<PathValidationService>();

// FluentValidation 12 ya no trae el registro automático por ensamblado
// (era un paquete aparte, FluentValidation.DependencyInjectionExtensions); se
// registra cada validador a mano en vez de sumar un paquete para un solo método.
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();
builder.Services.AddScoped<IValidator<TaskPathRequest>, TaskPathRequestValidator>();
builder.Services.AddScoped<IValidator<ExclusionRequest>, ExclusionRequestValidator>();
builder.Services.AddScoped<IValidator<FilterRequest>, FilterRequestValidator>();
builder.Services.AddScoped<IValidator<ScheduleRequest>, ScheduleRequestValidator>();
builder.Services.AddScoped<IValidator<CredentialRequest>, CredentialRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<SecuritySettingsRequest>, SecuritySettingsRequestValidator>();
builder.Services.AddScoped<IValidator<WebSettingsRequest>, WebSettingsRequestValidator>();
builder.Services.AddScoped<IValidator<GeneralSettingsRequest>, GeneralSettingsRequestValidator>();
builder.Services.AddScoped<IValidator<TransferSettingsRequest>, TransferSettingsRequestValidator>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "jmbackup_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(security.SessionInactivityMinutes);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options => options.DefaultPolicy =
    new AuthorizationPolicyBuilder().AddRequirements(new CredentialRequirement()).Build());

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.SegmentsPerWindow = 4;
        limiterOptions.QueueLimit = 0;
    });
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };
});

builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHsts();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<AntiforgeryValidationMiddleware>();

app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new AntiforgeryTokenResponse(tokens.RequestToken ?? string.Empty));
}).AllowAnonymous();

app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapTaskGroupEndpoints();
app.MapTaskPathEndpoints();
app.MapExclusionEndpoints();
app.MapFilterEndpoints();
app.MapScheduleEndpoints();
app.MapCredentialEndpoints();
app.MapControlEndpoints();
app.MapRunEndpoints();
app.MapLogEndpoints();
app.MapSettingsEndpoints();

app.MapHub<ProgressHub>("/hubs/progress");
app.MapOpenApi();

// Interfaz web (fase 3): wwwroot lo escribe "npm run build" (vite.config.ts).
// MapFallbackToFile deja pasar /api y /hubs (ya mapeados arriba) y solo entrega
// index.html para rutas que React Router resuelve del lado del cliente.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

/// <summary>Fábrica mínima para leer <c>Settings</c> antes de que exista el contenedor de DI.</summary>
internal sealed class BootstrapDbContextFactory(DbContextOptions<JMBackupDbContext> options) : IDbContextFactory<JMBackupDbContext>
{
    public JMBackupDbContext CreateDbContext() => new(options);
}

// Expone la clase Program generada por las sentencias de nivel superior para que
// WebApplicationFactory<Program> pueda hospedar esta aplicación en las pruebas de integración.
public partial class Program;
