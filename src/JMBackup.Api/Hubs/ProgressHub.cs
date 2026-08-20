using Microsoft.AspNetCore.SignalR;

namespace JMBackup.Api.Hubs;

/// <summary>RF-03: progreso en vivo. El cliente solo escucha; no expone métodos invocables por ahora.</summary>
public sealed class ProgressHub : Hub;
