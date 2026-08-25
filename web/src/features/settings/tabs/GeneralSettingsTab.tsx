import { useEffect, useRef, useState } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import type { ExportedConfiguration } from '../../../lib/api-client'
import { useConfigExportImport, useGeneralSettings } from '../useSettings'

export function GeneralSettingsTab() {
  const strings = useStrings()
  const { data, save, isSaving } = useGeneralSettings()
  const { exportConfig, importConfig, isImporting } = useConfigExportImport()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [theme, setTheme] = useState('System')
  const [startWithWindows, setStartWithWindows] = useState(true)
  const [historyRetentionDays, setHistoryRetentionDays] = useState('90')
  const [savedNotice, setSavedNotice] = useState(false)

  useEffect(() => {
    if (!data) {
      return
    }

    setTheme(data.theme)
    setStartWithWindows(data.startWithWindows)
    setHistoryRetentionDays(String(data.historyRetentionDays))
  }, [data])

  async function handleSave() {
    await save({ theme, startWithWindows, historyRetentionDays: Number(historyRetentionDays) })
    setSavedNotice(true)
  }

  async function handleExport() {
    const configuration = await exportConfig()
    const blob = new Blob([JSON.stringify(configuration, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'jmbackup-configuracion.json'
    link.click()
    URL.revokeObjectURL(url)
  }

  async function handleImportFile(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file || !window.confirm(strings.settings.general.importConfirm)) {
      return
    }

    const text = await file.text()
    const configuration = JSON.parse(text) as ExportedConfiguration
    await importConfig(configuration)
  }

  return (
    <div className="flex flex-col gap-4">
      <Select
        label={strings.settings.general.theme}
        value={theme}
        onValueChange={setTheme}
        options={[
          { value: 'Light', label: strings.theme.light },
          { value: 'Dark', label: strings.theme.dark },
          { value: 'System', label: strings.theme.system },
        ]}
      />
      <Switch
        id="start-with-windows"
        checked={startWithWindows}
        onCheckedChange={setStartWithWindows}
        label={strings.settings.general.startWithWindows}
      />
      <Input
        type="number"
        label={strings.settings.general.historyRetentionDays}
        value={historyRetentionDays}
        onChange={(event) => setHistoryRetentionDays(event.target.value)}
      />

      <div className="flex items-center gap-2">
        <Button variant="primary" disabled={isSaving} onClick={() => void handleSave()}>
          {strings.settings.save}
        </Button>
        {savedNotice ? <span className="text-sm text-success">{strings.settings.saved}</span> : null}
      </div>

      <hr className="border-border-subtle" />

      <div className="flex items-center gap-2">
        <Button onClick={() => void handleExport()}>{strings.settings.general.exportConfig}</Button>
        <Button disabled={isImporting} onClick={() => fileInputRef.current?.click()}>
          {strings.settings.general.importConfig}
        </Button>
        <input ref={fileInputRef} type="file" accept="application/json" className="hidden" onChange={(event) => void handleImportFile(event)} />
      </div>
    </div>
  )
}
