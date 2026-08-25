import { useEffect, useState } from 'react'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import type { TaskResponse } from '../../../lib/api-client'
import { useTaskCrud } from '../useTaskGeneral'
import { useDryRun } from '../useDryRun'
import { DryRunResultPanel } from './DryRunResultPanel'

interface AdvancedTabProps {
  taskId: number
  task: TaskResponse | undefined
}

export function AdvancedTab({ taskId, task }: AdvancedTabProps) {
  const strings = useStrings()
  const { update } = useTaskCrud()
  const dryRun = useDryRun(taskId)

  const [absolutePaths, setAbsolutePaths] = useState(false)
  const [removeEmptyDirs, setRemoveEmptyDirs] = useState(false)
  const [verifyLevel, setVerifyLevel] = useState('SizeOnly')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!task) {
      return
    }

    setAbsolutePaths(task.absolutePaths)
    setRemoveEmptyDirs(task.removeEmptyDirs)
    setVerifyLevel(task.verifyLevel)
  }, [task])

  async function handleSave() {
    if (!task) {
      return
    }

    setSaving(true)
    try {
      await update({
        id: taskId,
        request: {
          name: task.name,
          groupId: task.groupId,
          enabled: task.enabled,
          mode: task.mode,
          orderStrategy: task.orderStrategy,
          includeSubfolders: task.includeSubfolders,
          absolutePaths,
          removeEmptyDirs,
          verifyLevel,
        },
      })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {task?.mode === 'Mirror' ? (
        <Card className="border-warning bg-bg-canvas">
          <p className="font-semibold text-warning">{strings.wizard.advanced.mirrorWarningTitle}</p>
          <p className="mt-1 text-sm text-fg-muted">{strings.wizard.advanced.mirrorWarning}</p>
        </Card>
      ) : null}

      <Switch id="absolute-paths" checked={absolutePaths} onCheckedChange={setAbsolutePaths} label={strings.wizard.advanced.absolutePaths} />
      <Switch
        id="remove-empty-dirs"
        checked={removeEmptyDirs}
        onCheckedChange={setRemoveEmptyDirs}
        label={strings.wizard.advanced.removeEmptyDirs}
      />

      <Select
        label={strings.wizard.advanced.verifyLevel}
        value={verifyLevel}
        onValueChange={setVerifyLevel}
        options={[
          { value: 'SizeOnly', label: strings.wizard.advanced.verifySizeOnly },
          { value: 'SizeAndHash', label: strings.wizard.advanced.verifySizeAndHash },
        ]}
      />

      <div>
        <Button variant="primary" disabled={saving} onClick={() => void handleSave()}>
          {strings.wizard.save}
        </Button>
      </div>

      <hr className="border-border-subtle" />

      <div>
        <Button disabled={dryRun.isPending} onClick={() => dryRun.mutate()}>
          {dryRun.isPending ? strings.wizard.advanced.simulating : strings.wizard.advanced.simulate}
        </Button>
      </div>

      {dryRun.data ? <DryRunResultPanel result={dryRun.data} /> : <p className="text-sm text-fg-muted">{strings.wizard.advanced.dryRunEmpty}</p>}
    </div>
  )
}
