import type { Status } from '../../components/StatusDot'
import type { TaskSummaryResponse } from '../../lib/api-client'
import type { ProgressMessage } from '../../lib/signalr'

export interface TaskHealth {
  status: Status
  rpoAlert: boolean
}

/**
 * RF-04 pide alertar cuando una tarea "lleva más tiempo del esperado sin éxito". El
 * resumen de tareas no trae el intervalo de su horario (agregarlo es trabajo de la
 * fase 3B/3C, cuando el asistente de tarea exponga horarios en detalle), así que acá
 * se usan dos señales que sí están disponibles y que en la práctica cubren lo mismo:
 * el último resultado conocido no fue un éxito limpio, o la tarea ya pasó la hora en
 * la que Quartz tenía planificado el próximo disparo sin que corriera.
 */
export function computeTaskHealth(task: TaskSummaryResponse, liveProgress: ProgressMessage | undefined): TaskHealth {
  if (liveProgress) {
    return { status: 'running', rpoAlert: false }
  }

  if (!task.enabled) {
    return { status: 'idle', rpoAlert: false }
  }

  if (!task.lastRun) {
    return { status: 'idle', rpoAlert: true }
  }

  const lastRunFailed = task.lastRun.status === 'Failed' || task.lastRun.status === 'CompletedWithErrors'
  const missedNextRun = task.nextRunAtUtc !== undefined && new Date(task.nextRunAtUtc).getTime() < Date.now()
  const rpoAlert = lastRunFailed || missedNextRun

  if (task.lastRun.status === 'Failed') {
    return { status: 'error', rpoAlert }
  }

  if (task.lastRun.status === 'CompletedWithErrors' || task.lastRun.status === 'Cancelled') {
    return { status: 'warning', rpoAlert }
  }

  return { status: rpoAlert ? 'warning' : 'ok', rpoAlert }
}
