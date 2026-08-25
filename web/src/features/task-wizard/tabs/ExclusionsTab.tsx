import { useState } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import type { ExclusionRequest } from '../../../lib/api-client'
import { EXTENSION_GROUPS } from '../extensionGroups'
import { useExclusionCrud, useExclusions } from '../useExclusions'

type Kind = 'Extension' | 'FileName' | 'Folder' | 'Contains' | 'Size' | 'Age'

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

function describeExclusion(exclusion: { kind: string; pattern?: string; sizeBytes?: number; ageDays?: number; operator?: string }): string {
  switch (exclusion.kind) {
    case 'Size':
      return `${exclusion.operator === 'GreaterThan' ? '>' : '<'} ${exclusion.sizeBytes ?? 0} bytes`
    case 'Age':
      return `${exclusion.operator === 'GreaterThan' ? '>' : '<'} ${exclusion.ageDays ?? 0} días`
    default:
      return exclusion.pattern ?? ''
  }
}

export function ExclusionsTab({ taskId }: { taskId: number }) {
  const strings = useStrings()
  const exclusionsQuery = useExclusions(taskId)
  const exclusions = exclusionsQuery.data ?? []
  const { add, remove } = useExclusionCrud(taskId)

  const [kind, setKind] = useState<Kind>('Extension')
  const [selectedGroups, setSelectedGroups] = useState<Set<string>>(new Set())
  const [manualExtensions, setManualExtensions] = useState('')
  const [pattern, setPattern] = useState('')
  const [useRegex, setUseRegex] = useState(false)
  const [caseSensitive, setCaseSensitive] = useState(false)
  const [operator, setOperator] = useState<'GreaterThan' | 'LessThan'>('GreaterThan')
  const [sizeBytes, setSizeBytes] = useState('')
  const [ageDays, setAgeDays] = useState('')

  function toggleGroup(key: string) {
    setSelectedGroups((previous) => {
      const next = new Set(previous)
      if (next.has(key)) {
        next.delete(key)
      } else {
        next.add(key)
      }
      return next
    })
  }

  function handleAdd() {
    let request: ExclusionRequest | undefined

    if (kind === 'Extension') {
      const groupExtensions = EXTENSION_GROUPS.filter((group) => selectedGroups.has(group.key)).flatMap((group) => group.extensions)
      const manual = manualExtensions
        .split(',')
        .map((value) => value.trim())
        .filter(Boolean)
      const extensions = [...new Set([...groupExtensions, ...manual])]
      if (extensions.length === 0) {
        return
      }

      request = {
        kind: 'Extension',
        pattern: `\\.(${extensions.map(escapeRegex).join('|')})$`,
        useRegex: true,
        caseSensitive,
        operator: undefined,
        sizeBytes: undefined,
        ageDays: undefined,
      }
    } else if (kind === 'FileName' || kind === 'Folder' || kind === 'Contains') {
      if (!pattern.trim()) {
        return
      }

      request = { kind, pattern: pattern.trim(), useRegex, caseSensitive, operator: undefined, sizeBytes: undefined, ageDays: undefined }
    } else if (kind === 'Size') {
      if (!sizeBytes) {
        return
      }

      request = { kind: 'Size', pattern: undefined, useRegex: false, caseSensitive: false, operator, sizeBytes: Number(sizeBytes), ageDays: undefined }
    } else {
      if (!ageDays) {
        return
      }

      request = { kind: 'Age', pattern: undefined, useRegex: false, caseSensitive: false, operator, sizeBytes: undefined, ageDays: Number(ageDays) }
    }

    void add(request)
    setSelectedGroups(new Set())
    setManualExtensions('')
    setPattern('')
    setSizeBytes('')
    setAgeDays('')
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-3 rounded-jm border border-border-subtle p-3">
        <Select
          label={strings.wizard.exclusions.kind}
          value={kind}
          onValueChange={(value) => setKind(value as Kind)}
          options={[
            { value: 'Extension', label: strings.wizard.exclusions.kindExtension },
            { value: 'FileName', label: strings.wizard.exclusions.kindFileName },
            { value: 'Folder', label: strings.wizard.exclusions.kindFolder },
            { value: 'Contains', label: strings.wizard.exclusions.kindContains },
            { value: 'Size', label: strings.wizard.exclusions.kindSize },
            { value: 'Age', label: strings.wizard.exclusions.kindAge },
          ]}
        />

        {kind === 'Extension' ? (
          <>
            <div className="flex flex-col gap-1">
              <span className="text-sm font-medium text-fg">{strings.wizard.exclusions.extensionGroups}</span>
              <div className="flex flex-wrap gap-3">
                {EXTENSION_GROUPS.map((group) => (
                  <label key={group.key} className="flex items-center gap-1.5 text-sm text-fg">
                    <input type="checkbox" checked={selectedGroups.has(group.key)} onChange={() => toggleGroup(group.key)} />
                    {group.label}
                  </label>
                ))}
              </div>
            </div>
            <Input
              label={strings.wizard.exclusions.extensionManual}
              value={manualExtensions}
              onChange={(event) => setManualExtensions(event.target.value)}
              placeholder="log, bak, tmp"
            />
          </>
        ) : null}

        {kind === 'FileName' || kind === 'Folder' || kind === 'Contains' ? (
          <>
            <Input label={strings.wizard.exclusions.pattern} value={pattern} onChange={(event) => setPattern(event.target.value)} />
            <Switch id="exclusion-regex" checked={useRegex} onCheckedChange={setUseRegex} label={strings.wizard.exclusions.useRegex} />
          </>
        ) : null}

        {kind === 'Size' || kind === 'Age' ? (
          <div className="flex items-end gap-2">
            <Select
              label=""
              value={operator}
              onValueChange={(value) => setOperator(value as 'GreaterThan' | 'LessThan')}
              options={[
                { value: 'GreaterThan', label: strings.wizard.exclusions.operatorGreaterThan },
                { value: 'LessThan', label: strings.wizard.exclusions.operatorLessThan },
              ]}
            />
            <Input
              type="number"
              label={kind === 'Size' ? strings.wizard.exclusions.sizeBytes : strings.wizard.exclusions.ageDays}
              value={kind === 'Size' ? sizeBytes : ageDays}
              onChange={(event) => (kind === 'Size' ? setSizeBytes(event.target.value) : setAgeDays(event.target.value))}
            />
          </div>
        ) : null}

        {kind !== 'Size' && kind !== 'Age' && kind !== 'Extension' ? (
          <Switch
            id="exclusion-case-sensitive"
            checked={caseSensitive}
            onCheckedChange={setCaseSensitive}
            label={strings.wizard.exclusions.caseSensitive}
          />
        ) : null}

        <div>
          <Button variant="primary" onClick={handleAdd}>
            {strings.wizard.exclusions.addExclusion}
          </Button>
        </div>
      </div>

      <div className="rounded-jm border border-border-subtle">
        {exclusions.length === 0 ? (
          <p className="px-3 py-4 text-sm text-fg-muted">{strings.wizard.exclusions.empty}</p>
        ) : (
          exclusions.map((exclusion) => (
            <div key={exclusion.id} className="flex items-center justify-between gap-3 border-b border-border-subtle px-3 py-2 last:border-b-0">
              <span className="text-sm text-fg">
                <span className="font-medium">{exclusion.kind}</span> — {describeExclusion(exclusion)}
              </span>
              <Button variant="ghost" size="sm" onClick={() => void remove(exclusion.id)}>
                {strings.wizard.remove}
              </Button>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
