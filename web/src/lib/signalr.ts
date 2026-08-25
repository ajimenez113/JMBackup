import { HttpTransportType, HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'

export interface ProgressMessage {
  taskId: number
  runId: number
  currentPath: string | null
  filesCompleted: number
  filesTotal: number
  bytesCompleted: number
  bytesTotal: number
  filesOk: number
  filesFailed: number
  bytesPerSecond: number
  estimatedTimeRemaining: string | null
}

let connection: HubConnection | null = null

/** Una única conexión compartida para toda la app, con reconexión automática. */
export function getProgressConnection(): HubConnection {
  connection ??= new HubConnectionBuilder()
    .withUrl('/hubs/progress', { transport: HttpTransportType.WebSockets, withCredentials: true })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build()

  return connection
}

export interface RunFinishedMessage {
  taskId: number
  runId: number
}

export function onProgress(handler: (message: ProgressMessage) => void): () => void {
  const hub = getProgressConnection()
  hub.on('progress', handler)

  if (hub.state === 'Disconnected') {
    void hub.start()
  }

  return () => hub.off('progress', handler)
}

/** RF-03: avisa que una ejecución terminó, para dejar de mostrarla como "corriendo" sin esperar al próximo sondeo. */
export function onRunFinished(handler: (message: RunFinishedMessage) => void): () => void {
  const hub = getProgressConnection()
  hub.on('runFinished', handler)

  if (hub.state === 'Disconnected') {
    void hub.start()
  }

  return () => hub.off('runFinished', handler)
}
