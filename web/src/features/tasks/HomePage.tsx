import { useState } from 'react'
import { AboutModal } from '../about/AboutModal'
import { useStrings } from '../../i18n'
import { ActionBar } from './ActionBar'
import { HealthPanel } from './HealthPanel'
import { TaskGroupList } from './TaskGroupList'
import { useLiveProgress } from './useLiveProgress'
import { useTaskGroups, useTaskSummaries } from './useTasks'

export function HomePage() {
  const strings = useStrings()
  const tasksQuery = useTaskSummaries()
  const groupsQuery = useTaskGroups()
  const liveProgress = useLiveProgress()

  const [selectedTaskId, setSelectedTaskId] = useState<number>()
  const [aboutOpen, setAboutOpen] = useState(false)

  const tasks = tasksQuery.data ?? []
  const groups = groupsQuery.data ?? []
  const firstTaskId = tasks[0]?.id

  return (
    <div className="flex flex-1 flex-col">
      <ActionBar
        selectedTaskId={selectedTaskId}
        firstTaskId={firstTaskId}
        hasRunningTasks={liveProgress.size > 0}
        isSelectedTaskRunning={selectedTaskId !== undefined && liveProgress.has(selectedTaskId)}
        onOpenAbout={() => setAboutOpen(true)}
      />
      <div className="flex-1 overflow-y-auto p-4">
        {tasksQuery.isLoading ? (
          <p className="text-sm text-fg-muted">{strings.common.loading}</p>
        ) : (
          <>
            <HealthPanel tasks={tasks} liveProgress={liveProgress} />
            <TaskGroupList
              tasks={tasks}
              groups={groups}
              liveProgress={liveProgress}
              selectedTaskId={selectedTaskId}
              onSelectTask={setSelectedTaskId}
            />
          </>
        )}
      </div>
      <AboutModal open={aboutOpen} onOpenChange={setAboutOpen} />
    </div>
  )
}
