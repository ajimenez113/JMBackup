using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;

namespace JMBackup.Desktop.Services;

/// <summary>
/// Detecta si el backend responde en <c>https://127.0.0.1:&lt;puerto&gt;</c> y, si no,
/// lo arranca: primero intenta el servicio de Windows "JMBackup" (si está instalado,
/// <c>build/Install-JMBackupService.ps1</c>), y si no existe o no se puede arrancar
/// (por ejemplo, sin privilegios para controlar servicios), lanza
/// <c>JMBackup.Api.exe</c> directamente desde la misma carpeta que este ejecutable —
/// la instalación manual sin servicio registrado (README) también tiene que andar.
/// </summary>
public sealed class ServiceLauncher(int port)
{
    private const string ServiceName = "JMBackup";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartupWaitTimeout = TimeSpan.FromSeconds(20);

    public async Task<bool> EnsureRunningAsync(CancellationToken cancellationToken)
    {
        if (await IsRespondingAsync(cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        if (!TryStartWindowsService() && !TryLaunchExecutableDirectly())
        {
            return false;
        }

        var deadline = DateTime.UtcNow + StartupWaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsRespondingAsync(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<bool> IsRespondingAsync(CancellationToken cancellationToken)
    {
        using var handler = new HttpClientHandler
        {
            // El certificado es el autofirmado propio (SelfSignedCertificateProvider):
            // esto solo sondea que el backend responde, la aceptación real del
            // certificado dentro del WebView2 la hace MainWindow con el mismo criterio
            // acotado (ver ServerCertificateErrorDetected).
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        using var client = new HttpClient(handler) { Timeout = ProbeTimeout };

        try
        {
            using var response = await client.GetAsync(new Uri($"https://127.0.0.1:{port}/api/auth/session"), cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static bool TryStartWindowsService()
    {
        try
        {
            using var controller = new ServiceController(ServiceName);
            if (controller.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            {
                return true;
            }

            controller.Start();
            return true;
        }
        catch (InvalidOperationException)
        {
            // El servicio no está instalado.
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Sin privilegios para controlar el servicio.
            return false;
        }
    }

    private static bool TryLaunchExecutableDirectly()
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (executableDirectory is null)
        {
            return false;
        }

        var apiExecutablePath = Path.Combine(executableDirectory, "JMBackup.Api.exe");
        if (!File.Exists(apiExecutablePath))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(apiExecutablePath) { UseShellExecute = false });
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
