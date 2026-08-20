using System.Net;
using JMBackup.Domain.Enums;
using JMBackup.Domain.Exceptions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;

namespace JMBackup.Infrastructure.Windows;

/// <summary>
/// Conecta y desconecta recursos UNC con credenciales, vía <c>WNetAddConnection2</c>
/// (ADR-008). Una vez conectado, cualquier acceso posterior por <c>System.IO</c> a ese
/// recurso ya usa la sesión autenticada a nivel de proceso: por eso
/// <c>LocalStorageBackend</c> no necesita saber nada de credenciales — es quien arma la
/// ejecución de la tarea (JMBackup.Cli en el hito 1) quien conecta el recurso antes de
/// arrancar y lo desconecta al final.
/// </summary>
public static class WNetShareConnector
{
    public static unsafe void Connect(string uncRoot, NetworkCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uncRoot);
        ArgumentNullException.ThrowIfNull(credential);

        var userName = string.IsNullOrEmpty(credential.Domain)
            ? credential.UserName
            : $"{credential.Domain}\\{credential.UserName}";

        fixed (char* remoteNamePtr = uncRoot)
        {
            var resource = new NETRESOURCEW
            {
                dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK,
                lpRemoteName = remoteNamePtr,
            };

            var result = PInvoke.WNetAddConnection2W(resource, credential.Password, userName, 0);
            if (result != WIN32_ERROR.NO_ERROR)
            {
                throw new StorageOperationException(
                    MapErrorReason(result),
                    uncRoot,
                    $"No se pudo conectar el recurso de red \"{uncRoot}\" (código {(uint)result}).");
            }
        }
    }

    public static void Disconnect(string uncRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uncRoot);
        PInvoke.WNetCancelConnection2W(uncRoot, 0, true);
    }

    private static StorageErrorReason MapErrorReason(WIN32_ERROR error) => error switch
    {
        WIN32_ERROR.ERROR_ACCESS_DENIED => StorageErrorReason.InvalidCredentials,
        WIN32_ERROR.ERROR_LOGON_FAILURE => StorageErrorReason.InvalidCredentials,
        WIN32_ERROR.ERROR_BAD_NETPATH => StorageErrorReason.PathNotFound,
        WIN32_ERROR.ERROR_BAD_NET_NAME => StorageErrorReason.PathNotFound,
        _ => StorageErrorReason.HostUnreachable,
    };
}
