using System.Net;

namespace JMBackup.Application.Abstractions;

/// <summary>
/// Conecta y desconecta recursos UNC con credenciales (ADR-008). Infrastructure la
/// implementa con <c>WNetAddConnection2</c> vía CsWin32; Application no puede
/// referenciar Windows directamente.
/// </summary>
public interface INetworkShareConnector
{
    void Connect(string uncRoot, NetworkCredential credential);

    void Disconnect(string uncRoot);
}
