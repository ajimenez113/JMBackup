using Microsoft.AspNetCore.Mvc.Testing;

namespace JMBackup.Api.IntegrationTests;

/// <summary>
/// Hospeda JMBackup.Api para las pruebas de integración. <c>Program.cs</c> lee
/// <c>Settings</c> (credencial, dirección de escucha) ANTES de que exista el
/// contenedor de DI, para decidir el bind de Kestrel (ADR-007) — un
/// <c>ConfigureWebHost</c>/<c>ConfigureAppConfiguration</c> normal llega demasiado
/// tarde para eso, porque <c>WebApplicationFactory</c> solo intercepta la
/// configuración una vez que <c>Program.cs</c> ya la leyó. La única fuente que
/// <c>WebApplication.CreateBuilder</c> lee ANTES de que corra una sola línea de
/// <c>Program.cs</c> son las variables de entorno del proceso, así que se usa esa vía
/// para apuntar <c>Paths:DataDirectory</c> a una carpeta temporal — nunca toca la base
/// real de %ProgramData%\JMBackup. Las pruebas de esta clase comparten la colección
/// "ApiIntegrationTests" (ver <see cref="ApiIntegrationTestGroup"/>) para que
/// xUnit no las corra en paralelo: la variable de entorno es del proceso completo.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DataDirectoryVariable = "Paths__DataDirectory";

    private readonly DirectoryInfo _tempDataDirectory = Directory.CreateTempSubdirectory("jmbackup-apitests-");
    private readonly string? _previousValue = Environment.GetEnvironmentVariable(DataDirectoryVariable);

    public ApiWebApplicationFactory() => Environment.SetEnvironmentVariable(DataDirectoryVariable, _tempDataDirectory.FullName);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable(DataDirectoryVariable, _previousValue);

            try
            {
                _tempDataDirectory.Delete(recursive: true);
            }
            catch (IOException)
            {
                // Un handle todavía abierto no debe romper la limpieza de la prueba.
            }
        }

        base.Dispose(disposing);
    }
}
