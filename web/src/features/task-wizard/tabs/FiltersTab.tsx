import { useState } from 'react'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Input } from '../../../components/Input'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import { useFilterCrud, useFilters } from '../useFilters'

export function FiltersTab({ taskId }: { taskId: number }) {
  const strings = useStrings()
  const filtersQuery = useFilters(taskId)
  const filters = filtersQuery.data ?? []
  const { add, remove } = useFilterCrud(taskId)

  const [pattern, setPattern] = useState('')
  const [useRegex, setUseRegex] = useState(false)
  const [caseSensitive, setCaseSensitive] = useState(false)
  const [priority, setPriority] = useState(false)

  function handleAdd() {
    if (!pattern.trim()) {
      return
    }

    void add({ pattern: pattern.trim(), useRegex, caseSensitive, priority })
    setPattern('')
    setUseRegex(false)
    setCaseSensitive(false)
    setPriority(false)
  }

  return (
    <div className="flex flex-col gap-4">
      <Card className="bg-bg-canvas">
        <p className="text-sm text-fg-muted">{strings.wizard.filters.precedenceNotice}</p>
      </Card>

      <div className="flex flex-col gap-3 rounded-jm border border-border-subtle p-3">
        <Input label={strings.wizard.filters.pattern} value={pattern} onChange={(event) => setPattern(event.target.value)} />
        <Switch id="filter-regex" checked={useRegex} onCheckedChange={setUseRegex} label={strings.wizard.exclusions.useRegex} />
        <Switch
          id="filter-case-sensitive"
          checked={caseSensitive}
          onCheckedChange={setCaseSensitive}
          label={strings.wizard.exclusions.caseSensitive}
        />
        <Switch id="filter-priority" checked={priority} onCheckedChange={setPriority} label={strings.wizard.filters.priority} />
        <div>
          <Button variant="primary" onClick={handleAdd}>
            {strings.wizard.filters.addFilter}
          </Button>
        </div>
      </div>

      <div className="rounded-jm border border-border-subtle">
        {filters.length === 0 ? (
          <p className="px-3 py-4 text-sm text-fg-muted">{strings.wizard.filters.empty}</p>
        ) : (
          filters.map((filter) => (
            <div key={filter.id} className="flex items-center justify-between gap-3 border-b border-border-subtle px-3 py-2 last:border-b-0">
              <span className="text-sm text-fg">
                {filter.pattern} {filter.priority ? `· ${strings.wizard.filters.priority}` : ''}
              </span>
              <Button variant="ghost" size="sm" onClick={() => void remove(filter.id)}>
                {strings.wizard.remove}
              </Button>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
