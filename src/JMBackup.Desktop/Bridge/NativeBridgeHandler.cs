using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace JMBackup.Desktop.Bridge;

/// <summary>
/// Puente nativo (fase 4): lo que el navegador no puede hacer en el hito 1 —
/// diálogos nativos de archivo/carpeta y arrastrar y soltar con la ruta absoluta
/// real. Es superficie de confianza (CLAUDE.md §6): valida el origen de cada mensaje
/// contra la URL exacta del backend, y trata cada mensaje como si viniera de un
/// atacante, no de la propia interfaz.
/// </summary>
public sealed class NativeBridgeHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CoreWebView2 _coreWebView;
    private readonly Uri _expectedOrigin;

    public NativeBridgeHandler(CoreWebView2 coreWebView, Uri expectedOrigin)
    {
        _coreWebView = coreWebView;
        _expectedOrigin = expectedOrigin;
        _coreWebView.WebMessageReceived += OnWebMessageReceived;
    }

    /// <summary>Manda rutas reales sueltas sobre la ventana al contenido web (ver MainWindow, AllowExternalDrop).</summary>
    public void PostDroppedPaths(IReadOnlyList<string> paths)
    {
        var message = new DroppedPathsMessage { Paths = paths };
        _coreWebView.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOptions));
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // El puente es superficie de confianza: todo mensaje se valida como si viniera
        // de un atacante. Un origen que no sea exactamente el backend esperado se
        // descarta sin procesar nada — no hay "modo degradado" ni intento de adivinar
        // qué quiso decir.
        if (!IsExpectedOrigin(e.Source))
        {
            return;
        }

        BridgeRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(e.WebMessageAsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (request is null || string.IsNullOrEmpty(request.RequestId))
        {
            return;
        }

        var response = request.Type switch
        {
            "pickFolder" => HandlePickFolder(request.RequestId),
            "pickFile" => HandlePickFile(request.RequestId),
            _ => new BridgeResponse { RequestId = request.RequestId, Error = "Tipo de mensaje no reconocido." },
        };

        _coreWebView.PostWebMessageAsJson(JsonSerializer.Serialize(response, JsonOptions));
    }

    private bool IsExpectedOrigin(string source) =>
        Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
        && Uri.Compare(sourceUri, _expectedOrigin, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.Ordinal) == 0;

    private static BridgeResponse HandlePickFolder(string requestId)
    {
        var dialog = new OpenFolderDialog { Title = "Elegir carpeta" };
        return dialog.ShowDialog() == true
            ? new BridgeResponse { RequestId = requestId, Path = dialog.FolderName }
            : new BridgeResponse { RequestId = requestId, Path = null };
    }

    private static BridgeResponse HandlePickFile(string requestId)
    {
        var dialog = new OpenFileDialog { Title = "Elegir archivo", CheckFileExists = true };
        return dialog.ShowDialog() == true
            ? new BridgeResponse { RequestId = requestId, Path = dialog.FileName }
            : new BridgeResponse { RequestId = requestId, Path = null };
    }
}
