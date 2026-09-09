import { useNavigate } from 'react-router-dom'
import { Button } from '../../components/Button'
import { Tooltip } from '../../components/Tooltip'
import { useStrings } from '../../i18n'
import { useTaskActions } from './useTasks'

interface ActionBarProps {
  selectedTaskId: number | undefined
  firstTaskId: number | undefined
  hasRunningTasks: boolean
  isSelectedTaskRunning: boolean
  onOpenAbout: () => void
}

export function ActionBar({ selectedTaskId, firstTaskId, hasRunningTasks, isSelectedTaskRunning, onOpenAbout }: ActionBarProps) {
  const strings = useStrings()
  const navigate = useNavigate()
  const actions = useTaskActions()

  const targetTaskId = selectedTaskId ?? firstTaskId

  async function handleDelete() {
    if (selectedTaskId === undefined || !window.confirm(strings.actionBar.deleteConfirm)) {
      return
    }
    await actions.deleteTask(selectedTaskId)
  }

  return (
    <div className="flex flex-wrap items-center gap-2 border-b border-border-subtle bg-bg-elevated px-4 py-3">
      <Tooltip content={strings.actionBar.runAllTitle}>
        <Button variant="primary" onClick={() => void actions.runAll()}>
          {strings.actionBar.runAll}
        </Button>
      </Tooltip>

      <Tooltip content={strings.actionBar.startTitle}>
        <Button disabled={targetTaskId === undefined} onClick={() => targetTaskId !== undefined && void actions.run(targetTaskId)}>
          {strings.actionBar.start}
        </Button>
      </Tooltip>

      <Tooltip content={strings.actionBar.pauseTitle}>
        <Button
          disabled={!hasRunningTasks}
          onClick={() => void (selectedTaskId !== undefined ? actions.pause(selectedTaskId) : actions.pauseAll())}
        >
          {strings.actionBar.pause}
        </Button>
      </Tooltip>

      <Tooltip content={strings.actionBar.cancelTitle}>
        <Button
          variant="danger"
          disabled={!hasRunningTasks}
          onClick={() => void (selectedTaskId !== undefined ? actions.cancel(selectedTaskId) : actions.cancelAll())}
        >
          {strings.actionBar.cancel}
        </Button>
      </Tooltip>

      <div className="ms-auto flex items-center gap-2">
        <Tooltip content={strings.actionBar.deleteTaskTitle}>
          <Button variant="danger" disabled={selectedTaskId === undefined || isSelectedTaskRunning} onClick={() => void handleDelete()}>
            {strings.actionBar.deleteTask}
          </Button>
        </Tooltip>

        <div className="mx-1 h-6 w-px bg-border-subtle" aria-hidden="true" />

        <Tooltip content={strings.actionBar.addTitle}>
          <Button onClick={() => navigate('/tasks/new')}>{strings.actionBar.add}</Button>
        </Tooltip>
        <Tooltip content={strings.actionBar.settingsTitle}>
          <Button variant="ghost" onClick={() => navigate('/settings')}>
            {strings.actionBar.settings}
          </Button>
        </Tooltip>
        <Tooltip content={strings.actionBar.aboutTitle}>
          <Button variant="ghost" onClick={onOpenAbout}>
            {strings.actionBar.about}
          </Button>
        </Tooltip>
      </div>
    </div>
  )
}
