import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useToast } from '../../../components/useToast'
import { useStrings } from '../../../i18n'
import type { TaskResponse } from '../../../lib/api-client'
import { useTaskGroups } from '../../tasks/useTasks'
import { useTaskCrud, useTaskGroupCrud } from '../useTaskGeneral'

interface GeneralTabProps {
  taskId: number | undefined
  task: TaskResponse | undefined
  onCreated: (taskId: number) => void
}

const NEW_GROUP_VALUE = '__new__'
const NONE_GROUP_VALUE = '__none__'

export function GeneralTab({ taskId, task, onCreated }: GeneralTabProps) {
  const strings = useStrings()
  const navigate = useNavigate()
  const toast = useToast()
  const groupsQuery = useTaskGroups()
  const { create: createTask, update: updateTask } = useTaskCrud()
  const { create: createGroup } = useTaskGroupCrud()

  const [name, setName] = useState('')
  const [groupId, setGroupId] = useState<number>()
  const [enabled, setEnabled] = useState(true)
  const [includeSubfolders, setIncludeSubfolders] = useState(true)
  const [mode, setMode] = useState<'Incremental' | 'Mirror'>('Incremental')
  const [orderStrategy, setOrderStrategy] = useState('NameAscending')
  const [nameError, setNameError] = useState<string>()
  const [newGroupName, setNewGroupName] = useState('')
  const [creatingGroup, setCreatingGroup] = useState(false)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!task) {
      return
    }

    setName(task.name)
    setGroupId(task.groupId)
    setEnabled(task.enabled)
    setIncludeSubfolders(task.includeSubfolders)
    setMode(task.mode === 'Mirror' ? 'Mirror' : 'Incremental')
    setOrderStrategy(task.orderStrategy)
  }, [task])

  async function handleSave() {
    if (!name.trim()) {
      setNameError(strings.wizard.general.nameRequired)
      return
    }

    setNameError(undefined)
    setSaving(true)
    try {
      const request = {
        name: name.trim(),
        groupId,
        enabled,
        mode,
        orderStrategy,
        includeSubfolders,
        // El resto de estos campos vive de verdad en la pestaña Avanzado; acá se
        // preservan sin cambiarlos al editar, y van en su default al crear.
        absolutePaths: task?.absolutePaths ?? false,
        removeEmptyDirs: task?.removeEmptyDirs ?? false,
        verifyLevel: task?.verifyLevel ?? 'SizeOnly',
      }

      if (taskId === undefined) {
        const created = await createTask(request)
        toast.show(strings.wizard.createdNotice)
        onCreated(created.id)
      } else {
        await updateTask({ id: taskId, request })
        toast.show(strings.wizard.savedNotice)
      }
    } finally {
      setSaving(false)
    }
  }

  async function handleGroupChange(value: string) {
    if (value === NEW_GROUP_VALUE) {
      setCreatingGroup(true)
      return
    }

    setGroupId(value === NONE_GROUP_VALUE ? undefined : Number(value))
  }

  async function handleCreateGroup() {
    if (!newGroupName.trim()) {
      return
    }

    const group = await createGroup({ name: newGroupName.trim(), position: groupsQuery.data?.length ?? 0 })
    setGroupId(group.id)
    setNewGroupName('')
    setCreatingGroup(false)
  }

  const groupOptions = [
    { value: NONE_GROUP_VALUE, label: strings.tasks.ungrouped },
    ...(groupsQuery.data ?? []).map((group) => ({ value: String(group.id), label: group.name })),
    { value: NEW_GROUP_VALUE, label: strings.wizard.general.newGroup },
  ]

  return (
    <div className="flex flex-col gap-4">
      <Input label={strings.wizard.general.name} value={name} onChange={(event) => setName(event.target.value)} error={nameError} />

      <div className="flex flex-col gap-1">
        <Select
          label={strings.wizard.general.group}
          value={groupId === undefined ? NONE_GROUP_VALUE : String(groupId)}
          onValueChange={(value) => void handleGroupChange(value)}
          options={groupOptions}
        />
        {creatingGroup ? (
          <div className="mt-1 flex items-center gap-2">
            <Input
              placeholder={strings.wizard.general.newGroupPrompt}
              value={newGroupName}
              onChange={(event) => setNewGroupName(event.target.value)}
            />
            <Button size="sm" onClick={() => void handleCreateGroup()}>
              {strings.wizard.add}
            </Button>
          </div>
        ) : null}
      </div>

      <Switch id="task-enabled" checked={enabled} onCheckedChange={setEnabled} label={strings.wizard.general.enabled} />
      <Switch
        id="task-include-subfolders"
        checked={includeSubfolders}
        onCheckedChange={setIncludeSubfolders}
        label={strings.wizard.general.includeSubfolders}
      />

      <Select
        label={strings.wizard.general.mode}
        value={mode}
        onValueChange={(value) => setMode(value as 'Incremental' | 'Mirror')}
        options={[
          { value: 'Incremental', label: strings.wizard.general.modeIncremental },
          { value: 'Mirror', label: strings.wizard.general.modeMirror },
        ]}
      />
      <p className="text-xs text-fg-muted">
        {strings.wizard.general.modeSendOnly} · {strings.wizard.general.modeReceiveOnly}
      </p>

      <Select
        label={strings.wizard.general.orderStrategy}
        value={orderStrategy}
        onValueChange={setOrderStrategy}
        options={[
          { value: 'NameAscending', label: strings.wizard.general.orderNameAscending },
          { value: 'NameDescending', label: strings.wizard.general.orderNameDescending },
          { value: 'SizeDescending', label: strings.wizard.general.orderSizeDescending },
          { value: 'SizeAscending', label: strings.wizard.general.orderSizeAscending },
          { value: 'ModifiedOldestFirst', label: strings.wizard.general.orderModifiedOldestFirst },
          { value: 'ModifiedNewestFirst', label: strings.wizard.general.orderModifiedNewestFirst },
        ]}
      />

      <div className="flex gap-2">
        <Button variant="primary" disabled={saving} onClick={() => void handleSave()}>
          {strings.wizard.save}
        </Button>
        <Button variant="ghost" onClick={() => navigate('/')}>
          {strings.wizard.cancel}
        </Button>
      </div>
    </div>
  )
}
