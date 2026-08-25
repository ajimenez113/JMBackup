/**
 * Puente hacia el shell de escritorio (fase 4): expone lo que el navegador no puede
 * hacer (diálogos nativos de archivo/carpeta, ruta absoluta real al soltar). Cuando la
 * app corre en un navegador común, `window.chrome.webview` no existe y todo esto queda
 * inutilizable — el código que llama a este módulo tiene que revisar
 * {@link isNativeBridgeAvailable} antes y ofrecer la alternativa del navegador.
 */

interface ChromeWebView {
  postMessage: (message: unknown) => void
  addEventListener: (type: 'message', listener: (event: MessageEvent) => void) => void
}

declare global {
  interface Window {
    chrome?: { webview?: ChromeWebView }
  }
}

interface BridgeResponse {
  requestId: string
  path?: string | null
  error?: string | null
}

interface DroppedPathsMessage {
  type: 'droppedPaths'
  paths: string[]
}

type DroppedPathsListener = (paths: string[]) => void

const pendingRequests = new Map<string, (response: BridgeResponse) => void>()
const droppedPathsListeners = new Set<DroppedPathsListener>()
let listenerRegistered = false

function getWebView(): ChromeWebView | undefined {
  return window.chrome?.webview
}

export function isNativeBridgeAvailable(): boolean {
  return getWebView() !== undefined
}

function ensureListenerRegistered(webview: ChromeWebView) {
  if (listenerRegistered) {
    return
  }
  listenerRegistered = true

  webview.addEventListener('message', (event) => {
    const data = event.data as Partial<BridgeResponse & DroppedPathsMessage> | undefined
    if (!data) {
      return
    }

    if (data.type === 'droppedPaths' && Array.isArray(data.paths)) {
      const paths = data.paths
      droppedPathsListeners.forEach((listener) => listener(paths))
      return
    }

    if (data.requestId) {
      const resolve = pendingRequests.get(data.requestId)
      if (resolve) {
        pendingRequests.delete(data.requestId)
        resolve(data as BridgeResponse)
      }
    }
  })
}

function sendBridgeRequest(type: 'pickFolder' | 'pickFile'): Promise<string | null> {
  const webview = getWebView()
  if (!webview) {
    return Promise.resolve(null)
  }

  ensureListenerRegistered(webview)

  const requestId = crypto.randomUUID()
  return new Promise((resolve) => {
    pendingRequests.set(requestId, (response) => resolve(response.path ?? null))
    webview.postMessage({ requestId, type })
  })
}

/** Abre el selector nativo de carpeta y devuelve la ruta absoluta elegida, o null si se canceló. */
export function pickFolder(): Promise<string | null> {
  return sendBridgeRequest('pickFolder')
}

/** Abre el selector nativo de archivo y devuelve la ruta absoluta elegida, o null si se canceló. */
export function pickFile(): Promise<string | null> {
  return sendBridgeRequest('pickFile')
}

/** Se suscribe a las rutas reales que llegan cuando se suelta algo sobre la ventana. Devuelve la función para desuscribirse. */
export function onDroppedPaths(listener: DroppedPathsListener): () => void {
  const webview = getWebView()
  if (webview) {
    ensureListenerRegistered(webview)
  }

  droppedPathsListeners.add(listener)
  return () => droppedPathsListeners.delete(listener)
}
