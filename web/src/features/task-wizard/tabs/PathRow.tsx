import { StatusDot, type Status } from '../../../components/StatusDot'
import { Tooltip } from '../../../components/Tooltip'
import { Button } from '../../../components/Button'
import { Select } from '../../../components/Select'
import { useStrings } from '../../../i18n'
import type { CredentialResponse, TaskPathResponse } from '../../../lib/api-client'
import type { ConnectivityState } from './useConnectivityCheck'

const NEW_CREDENTIAL_VALUE = '__new__'
const NO_CREDENTIAL_VALUE = '__none__'

const REASON_KEYS = {
  Unknown: 'reasonUnknown',
  FileLocked: 'reasonFileLocked',
  HostUnreachable: 'reasonHostUnreachable',
  PermissionDenied: 'reasonPermissionDenied',
  PathNotFound: 'reasonPathNotFound',
  InvalidCredentials: 'reasonInvalidCredentials',
} as const

interface PathRowProps {
  path: TaskPathResponse
  connectivity: ConnectivityState | undefined
  credentials: CredentialResponse[]
  onRemove: () => void
  onCredentialChange: (credentialId: number | undefined) => void
  onRequestNewCredential: () => void
}

export function PathRow({ path, connectivity, credentials, onRemove, onCredentialChange, onRequestNewCredential }: PathRowProps) {
  const strings = useStrings()

  const credentialOptions = [
    { value: NO_CREDENTIAL_VALUE, label: strings.wizard.files.credentialNone },
    ...credentials.map((credential) => ({ value: String(credential.id), label: credential.alias })),
    { value: NEW_CREDENTIAL_VALUE, label: strings.wizard.files.credentialNew },
  ]

  function handleCredentialChange(value: string) {
    if (value === NEW_CREDENTIAL_VALUE) {
      onRequestNewCredential()
      return
    }

    onCredentialChange(value === NO_CREDENTIAL_VALUE ? undefined : Number(value))
  }

  let status: Status = 'idle'
  let tooltip: string = strings.wizard.files.connectivityChecking

  if (connectivity === 'checking' || connectivity === undefined) {
    status = 'idle'
    tooltip = strings.wizard.files.connectivityChecking
  } else if (connectivity.isConnected) {
    status = 'ok'
    tooltip = strings.wizard.files.connectivityOk
  } else {
    status = 'error'
    const reasonKey = REASON_KEYS[connectivity.reason as keyof typeof REASON_KEYS] ?? 'reasonUnknown'
    tooltip = connectivity.detail ?? strings.wizard.files[reasonKey]
  }

  return (
    <div className="flex items-center justify-between gap-3 border-b border-border-subtle px-3 py-2 last:border-b-0">
      <div className="flex items-center gap-3">
        <Tooltip content={tooltip}>
          <span>
            <StatusDot status={status} />
          </span>
        </Tooltip>
        <span className="text-sm text-fg">{path.path}</span>
      </div>
      <div className="flex items-center gap-3">
        <Select
          value={path.credentialId ? String(path.credentialId) : NO_CREDENTIAL_VALUE}
          onValueChange={handleCredentialChange}
          options={credentialOptions}
        />
        <Button variant="ghost" size="sm" onClick={onRemove}>
          {strings.wizard.remove}
        </Button>
      </div>
    </div>
  )
}
