import { useEffect, useState } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Modal } from '../../../components/Modal'
import { Select } from '../../../components/Select'
import { useStrings } from '../../../i18n'
import type { CredentialResponse } from '../../../lib/api-client'
import { useCreateCredential, useUpdateCredential } from '../useCredentials'

interface CredentialDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved: (credential: CredentialResponse) => void
  /** Presente => modo edición: los campos se precargan y el backend ya no se puede cambiar. */
  credential?: CredentialResponse
  /**
   * Fija el backend en el alta y oculta el selector — lo usa el diálogo de agregar ruta,
   * que ya sabe para qué backend hace falta la credencial.
   */
  fixedBackendType?: string
}

const BACKEND_TYPES = ['Local', 'Ftp', 'S3'] as const

export function CredentialDialog({ open, onOpenChange, onSaved, credential, fixedBackendType }: CredentialDialogProps) {
  const strings = useStrings()
  const createCredential = useCreateCredential()
  const updateCredential = useUpdateCredential()
  const isEditing = credential !== undefined

  const [alias, setAlias] = useState('')
  const [backendType, setBackendType] = useState<string>(fixedBackendType ?? 'Local')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  useEffect(() => {
    if (!open) {
      return
    }

    setAlias(credential?.alias ?? '')
    setBackendType(credential?.backendType ?? fixedBackendType ?? 'Local')
    setUsername(credential?.username ?? '')
    setPassword('')
  }, [open, credential, fixedBackendType])

  const backendTypeOptions = BACKEND_TYPES.map((type) => ({ value: type, label: strings.wizard.files.backendTypeLabels[type] }))
  const isPending = createCredential.isPending || updateCredential.isPending
  const canSave = alias.trim().length > 0 && (isEditing || password.length > 0)

  async function handleSave() {
    if (!canSave) {
      return
    }

    const saved = isEditing
      ? await updateCredential.mutateAsync({
          id: credential.id,
          request: { alias: alias.trim(), username: username.trim() || undefined, password: password || undefined },
        })
      : await createCredential.mutateAsync({
          alias: alias.trim(),
          backendType,
          username: username.trim() || undefined,
          password,
        })

    onOpenChange(false)
    onSaved(saved)
  }

  return (
    <Modal
      open={open}
      onOpenChange={onOpenChange}
      title={isEditing ? strings.wizard.files.credentialEditTitle : strings.wizard.files.credentialDialogTitle}
    >
      <div className="flex flex-col gap-4">
        {isEditing || fixedBackendType ? (
          <p className="text-sm text-fg-muted">
            {strings.wizard.files.credentialBackendLabel}: {strings.wizard.files.backendTypeLabels[backendType as keyof typeof strings.wizard.files.backendTypeLabels] ?? backendType}
          </p>
        ) : (
          <Select
            label={strings.wizard.files.credentialBackendLabel}
            value={backendType}
            onValueChange={setBackendType}
            options={backendTypeOptions}
          />
        )}
        <Input label={strings.wizard.files.credentialAlias} value={alias} onChange={(event) => setAlias(event.target.value)} />
        <Input label={strings.wizard.files.credentialUsername} value={username} onChange={(event) => setUsername(event.target.value)} />
        <Input
          label={isEditing ? strings.wizard.files.credentialPasswordOptional : strings.wizard.files.credentialPassword}
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
        <div className="flex justify-end">
          <Button variant="primary" onClick={() => void handleSave()} disabled={isPending || !canSave}>
            {strings.wizard.files.credentialSave}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
