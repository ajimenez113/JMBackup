import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { onProgress, onRunFinished, type ProgressMessage } from '../../lib/signalr'
import { SUMMARY_QUERY_KEY } from './useTasks'

/**
 * Progreso en vivo por tarea (RF-03), tal como llega del hub SignalR. El estado
 * "de verdad" (última ejecución, resultado) lo sigue teniendo el backend — esto es
 * solo lo que se ve mientras una tarea está corriendo.
 *
 * Sin el evento "runFinished", esta tarea se quedaba con la última entrada de
 * progreso reportada, mostrándose como "corriendo" indefinidamente incluso mucho
 * después de que terminara — hasta que algo (cambiar de pantalla) remontaba el
 * componente y reseteaba este estado.
 */
export function useLiveProgress(): Map<number, ProgressMessage> {
  const queryClient = useQueryClient()
  const [progressByTask, setProgressByTask] = useState<Map<number, ProgressMessage>>(new Map())

  useEffect(
    () =>
      onProgress((message) => {
        setProgressByTask((previous) => {
          const next = new Map(previous)
          next.set(message.taskId, message)
          return next
        })
      }),
    [],
  )

  useEffect(
    () =>
      onRunFinished((message) => {
        setProgressByTask((previous) => {
          if (!previous.has(message.taskId)) {
            return previous
          }

          const next = new Map(previous)
          next.delete(message.taskId)
          return next
        })
        void queryClient.invalidateQueries({ queryKey: SUMMARY_QUERY_KEY })
      }),
    [queryClient],
  )

  return progressByTask
}
