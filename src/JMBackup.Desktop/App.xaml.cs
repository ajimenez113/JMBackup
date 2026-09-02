using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace JMBackup.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App() => DispatcherUnhandledException += OnDispatcherUnhandledException;

    /// <summary>
    /// Sin este manejador, cualquier excepción no atrapada durante el arranque —el
    /// caso real que motivó esto: el runtime de WebView2 no instalado en el equipo
    /// destino— cierra la aplicación en silencio, sin ninguna ventana ni mensaje. Se
    /// muestra el error real y se cierra de forma controlada, en vez de un cierre
    /// mudo que no deja ninguna pista de qué pasó.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"JMBackup encontró un error inesperado y tiene que cerrarse:\n\n{e.Exception.Message}",
            "JMBackup — Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }
}

