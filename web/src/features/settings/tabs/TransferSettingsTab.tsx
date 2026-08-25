import { useEffect, useState } from 'react'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Input } from '../../../components/Input'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import { useTransferSettings } from '../useSettings'

export function TransferSettingsTab() {
  const strings = useStrings()
  const { data, save, isSaving } = useTransferSettings()

  const [maxParallelTransfers, setMaxParallelTransfers] = useState('4')
  const [bandwidthLimit, setBandwidthLimit] = useState('')
  const [blockSize, setBlockSize] = useState('81920')
  const [preserveTimestamps, setPreserveTimestamps] = useState(true)
  const [savedNotice, setSavedNotice] = useState(false)

  useEffect(() => {
    if (!data) {
      return
    }

    setMaxParallelTransfers(String(data.maxParallelTransfers))
    setBandwidthLimit(data.globalBandwidthLimitBytesPerSecond ? String(data.globalBandwidthLimitBytesPerSecond) : '')
    setBlockSize(String(data.blockSizeBytes))
    setPreserveTimestamps(data.preserveTimestampsAndAttributes)
  }, [data])

  async function handleSave() {
    await save({
      maxParallelTransfers: Number(maxParallelTransfers),
      globalBandwidthLimitBytesPerSecond: bandwidthLimit ? Number(bandwidthLimit) : undefined,
      blockSizeBytes: Number(blockSize),
      preserveTimestampsAndAttributes: preserveTimestamps,
    })
    setSavedNotice(true)
  }

  return (
    <div className="flex flex-col gap-4">
      <Card className="bg-bg-canvas">
        <p className="text-sm text-fg-muted">{strings.settings.transfer.notAppliedYet}</p>
      </Card>

      <Input
        type="number"
        min={1}
        max={16}
        label={strings.settings.transfer.maxParallelTransfers}
        value={maxParallelTransfers}
        onChange={(event) => setMaxParallelTransfers(event.target.value)}
      />
      <Input
        type="number"
        label={strings.settings.transfer.bandwidthLimit}
        value={bandwidthLimit}
        onChange={(event) => setBandwidthLimit(event.target.value)}
      />
      <Input
        type="number"
        label={strings.settings.transfer.blockSize}
        value={blockSize}
        onChange={(event) => setBlockSize(event.target.value)}
      />
      <Switch
        id="preserve-timestamps"
        checked={preserveTimestamps}
        onCheckedChange={setPreserveTimestamps}
        label={strings.settings.transfer.preserveTimestamps}
      />

      <div className="flex items-center gap-2">
        <Button variant="primary" disabled={isSaving} onClick={() => void handleSave()}>
          {strings.settings.save}
        </Button>
        {savedNotice ? <span className="text-sm text-success">{strings.settings.saved}</span> : null}
      </div>
    </div>
  )
}
