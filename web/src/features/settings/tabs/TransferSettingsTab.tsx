import { useEffect, useState } from 'react'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Input } from '../../../components/Input'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import { useTransferSettings } from '../useSettings'

const BYTES_PER_KB = 1024
const KB_PER_MB = 1024

type BandwidthUnit = 'KBs' | 'MBs'

export function TransferSettingsTab() {
  const strings = useStrings()
  const { data, save, isSaving } = useTransferSettings()

  const [maxParallelTransfers, setMaxParallelTransfers] = useState('4')
  const [bandwidthLimit, setBandwidthLimit] = useState('')
  const [bandwidthUnit, setBandwidthUnit] = useState<BandwidthUnit>('KBs')
  const [blockSize, setBlockSize] = useState('81920')
  const [preserveTimestamps, setPreserveTimestamps] = useState(true)
  const [savedNotice, setSavedNotice] = useState(false)

  useEffect(() => {
    if (!data) {
      return
    }

    setMaxParallelTransfers(String(data.maxParallelTransfers))
    // Se guarda en bytes/s (RF-90); acá solo se ajusta la presentación. Si el valor
    // guardado cae justo en un múltiplo de MB/s se muestra en esa unidad, para no
    // mostrar un número innecesariamente grande apenas se reabre la pestaña.
    const bytesPerSecond = data.globalBandwidthLimitBytesPerSecond
    if (!bytesPerSecond) {
      setBandwidthLimit('')
      setBandwidthUnit('KBs')
    } else {
      const kbPerSecond = bytesPerSecond / BYTES_PER_KB
      const isWholeMegabytes = kbPerSecond % KB_PER_MB === 0
      setBandwidthUnit(isWholeMegabytes ? 'MBs' : 'KBs')
      setBandwidthLimit(String(isWholeMegabytes ? kbPerSecond / KB_PER_MB : kbPerSecond))
    }
    setBlockSize(String(data.blockSizeBytes))
    setPreserveTimestamps(data.preserveTimestampsAndAttributes)
  }, [data])

  async function handleSave() {
    const enteredValue = bandwidthLimit ? Number(bandwidthLimit) : undefined
    const kbPerSecond = enteredValue === undefined ? undefined : bandwidthUnit === 'MBs' ? enteredValue * KB_PER_MB : enteredValue

    await save({
      maxParallelTransfers: Number(maxParallelTransfers),
      globalBandwidthLimitBytesPerSecond: kbPerSecond === undefined ? undefined : kbPerSecond * BYTES_PER_KB,
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
      <div className="flex items-end gap-2">
        <div className="flex-1">
          <Input
            type="number"
            min={0}
            label={strings.settings.transfer.bandwidthLimit}
            value={bandwidthLimit}
            onChange={(event) => setBandwidthLimit(event.target.value)}
          />
        </div>
        <Select
          value={bandwidthUnit}
          onValueChange={(value) => setBandwidthUnit(value as BandwidthUnit)}
          options={[
            { value: 'KBs', label: strings.settings.transfer.bandwidthUnitKBs },
            { value: 'MBs', label: strings.settings.transfer.bandwidthUnitMBs },
          ]}
        />
      </div>
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
