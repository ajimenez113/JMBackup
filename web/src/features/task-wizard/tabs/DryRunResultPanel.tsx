import { Card } from '../../../components/Card'
import { useStrings } from '../../../i18n'
import type { DryRunItem, DryRunResponse } from '../../../lib/api-client'

function ItemList({ items }: { items: DryRunItem[] }) {
  if (items.length === 0) {
    return <p className="px-3 py-2 text-xs text-fg-muted">—</p>
  }

  return (
    <ul className="max-h-48 overflow-y-auto">
      {items.map((item) => (
        <li key={item.path} className="flex justify-between gap-3 border-t border-border-subtle px-3 py-1.5 text-xs first:border-t-0">
          <span className="truncate text-fg">{item.path}</span>
          <span className="shrink-0 text-fg-muted">{item.size.toLocaleString()} B</span>
        </li>
      ))}
    </ul>
  )
}

export function DryRunResultPanel({ result }: { result: DryRunResponse }) {
  const strings = useStrings()

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <Card title={`${strings.wizard.advanced.dryRunToCopy} (${result.toCopy.length})`} noPadding>
        <ItemList items={result.toCopy} />
      </Card>
      <Card title={`${strings.wizard.advanced.dryRunToSkip} (${result.toSkip.length})`} noPadding>
        <ItemList items={result.toSkip} />
      </Card>
      <Card title={`${strings.wizard.advanced.dryRunToTrash} (${result.toTrash.length})`} noPadding>
        <ItemList items={result.toTrash} />
      </Card>
      <p className="col-span-full text-sm text-fg-muted">
        {strings.wizard.advanced.dryRunTotalBytes}: {result.totalBytesToCopy.toLocaleString()} bytes
      </p>
    </div>
  )
}
