import { useEffect, useState } from 'react'
import type { ConnectionStatusResponse } from '../../../lib/api-client'
import { useTestConnection } from '../usePaths'

const RECHECK_INTERVAL_MS = 60_000

export type ConnectivityState = ConnectionStatusResponse | 'checking'

/**
 * RF-22: se verifica al agregar la ruta y cada 60 segundos mientras esta pestaña está
 * abierta (acá hace las veces del diálogo del asistente, que en esta interfaz es una
 * pestaña en vez de una ventana aparte).
 */
export function useConnectivityCheck(taskId: number, pathIds: number[]) {
  const [statusByPathId, setStatusByPathId] = useState<Map<number, ConnectivityState>>(new Map())
  const testConnection = useTestConnection(taskId)

  useEffect(() => {
    let cancelled = false

    async function checkAll() {
      for (const pathId of pathIds) {
        if (cancelled) {
          return
        }

        setStatusByPathId((previous) => new Map(previous).set(pathId, 'checking'))
        try {
          const status = await testConnection.mutateAsync(pathId)
          if (!cancelled) {
            setStatusByPathId((previous) => new Map(previous).set(pathId, status))
          }
        } catch {
          if (!cancelled) {
            setStatusByPathId((previous) =>
              new Map(previous).set(pathId, { isConnected: false, reason: 'Unknown', detail: undefined }),
            )
          }
        }
      }
    }

    void checkAll()
    const interval = setInterval(() => void checkAll(), RECHECK_INTERVAL_MS)

    return () => {
      cancelled = true
      clearInterval(interval)
    }
    // Se compara pathIds por valor (join), no por identidad del array, para no
    // reiniciar el temporizador de 60s en cada render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [taskId, pathIds.join(',')])

  return statusByPathId
}
