using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JMBackup.Desktop.Lock;

/// <summary>
/// RF-104: bloqueo nativo de la ventana, independiente de la sesión web que corre
/// adentro del WebView2. Solo confirma que quien está frente a la pantalla conoce la
/// credencial — no inicia sesión web por su cuenta (esa cookie vive en el perfil del
/// WebView2, no acá) ni la cierra: la sesión web sigue con el estado que ya tenía.
/// </summary>
public partial class LockOverlay : UserControl
{
    public event EventHandler? Unlocked;

    private int _port;

    public LockOverlay()
    {
        InitializeComponent();
    }

    public void Show(int port)
    {
        _port = port;
        UsernameBox.Text = string.Empty;
        PasswordBox.Password = string.Empty;
        ErrorText.Visibility = Visibility.Collapsed;
        Visibility = Visibility.Visible;
        UsernameBox.Focus();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = TryUnlockAsync();
        }
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e) => _ = TryUnlockAsync();

    private async Task TryUnlockAsync()
    {
        UnlockButton.IsEnabled = false;
        ErrorText.Visibility = Visibility.Collapsed;

        try
        {
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var httpClient = new HttpClient(handler);
            using var response = await httpClient.PostAsJsonAsync(
                new Uri($"https://127.0.0.1:{_port}/api/auth/login"),
                new { username = UsernameBox.Text, password = PasswordBox.Password });

            if (response.IsSuccessStatusCode)
            {
                Visibility = Visibility.Collapsed;
                Unlocked?.Invoke(this, EventArgs.Empty);
                return;
            }

            ErrorText.Text = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                ? "Demasiados intentos. Esperá antes de volver a intentar."
                : "Usuario o contraseña incorrectos.";
            ErrorText.Visibility = Visibility.Visible;
        }
        catch (HttpRequestException)
        {
            ErrorText.Text = "No se pudo contactar al servicio.";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            UnlockButton.IsEnabled = true;
        }
    }
}
