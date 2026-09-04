import { useState } from 'react'
import { Card } from '../../components/Card'
import { useStrings } from '../../i18n'
import type { TaskGroupResponse, TaskSummaryResponse } from '../../lib/api-client'
import type { ProgressMessage } from '../../lib/signalr'
import { TaskRow } from './TaskRow'

interface TaskGroupListProps {
  tasks: TaskSummaryResponse[]
  groups: TaskGroupResponse[]
  liveProgress: Map<number, ProgressMessage>
  selectedTaskId: number | undefined
  onSelectTask: (taskId: number) => void
}

interface GroupBucket {
  key: string
  label: string
  tasks: TaskSummaryResponse[]
}

function groupTasks(tasks: TaskSummaryResponse[], groups: TaskGroupResponse[], ungroupedLabel: string): GroupBucket[] {
  const groupsById = new Map(groups.map((group) => [group.id, group]))
  const buckets = new Map<string, GroupBucket>()

  for (const task of tasks) {
    const group = task.groupId !== undefined ? groupsById.get(task.groupId) : undefined
    const key = group ? String(group.id) : 'ungrouped'
    const label = group?.name ?? ungroupedLabel

    const bucket = buckets.get(key) ?? { key, label, tasks: [] }
    bucket.tasks.push(task)
    buckets.set(key, bucket)
  }

  return [...buckets.values()]
}

const COLLAPSED_GROUPS_STORAGE_KEY = 'jmbackup.collapsedGroups'

function readCollapsedGroups(): Set<string> {
  try {
    const raw = localStorage.getItem(COLLAPSED_GROUPS_STORAGE_KEY)
    return raw ? new Set<string>(JSON.parse(raw) as string[]) : new Set()
  } catch {
    return new Set()
  }
}

function writeCollapsedGroups(groups: Set<string>) {
  try {
    localStorage.setItem(COLLAPSED_GROUPS_STORAGE_KEY, JSON.stringify([...groups]))
  } catch {
    // Sin localStorage disponible (navegación privada, por ejemplo): el plegado
    // simplemente no persiste entre pestañas, no es un error que deba interrumpir nada.
  }
}

function GroupSection({
  bucket,
  liveProgress,
  selectedTaskId,
  onSelectTask,
}: {
  bucket: GroupBucket
  liveProgress: Map<number, ProgressMessage>
  selectedTaskId: number | undefined
  onSelectTask: (taskId: number) => void
}) {
  // Estado local nada más: al cambiar de pestaña, este componente se desmonta y
  // remonta, así que el plegado tiene que sobrevivir en localStorage — no alcanza con
  // useState solo, porque cada remontaje volvía a arrancar en "expandido".
  const [collapsed, setCollapsed] = useState(() => readCollapsedGroups().has(bucket.key))

  function toggleCollapsed() {
    setCollapsed((value) => {
      const next = !value
      const stored = readCollapsedGroups()
      if (next) {
        stored.add(bucket.key)
      } else {
        stored.delete(bucket.key)
      }
      writeCollapsedGroups(stored)
      return next
    })
  }

  return (
    <div className="border-b border-border-subtle last:border-b-0">
      <button
        type="button"
        onClick={toggleCollapsed}
        className="flex w-full items-center gap-2 px-4 py-2 text-left text-sm font-semibold text-fg-muted
          hover:text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
      >
        <span aria-hidden="true">{collapsed ? '▸' : '▾'}</span>
        {bucket.label}
        <span className="text-xs font-normal">({bucket.tasks.length})</span>
      </button>
      {collapsed ? null : (
        <div>
          {bucket.tasks.map((task) => (
            <TaskRow
              key={task.id}
              task={task}
              liveProgress={liveProgress.get(task.id)}
              selected={task.id === selectedTaskId}
              onSelect={() => onSelectTask(task.id)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export function TaskGroupList({ tasks, groups, liveProgress, selectedTaskId, onSelectTask }: TaskGroupListProps) {
  const strings = useStrings()

  if (tasks.length === 0) {
    return (
      <Card>
        <p className="text-sm text-fg-muted">{strings.tasks.empty}</p>
      </Card>
    )
  }

  const buckets = groupTasks(tasks, groups, strings.tasks.ungrouped)

  return (
    <Card title={strings.tasks.title} className="overflow-hidden" noPadding>
      {buckets.map((bucket) => (
        <GroupSection
          key={bucket.key}
          bucket={bucket}
          liveProgress={liveProgress}
          selectedTaskId={selectedTaskId}
          onSelectTask={onSelectTask}
        />
      ))}
    </Card>
  )
}
