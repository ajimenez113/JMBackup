import { useState } from 'react'
import { Button } from '../../../components/Button'
import { Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '../../../components/Table'
import { useStrings } from '../../../i18n'
import type { CredentialResponse } from '../../../lib/api-client'
import { CredentialDialog } from '../../task-wizard/tabs/CredentialDialog'
import { useCredentials, useDeleteCredential } from '../../task-wizard/useCredentials'

export function CredentialsSettingsTab() {
  const strings = useStrings()
  const credentialsQuery = useCredentials()
  const credentials = credentialsQuery.data ?? []
  const deleteCredential = useDeleteCredential()

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingCredential, setEditingCredential] = useState<CredentialResponse>()

  function handleAdd() {
    setEditingCredential(undefined)
    setDialogOpen(true)
  }

  function handleEdit(credential: CredentialResponse) {
    setEditingCredential(credential)
    setDialogOpen(true)
  }

  function handleDelete(credential: CredentialResponse) {
    if (window.confirm(strings.settings.credentials.deleteConfirm)) {
      void deleteCredential.mutateAsync(credential.id)
    }
  }

  const backendLabels = strings.wizard.files.backendTypeLabels

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-fg-muted">{strings.settings.credentials.description}</p>

      <div className="flex justify-end">
        <Button variant="primary" size="sm" onClick={handleAdd}>
          {strings.settings.credentials.add}
        </Button>
      </div>

      {credentials.length === 0 ? (
        <p className="text-sm text-fg-muted">{strings.settings.credentials.empty}</p>
      ) : (
        <Table>
          <TableHead>
            <TableRow>
              <TableHeaderCell>{strings.settings.credentials.columnAlias}</TableHeaderCell>
              <TableHeaderCell>{strings.settings.credentials.columnBackend}</TableHeaderCell>
              <TableHeaderCell>{strings.settings.credentials.columnUsername}</TableHeaderCell>
              <TableHeaderCell>{strings.settings.credentials.columnCreatedAt}</TableHeaderCell>
              <TableHeaderCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {credentials.map((credential) => (
              <TableRow key={credential.id}>
                <TableCell>{credential.alias}</TableCell>
                <TableCell>{backendLabels[credential.backendType as keyof typeof backendLabels] ?? credential.backendType}</TableCell>
                <TableCell>{credential.username ?? '—'}</TableCell>
                <TableCell>{new Date(credential.createdAt).toLocaleString()}</TableCell>
                <TableCell>
                  <div className="flex justify-end gap-2">
                    <Button variant="ghost" size="sm" onClick={() => handleEdit(credential)}>
                      {strings.settings.credentials.edit}
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => handleDelete(credential)}>
                      {strings.settings.credentials.delete}
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <CredentialDialog open={dialogOpen} onOpenChange={setDialogOpen} onSaved={() => setDialogOpen(false)} credential={editingCredential} />
    </div>
  )
}
