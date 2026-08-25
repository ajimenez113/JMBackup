import { useState } from 'react'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Select } from '../../components/Select'
import { StatusDot, type Status } from '../../components/StatusDot'
import { Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '../../components/Table'
import { useStrings } from '../../i18n'
import { LogsPanel } from './LogsPanel'
import { useRuns, useTasksForFilter } from './useRuns'

const STATUS_MAP: Record<string, Status> = {
  Completed: 'ok',
  CompletedWithErrors: 'warning',
  Failed: 'error',
  Cancelled: 'warning',
  Running: 'running',
}

const RESULT_LABEL_KEYS: Record<string, 'statusOk' | 'statusWithErrors' | 'statusFailed' | 'statusCancelled' | 'statusRunning'> = {
  Completed: 'statusOk',
  CompletedWithErrors: 'statusWithErrors',
  Failed: 'statusFailed',
  Cancelled: 'statusCancelled',
  Running: 'statusRunning',
}

function formatDuration(startedAt: string, finishedAt: string | undefined): string {
  if (!finishedAt) {
    return '—'
  }

  const seconds = Math.round((new Date(finishedAt).getTime() - new Date(startedAt).getTime()) / 1000)
  const minutes = Math.floor(seconds / 60)
  const remainingSeconds = seconds % 60
  return minutes > 0 ? `${minutes}m ${remainingSeconds}s` : `${remainingSeconds}s`
}

export function HistoryPage() {
  const strings = useStrings()
  const tasksQuery = useTasksForFilter()

  const [taskId, setTaskId] = useState<string>('all')
  const [from, setFrom] = useState('')
  const [until, setUntil] = useState('')
  const [selectedRunId, setSelectedRunId] = useState<number>()

  const runsQuery = useRuns(
    taskId === 'all' ? undefined : Number(taskId),
    from ? new Date(from).toISOString() : undefined,
    until ? new Date(until).toISOString() : undefined,
  )
  const runs = runsQuery.data ?? []
  const tasksById = new Map((tasksQuery.data ?? []).map((task) => [task.id, task.name]))

  const csvParams = new URLSearchParams()
  if (taskId !== 'all') {
    csvParams.set('taskId', taskId)
  }
  if (from) {
    csvParams.set('from', new Date(from).toISOString())
  }
  if (until) {
    csvParams.set('until', new Date(until).toISOString())
  }

  return (
    <div className="p-4">
      <Card title={strings.history.title}>
        <div className="mb-4 flex flex-wrap items-end gap-3">
          <Select
            label={strings.history.filterTask}
            value={taskId}
            onValueChange={setTaskId}
            options={[
              { value: 'all', label: strings.history.filterAllTasks },
              ...(tasksQuery.data ?? []).map((task) => ({ value: String(task.id), label: task.name })),
            ]}
          />
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-fg">{strings.history.filterFrom}</span>
            <input
              type="date"
              value={from}
              onChange={(event) => setFrom(event.target.value)}
              className="rounded-jm border border-brand bg-bg-elevated px-3 py-2 text-sm text-fg"
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-fg">{strings.history.filterUntil}</span>
            <input
              type="date"
              value={until}
              onChange={(event) => setUntil(event.target.value)}
              className="rounded-jm border border-brand bg-bg-elevated px-3 py-2 text-sm text-fg"
            />
          </label>
          <a
            href={`/api/runs/export.csv?${csvParams.toString()}`}
            className="inline-flex items-center justify-center gap-2 rounded-jm border border-brand bg-transparent px-4 py-2
              text-sm font-medium text-accent hover:bg-border-subtle"
          >
            {strings.history.exportCsv}
          </a>
        </div>

        {runs.length === 0 ? (
          <p className="text-sm text-fg-muted">{strings.history.empty}</p>
        ) : (
          <Table>
            <TableHead>
              <TableRow>
                <TableHeaderCell>{strings.history.columnTask}</TableHeaderCell>
                <TableHeaderCell>{strings.history.columnStartedAt}</TableHeaderCell>
                <TableHeaderCell>{strings.history.columnFinishedAt}</TableHeaderCell>
                <TableHeaderCell>{strings.history.columnDuration}</TableHeaderCell>
                <TableHeaderCell>{strings.history.columnResult}</TableHeaderCell>
                <TableHeaderCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {runs.map((run) => (
                <TableRow key={run.id}>
                  <TableCell>{tasksById.get(run.taskId) ?? run.taskId}</TableCell>
                  <TableCell>{new Date(run.startedAt).toLocaleString()}</TableCell>
                  <TableCell>{run.finishedAt ? new Date(run.finishedAt).toLocaleString() : '—'}</TableCell>
                  <TableCell>{formatDuration(run.startedAt, run.finishedAt)}</TableCell>
                  <TableCell>
                    <StatusDot status={STATUS_MAP[run.status] ?? 'idle'} label={strings.tasks[RESULT_LABEL_KEYS[run.status] ?? 'statusOk']} />
                  </TableCell>
                  <TableCell>
                    <Button variant="ghost" size="sm" onClick={() => setSelectedRunId(run.id)}>
                      {strings.history.viewLogs}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      <LogsPanel runId={selectedRunId} onOpenChange={(open) => !open && setSelectedRunId(undefined)} />
    </div>
  )
}
