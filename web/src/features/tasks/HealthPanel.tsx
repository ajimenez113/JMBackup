import { Card } from '../../components/Card'
import { StatusDot } from '../../components/StatusDot'
import { useStrings } from '../../i18n'
import type { TaskSummaryResponse } from '../../lib/api-client'
import { computeTaskHealth } from './taskHealth'
import type { ProgressMessage } from '../../lib/signalr'

interface HealthPanelProps {
  tasks: TaskSummaryResponse[]
  liveProgress: Map<number, ProgressMessage>
}

export function HealthPanel({ tasks, liveProgress }: HealthPanelProps) {
  const strings = useStrings()
  const alerts = tasks.filter((task) => computeTaskHealth(task, liveProgress.get(task.id)).rpoAlert)

  return (
    <Card title={strings.health.title} className="mb-4">
      {alerts.length === 0 ? (
        <p className="text-sm text-fg-muted">{strings.health.allHealthy}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {alerts.map((task) => (
            <li key={task.id} className="flex items-center justify-between gap-3 text-sm">
              <span className="flex items-center gap-2 text-fg">
                <StatusDot status="warning" />
                {task.name}
              </span>
              <span className="text-fg-muted">
                {task.lastRun
                  ? `${strings.health.lastSuccess}: ${new Date(task.lastRun.startedAt).toLocaleString()}`
                  : strings.tasks.neverRun}
              </span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
