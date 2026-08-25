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
  const [collapsed, setCollapsed] = useState(false)

  return (
    <div className="border-b border-border-subtle last:border-b-0">
      <button
        type="button"
        onClick={() => setCollapsed((value) => !value)}
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
