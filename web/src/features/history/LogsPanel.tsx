import { Modal } from '../../components/Modal'
import { Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '../../components/Table'
import { Tabs } from '../../components/Tabs'
import { useStrings } from '../../i18n'
import type { RunItemResponse } from '../../lib/api-client'
import { useBackedUpItems, useErrorItems } from './useRunItems'

function ItemsTable({ items, showErrorColumns }: { items: RunItemResponse[]; showErrorColumns: boolean }) {
  const strings = useStrings()

  if (items.length === 0) {
    return <p className="p-3 text-sm text-fg-muted">{strings.history.logsEmpty}</p>
  }

  return (
    <Table>
      <TableHead>
        <TableRow>
          <TableHeaderCell>{strings.history.columnPath}</TableHeaderCell>
          <TableHeaderCell>{strings.history.columnSize}</TableHeaderCell>
          {showErrorColumns ? (
            <>
              <TableHeaderCell>{strings.history.columnErrorReason}</TableHeaderCell>
              <TableHeaderCell>{strings.history.columnAttempts}</TableHeaderCell>
            </>
          ) : null}
          <TableHeaderCell>{strings.history.columnTimestamp}</TableHeaderCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {items.map((item) => (
          <TableRow key={item.id}>
            <TableCell>{item.path}</TableCell>
            <TableCell>{item.size.toLocaleString()}</TableCell>
            {showErrorColumns ? (
              <>
                <TableCell>{item.errorMessage ?? item.errorCode}</TableCell>
                <TableCell>{item.attempts}</TableCell>
              </>
            ) : null}
            <TableCell>{new Date(item.timestamp).toLocaleString()}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

interface LogsPanelProps {
  runId: number | undefined
  onOpenChange: (open: boolean) => void
}

export function LogsPanel({ runId, onOpenChange }: LogsPanelProps) {
  const strings = useStrings()
  const backedUpQuery = useBackedUpItems(runId)
  const errorsQuery = useErrorItems(runId)

  return (
    <Modal open={runId !== undefined} onOpenChange={onOpenChange} title={strings.history.logsTitle}>
      <div className="flex flex-col gap-3">
        <a href={`/api/logs/export.csv?runId=${runId}`} className="self-end text-sm text-accent underline">
          {strings.history.exportCsv}
        </a>
        <Tabs
          items={[
            {
              value: 'backed-up',
              label: strings.history.tabBackedUp,
              content: <ItemsTable items={backedUpQuery.data ?? []} showErrorColumns={false} />,
            },
            {
              value: 'errors',
              label: strings.history.tabErrors,
              content: <ItemsTable items={errorsQuery.data ?? []} showErrorColumns />,
            },
          ]}
        />
      </div>
    </Modal>
  )
}
