import { useState } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Modal } from '../../../components/Modal'
import { useStrings } from '../../../i18n'
import { useCreateCredential } from '../useCredentials'

interface CredentialDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: (credentialId: number) => void
}

export function CredentialDialog({ open, onOpenChange, onCreated }: CredentialDialogProps) {
  const strings = useStrings()
  const createCredential = useCreateCredential()
  const [alias, setAlias] = useState('')
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  function reset() {
    setAlias('')
    setUsername('')
    setPassword('')
  }

  async function handleSave() {
    if (!alias.trim() || !password) {
      return
    }

    const created = await createCredential.mutateAsync({ alias: alias.trim(), username: username.trim() || undefined, password })
    reset()
    onOpenChange(false)
    onCreated(created.id)
  }

  return (
    <Modal
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          reset()
        }
        onOpenChange(next)
      }}
      title={strings.wizard.files.credentialDialogTitle}
    >
      <div className="flex flex-col gap-4">
        <Input label={strings.wizard.files.credentialAlias} value={alias} onChange={(event) => setAlias(event.target.value)} />
        <Input label={strings.wizard.files.credentialUsername} value={username} onChange={(event) => setUsername(event.target.value)} />
        <Input
          label={strings.wizard.files.credentialPassword}
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
        <div className="flex justify-end">
          <Button variant="primary" onClick={() => void handleSave()} disabled={createCredential.isPending}>
            {strings.wizard.files.credentialSave}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
