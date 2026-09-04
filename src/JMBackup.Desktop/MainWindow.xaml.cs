using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using JMBackup.Desktop.Bridge;
using JMBackup.Desktop.Services;
using Microsoft.Web.WebView2.Core;

namespace JMBackup.Desktop;

public partial class MainWindow : Window
{
    private const int Port = 8483;

    // Espejo de JMBackup.Api.Authentication.CredentialRequirement.DesktopClientHeaderName:
    // el shell no referencia el proyecto de la API (son procesos separados, se instalan
    // por separado), así que el nombre del header es un contrato compartido, no un tipo.
    private const string DesktopClientHeaderName = "X-JMBackup-Client";
    private const string DesktopClientHeaderValue = "Desktop";

    private static readonly Uri ExpectedOrigin = new($"https://127.0.0.1:{Port}");

    // Las dos variantes de color del ícono de la bandeja según el estado de salud del
    // servicio (ver UpdateTrayHealthAsync) — antes era un solo GeneratedIconSource de
    // texto ("J") al que se le cambiaba el color de fondo; ahora son dos íconos reales.
    private static readonly BitmapImage TrayIconOk = new(new Uri("pack://application:,,,/Assets/tray-ok.png"));
    private static readonly BitmapImage TrayIconAlert = new(new Uri("pack://application:,,,/Assets/tray-alert.png"));

    private readonly ServiceLauncher _serviceLauncher = new(Port);
    private readonly DispatcherTimer _healthTimer = new() { Interval = TimeSpan.FromSeconds(15) };

    private NativeBridgeHandler? _bridge;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeAsync();
        _healthTimer.Tick += async (_, _) => await UpdateTrayHealthAsync();
    }

    private async Task InitializeAsync()
    {
        StartupText.Text = "Buscando el servicio…";
        var isRunning = await _serviceLauncher.EnsureRunningAsync(CancellationToken.None);
        if (!isRunning)
        {
            StartupText.Text = "No se pudo iniciar el servicio JMBackup. Revisá que esté instalado o que\n" +
                "JMBackup.Api.exe esté en la misma carpeta que esta aplicación.";
            return;
        }

        if (!IsWebView2RuntimeAvailable())
        {
            StartupText.Text = "Falta instalar el WebView2 Runtime de Microsoft Edge en este equipo.\n" +
                "Descargalo desde https://go.microsoft.com/fwlink/p/?LinkId=2124703 e instalalo,\n" +
                "después volvé a abrir JMBackup.";
            return;
        }

        StartupText.Text = "Cargando la interfaz…";
        await WebView.EnsureCoreWebView2Async(await CreateWebView2EnvironmentAsync());
        ConfigureCoreWebView2(WebView.CoreWebView2);

        _bridge = new NativeBridgeHandler(WebView.CoreWebView2, ExpectedOrigin);

        AllowDrop = true;
        WebView.AllowDrop = true;
        PreviewDrop += MainWindow_PreviewDrop;

        if (await IsLockRequiredAsync())
        {
            LockOverlay.Show(Port);
        }

        WebView.CoreWebView2.Navigate(ExpectedOrigin.ToString());
        StartupOverlay.Visibility = Visibility.Collapsed;

        _healthTimer.Start();
        await UpdateTrayHealthAsync();
    }

    private void ConfigureCoreWebView2(CoreWebView2 coreWebView)
    {
        // RF-113: el certificado es el autofirmado propio, generado por
        // SelfSignedCertificateProvider — se acepta solo para nuestro propio backend
        // local, nunca en general (eso sería confiar en cualquier certificado inválido
        // que el WebView2 encuentre, un problema de seguridad real).
        coreWebView.ServerCertificateErrorDetected += (_, args) =>
        {
            args.Action = Uri.TryCreate(args.RequestUri, UriKind.Absolute, out var requestUri)
                && requestUri.Host == ExpectedOrigin.Host
                && requestUri.Port == ExpectedOrigin.Port
                ? CoreWebView2ServerCertificateErrorAction.AlwaysAllow
                : CoreWebView2ServerCertificateErrorAction.Cancel;
        };

        // Fase 4 (RF-101/RF-104): identifica al shell de escritorio ante la API, para
        // que CredentialRequirementHandler pueda exigir la credencial "solo
        // aplicación" o "solo web" por separado.
        coreWebView.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        coreWebView.WebResourceRequested += (_, args) =>
            args.Request.Headers.SetHeader(DesktopClientHeaderName, DesktopClientHeaderValue);

        // RF-23: deja que WPF maneje el drop en vez del propio contenido web, para
        // poder leer la ruta absoluta real (ver MainWindow_PreviewDrop).
        WebView.AllowExternalDrop = false;
    }

    private void MainWindow_PreviewDrop(object sender, DragEventArgs e)
    {
        if (_bridge is null || !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
        {
            _bridge.PostDroppedPaths(paths);
        }

        e.Handled = true;
    }

    /// <summary>
    /// JMBackup usa el modelo "Evergreen" de WebView2 (<c>Microsoft.Web.WebView2</c> sin
    /// <c>FixedVersion</c>): el control .NET viaja en el ejecutable, pero el motor de
    /// renderizado en sí lo tiene que tener instalado el sistema operativo. Viene con
    /// Windows 11 y con Edge en la mayoría de instalaciones de Windows 10, pero NO con
    /// Windows Server por defecto — sin este chequeo explícito,
    /// <c>EnsureCoreWebView2Async</c> tira <c>WebView2RuntimeNotFoundException</c> sin
    /// atrapar, adentro de un manejador de evento <c>async void</c>, lo que cierra toda
    /// la aplicación sin abrir ninguna ventana ni mostrar ningún mensaje.
    /// </summary>
    private static bool IsWebView2RuntimeAvailable()
    {
        try
        {
            CoreWebView2Environment.GetAvailableBrowserVersionString();
            return true;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sin esto, WebView2 usa por defecto una carpeta de datos de usuario al lado del
    /// propio ejecutable — que en una instalación normal es
    /// <c>C:\Program Files\JMBackup\desktop\</c>, donde una cuenta sin privilegios de
    /// administrador no tiene permiso de escritura. El síntoma real fue
    /// "Acceso denegado (0x80070005 E_ACCESSDENIED)" al cargar la interfaz. La carpeta
    /// de datos de usuario de <c>%LocalAppData%</c> es del usuario que abre la app, así
    /// que siempre tiene permiso de escritura ahí, esté instalado donde esté JMBackup.
    /// </summary>
    private static Task<CoreWebView2Environment> CreateWebView2EnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JMBackup", "WebView2");
        return CoreWebView2Environment.CreateAsync(browserExecutableFolder: null, userDataFolder);
    }

    private static async Task<bool> IsLockRequiredAsync()
    {
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
        client.DefaultRequestHeaders.Add(DesktopClientHeaderName, DesktopClientHeaderValue);

        try
        {
            using var response = await client.GetAsync(new Uri($"{ExpectedOrigin}/api/settings/security"));
            return response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private async Task UpdateTrayHealthAsync()
    {
        var isRunning = await _serviceLauncher.EnsureRunningAsync(CancellationToken.None);
        TrayIcon.IconSource = isRunning ? TrayIconOk : TrayIconAlert;
        TrayIcon.ToolTipText = isRunning ? "JMBackup — el servicio está corriendo" : "JMBackup — el servicio no responde";
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        // Cerrar la ventana la manda a la bandeja, no cierra la aplicación — igual que
        // cualquier app de bandeja: "Salir" del menú es la única forma de terminarla.
        e.Cancel = true;
        Hide();
        await Task.CompletedTask;
    }

    private async void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();

        if (await IsLockRequiredAsync())
        {
            LockOverlay.Show(Port);
        }
    }

    private void TrayIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e) => ShowWindow();

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => ShowWindow();

    private async void RunAllMenuItem_Click(object sender, RoutedEventArgs e) => await ExecuteViaWebViewAsync("/api/tasks/run-all");

    private async void PauseMenuItem_Click(object sender, RoutedEventArgs e) => await ExecuteViaWebViewAsync("/api/tasks/pause-all");

    /// <summary>
    /// El clic de bandeja no tiene una sesión HTTP propia: reutiliza la del WebView2
    /// ejecutando el mismo fetch que haría la interfaz, adentro de su contexto — así
    /// hereda la cookie de sesión y el token de antiforgery sin duplicar ninguno de
    /// los dos del lado nativo.
    /// </summary>
    private async Task ExecuteViaWebViewAsync(string path)
    {
        if (WebView.CoreWebView2 is null)
        {
            return;
        }

        var script = $$"""
            fetch('/api/antiforgery/token')
                .then(r => r.json())
                .then(t => fetch('{{path}}', { method: 'POST', headers: { 'X-XSRF-TOKEN': t.token } }));
            """;

        await WebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _isExiting = true;
        Application.Current.Shutdown();
    }
}
