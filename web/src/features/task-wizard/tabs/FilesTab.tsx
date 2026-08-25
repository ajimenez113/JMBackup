import { useState } from 'react'
import { Button } from '../../../components/Button'
import { useStrings } from '../../../i18n'
import type { CredentialResponse, TaskPathResponse } from '../../../lib/api-client'
import { useCredentials } from '../useCredentials'
import { usePathCrud, usePaths } from '../usePaths'
import { AddPathDialog } from './AddPathDialog'
import { CredentialDialog } from './CredentialDialog'
import { PathRow } from './PathRow'
import { useConnectivityCheck } from './useConnectivityCheck'

interface PathSectionProps {
  taskId: number
  role: 'Source' | 'Destination'
  title: string
  paths: TaskPathResponse[]
  credentials: CredentialResponse[]
  onRequestNewCredential: (pathId: number) => void
}

function PathSection({ taskId, role, title, paths, credentials, onRequestNewCredential }: PathSectionProps) {
  const strings = useStrings()
  const { add, remove, update } = usePathCrud(taskId)
  const [dialogOpen, setDialogOpen] = useState(false)
  const pathIds = paths.map((path) => path.id)
  const connectivity = useConnectivityCheck(taskId, pathIds)

  return (
    <div className="flex-1">
      <div className="mb-2 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-fg">{title}</h3>
        <Button size="sm" onClick={() => setDialogOpen(true)}>
          {strings.wizard.add}
        </Button>
      </div>
      <div className="rounded-jm border border-border-subtle">
        {paths.length === 0 ? (
          <p className="px-3 py-4 text-sm text-fg-muted">{strings.wizard.files.empty}</p>
        ) : (
          paths.map((path) => (
            <PathRow
              key={path.id}
              path={path}
              connectivity={connectivity.get(path.id)}
              credentials={credentials}
              onRemove={() => void remove(path.id)}
              onCredentialChange={(credentialId) =>
                void update({ pathId: path.id, request: { role, path: path.path, credentialId, position: path.position } })
              }
              onRequestNewCredential={() => onRequestNewCredential(path.id)}
            />
          ))
        )}
      </div>
      <AddPathDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onAdd={(path) => void add({ role, path, credentialId: undefined, position: paths.length })}
      />
    </div>
  )
}

export function FilesTab({ taskId }: { taskId: number }) {
  const strings = useStrings()
  const pathsQuery = usePaths(taskId)
  const paths = pathsQuery.data ?? []
  const credentialsQuery = useCredentials()
  const credentials = credentialsQuery.data ?? []
  const { update } = usePathCrud(taskId)
  const [credentialDialogPathId, setCredentialDialogPathId] = useState<number>()

  function handleCredentialCreated(credentialId: number) {
    const path = paths.find((p) => p.id === credentialDialogPathId)
    if (path) {
      const role = path.role as 'Source' | 'Destination'
      void update({ pathId: path.id, request: { role, path: path.path, credentialId, position: path.position } })
    }
    setCredentialDialogPathId(undefined)
  }

  return (
    <div className="flex flex-col gap-4 sm:flex-row">
      <PathSection
        taskId={taskId}
        role="Source"
        title={strings.wizard.files.source}
        paths={paths.filter((path) => path.role === 'Source')}
        credentials={credentials}
        onRequestNewCredential={setCredentialDialogPathId}
      />
      <PathSection
        taskId={taskId}
        role="Destination"
        title={strings.wizard.files.destination}
        paths={paths.filter((path) => path.role === 'Destination')}
        credentials={credentials}
        onRequestNewCredential={setCredentialDialogPathId}
      />
      <CredentialDialog
        open={credentialDialogPathId !== undefined}
        onOpenChange={(open) => !open && setCredentialDialogPathId(undefined)}
        onCreated={handleCredentialCreated}
      />
    </div>
  )
}
