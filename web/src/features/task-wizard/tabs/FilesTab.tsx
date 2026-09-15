import { useState } from 'react'
import { Button } from '../../../components/Button'
import { useStrings } from '../../../i18n'
import type { CredentialResponse, TaskPathRequest, TaskPathResponse } from '../../../lib/api-client'
import { useCredentials } from '../useCredentials'
import { usePathCrud, usePaths } from '../usePaths'
import { AddPathDialog, type NewPathInput } from './AddPathDialog'
import { CredentialDialog } from './CredentialDialog'
import { PathRow } from './PathRow'
import { useConnectivityCheck } from './useConnectivityCheck'

function toUpdateRequest(path: TaskPathResponse, credentialId: number | undefined): TaskPathRequest {
  return {
    role: path.role,
    backendType: path.backendType,
    path: path.path,
    credentialId,
    position: path.position,
    encrypted: path.encrypted,
    region: path.region,
    storageClass: path.storageClass,
    serverSideEncryption: path.serverSideEncryption,
  }
}

interface PathSectionProps {
  taskId: number
  role: 'Source' | 'Destination'
  title: string
  paths: TaskPathResponse[]
  credentials: CredentialResponse[]
  onRequestNewCredential: (path: TaskPathResponse) => void
}

function PathSection({ taskId, role, title, paths, credentials, onRequestNewCredential }: PathSectionProps) {
  const strings = useStrings()
  const { add, remove, update } = usePathCrud(taskId)
  const [dialogOpen, setDialogOpen] = useState(false)
  const pathIds = paths.map((path) => path.id)
  const connectivity = useConnectivityCheck(taskId, pathIds)

  function handleAdd(input: NewPathInput) {
    void add({
      role,
      backendType: input.backendType,
      path: input.path,
      credentialId: input.credentialId,
      position: paths.length,
      encrypted: input.encrypted,
      region: input.region,
      storageClass: input.storageClass,
      serverSideEncryption: input.serverSideEncryption,
    })
  }

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
              onCredentialChange={(credentialId) => void update({ pathId: path.id, request: toUpdateRequest(path, credentialId) })}
              onRequestNewCredential={() => onRequestNewCredential(path)}
            />
          ))
        )}
      </div>
      <AddPathDialog open={dialogOpen} onOpenChange={setDialogOpen} onAdd={handleAdd} />
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
  const [credentialDialogPath, setCredentialDialogPath] = useState<TaskPathResponse>()

  function handleCredentialSaved(credentialId: number) {
    const path = credentialDialogPath
    if (path) {
      void update({ pathId: path.id, request: toUpdateRequest(path, credentialId) })
    }
    setCredentialDialogPath(undefined)
  }

  return (
    <div className="flex flex-col gap-4 sm:flex-row">
      <PathSection
        taskId={taskId}
        role="Source"
        title={strings.wizard.files.source}
        paths={paths.filter((path) => path.role === 'Source')}
        credentials={credentials}
        onRequestNewCredential={setCredentialDialogPath}
      />
      <PathSection
        taskId={taskId}
        role="Destination"
        title={strings.wizard.files.destination}
        paths={paths.filter((path) => path.role === 'Destination')}
        credentials={credentials}
        onRequestNewCredential={setCredentialDialogPath}
      />
      <CredentialDialog
        open={credentialDialogPath !== undefined}
        onOpenChange={(open) => !open && setCredentialDialogPath(undefined)}
        onSaved={(credential) => handleCredentialSaved(credential.id)}
        fixedBackendType={credentialDialogPath?.backendType}
      />
    </div>
  )
}
