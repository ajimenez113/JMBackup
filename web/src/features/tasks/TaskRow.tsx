import { useNavigate } from 'react-router-dom'
import { Button } from '../../components/Button'
import { ProgressBar } from '../../components/ProgressBar'
import { SpinnerIcon } from '../../components/SpinnerIcon'
import { StatusDot } from '../../components/StatusDot'
import { useStrings } from '../../i18n'
import type { TaskSummaryResponse } from '../../lib/api-client'
import type { ProgressMessage } from '../../lib/signalr'
import { computeTaskHealth } from './taskHealth'

interface TaskRowProps {
  task: TaskSummaryResponse
  liveProgress: ProgressMessage | undefined
  selected: boolean
  onSelect: () => void
}

const RESULT_LABEL_KEYS = {
  Completed: 'statusOk',
  CompletedWithErrors: 'statusWithErrors',
  Failed: 'statusFailed',
  Cancelled: 'statusCancelled',
  Running: 'statusRunning',
} as const

export function TaskRow({ task, liveProgress, selected, onSelect }: TaskRowProps) {
  const strings = useStrings()
  const navigate = useNavigate()
  const health = computeTaskHealth(task, liveProgress)

  const resultLabel = liveProgress
    ? strings.tasks.statusRunning
    : task.lastRun
      ? strings.tasks[RESULT_LABEL_KEYS[task.lastRun.status as keyof typeof RESULT_LABEL_KEYS] ?? 'statusOk']
      : strings.tasks.neverRun

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onSelect}
      onKeyDown={(event) => (event.key === 'Enter' || event.key === ' ') && onSelect()}
      className={`flex flex-col gap-2 border-b border-border-subtle px-4 py-3 last:border-b-0 cursor-pointer
        hover:bg-border-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent
        ${selected ? 'bg-border-subtle' : ''}`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <StatusDot status={health.status} />
          <span className="font-medium text-fg">{task.name}</span>
          {!task.enabled ? <span className="text-xs text-fg-muted">({strings.tasks.disabled})</span> : null}
          <Button
            variant="ghost"
            size="sm"
            onClick={(event) => {
              event.stopPropagation()
              navigate(`/tasks/${task.id}/edit`)
            }}
          >
            {strings.wizard.edit}
          </Button>
        </div>
        <div className="flex flex-wrap items-center gap-4 text-xs text-fg-muted">
          <span className="flex items-center gap-1.5">
            {resultLabel}
            {liveProgress ? <SpinnerIcon label={strings.tasks.statusRunning} /> : null}
          </span>
          <span>
            {strings.tasks.columnLastRun}: {task.lastRun ? new Date(task.lastRun.startedAt).toLocaleString() : strings.tasks.neverRun}
          </span>
          <span>
            {strings.tasks.columnNextRun}: {task.nextRunAtUtc ? new Date(task.nextRunAtUtc).toLocaleString() : strings.tasks.notScheduled}
          </span>
        </div>
      </div>
      {liveProgress ? (
        <ProgressBar
          value={liveProgress.bytesCompleted}
          max={liveProgress.bytesTotal}
          label={`${liveProgress.filesOk} ${strings.progress.filesOk} · ${liveProgress.filesFailed} ${strings.progress.filesFailed}`}
        />
      ) : null}
    </div>
  )
}
