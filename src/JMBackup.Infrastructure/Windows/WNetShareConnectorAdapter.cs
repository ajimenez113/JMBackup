using System.Net;
using JMBackup.Application.Abstractions;

namespace JMBackup.Infrastructure.Windows;

/// <summary>Adapta la clase estática <see cref="WNetShareConnector"/> a <see cref="INetworkShareConnector"/> para poder inyectarla.</summary>
public sealed class WNetShareConnectorAdapter : INetworkShareConnector
{
    public void Connect(string uncRoot, NetworkCredential credential) => WNetShareConnector.Connect(uncRoot, credential);

    public void Disconnect(string uncRoot) => WNetShareConnector.Disconnect(uncRoot);
}
