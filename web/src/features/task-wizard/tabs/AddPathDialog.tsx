import { useEffect, useRef, useState, type DragEvent } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Modal } from '../../../components/Modal'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import type { CredentialResponse } from '../../../lib/api-client'
import { isNativeBridgeAvailable, onDroppedPaths, pickFile, pickFolder } from '../../../lib/nativeBridge'
import { useCredentials } from '../useCredentials'
import { CredentialDialog } from './CredentialDialog'

export interface NewPathInput {
  backendType: string
  path: string
  credentialId: number | undefined
  encrypted: boolean
  region: string | undefined
  storageClass: string | undefined
  serverSideEncryption: boolean
}

interface AddPathDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onAdd: (input: NewPathInput) => void
}

const NEW_CREDENTIAL_VALUE = '__new__'
const NO_CREDENTIAL_VALUE = '__none__'
const STORAGE_CLASS_DEFAULT = '__default__'
const STORAGE_CLASSES = ['STANDARD_IA', 'GLACIER_IR', 'GLACIER', 'DEEP_ARCHIVE'] as const

type BackendTab = 'Local' | 'Ftp' | 'S3'

/**
 * RF-21/RF-23: en el navegador ni el explorador ni arrastrar-y-soltar entregan una
 * ruta absoluta real (por seguridad, el navegador solo expone el nombre) — ahí se
 * precarga el campo de ruta manual con el nombre obtenido y se avisa que hay que
 * completarla a mano. Dentro del shell de escritorio (fase 4), el puente nativo
 * entrega la ruta absoluta real en los dos casos, así que el campo queda listo tal
 * cual. FTP y S3 arman la cadena de ruta en el formato que interpreta
 * StorageBackendFactory ("servidor[:puerto]/ruta" y "bucket/prefijo").
 */
export function AddPathDialog({ open, onOpenChange, onAdd }: AddPathDialogProps) {
  const strings = useStrings()
  const credentialsQuery = useCredentials()
  const credentials = credentialsQuery.data ?? []

  const [tab, setTab] = useState<BackendTab>('Local')

  const [path, setPath] = useState('')
  const [showBrowserNotice, setShowBrowserNotice] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const nativeBridgeAvailable = isNativeBridgeAvailable()

  const [ftpHost, setFtpHost] = useState('')
  const [ftpPort, setFtpPort] = useState('21')
  const [ftpRemotePath, setFtpRemotePath] = useState('')
  const [ftpEncrypted, setFtpEncrypted] = useState(false)
  const [ftpCredentialId, setFtpCredentialId] = useState<number>()

  const [s3Bucket, setS3Bucket] = useState('')
  const [s3Prefix, setS3Prefix] = useState('')
  const [s3Region, setS3Region] = useState('')
  const [s3StorageClass, setS3StorageClass] = useState(STORAGE_CLASS_DEFAULT)
  const [s3ServerSideEncryption, setS3ServerSideEncryption] = useState(false)
  const [s3CredentialId, setS3CredentialId] = useState<number>()

  const [credentialDialogOpen, setCredentialDialogOpen] = useState(false)

  useEffect(() => {
    if (!open) {
      return
    }

    return onDroppedPaths((paths) => {
      const firstPath = paths[0]
      if (firstPath) {
        setPath(firstPath)
        setShowBrowserNotice(false)
      }
    })
  }, [open])

  useEffect(() => {
    if (!open) {
      setTab('Local')
    }
  }, [open])

  function resetAll() {
    setPath('')
    setShowBrowserNotice(false)
    setFtpHost('')
    setFtpPort('21')
    setFtpRemotePath('')
    setFtpEncrypted(false)
    setFtpCredentialId(undefined)
    setS3Bucket('')
    setS3Prefix('')
    setS3Region('')
    setS3StorageClass(STORAGE_CLASS_DEFAULT)
    setS3ServerSideEncryption(false)
    setS3CredentialId(undefined)
  }

  function applyBrowserName(name: string) {
    setPath(name)
    setShowBrowserNotice(true)
  }

  async function handlePickFolder() {
    const picked = await pickFolder()
    if (picked) {
      setPath(picked)
      setShowBrowserNotice(false)
    }
  }

  async function handlePickFile() {
    const picked = await pickFile()
    if (picked) {
      setPath(picked)
      setShowBrowserNotice(false)
    }
  }

  function handleExplorerClick() {
    fileInputRef.current?.click()
  }

  function handleFileInputChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) {
      return
    }

    const topLevelName = file.webkitRelativePath ? file.webkitRelativePath.split('/')[0] : file.name
    applyBrowserName(topLevelName ?? file.name)
    event.target.value = ''
  }

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    // Con el puente nativo disponible, la ruta real la entrega onDroppedPaths (ver
    // MainWindow_PreviewDrop en el shell) — este handler del navegador solo aplica
    // cuando la app corre en un navegador común.
    event.preventDefault()
    if (nativeBridgeAvailable) {
      return
    }

    const file = event.dataTransfer.files[0]
    if (file) {
      applyBrowserName(file.name)
    }
  }

  function credentialOptionsFor(backendType: 'Ftp' | 'S3') {
    return [
      { value: NO_CREDENTIAL_VALUE, label: strings.wizard.files.credentialNone },
      ...credentials
        .filter((credential: CredentialResponse) => credential.backendType === backendType)
        .map((credential: CredentialResponse) => ({ value: String(credential.id), label: credential.alias })),
      { value: NEW_CREDENTIAL_VALUE, label: strings.wizard.files.credentialNew },
    ]
  }

  function handleCredentialChange(setter: (id: number | undefined) => void, value: string) {
    if (value === NEW_CREDENTIAL_VALUE) {
      setCredentialDialogOpen(true)
      return
    }

    setter(value === NO_CREDENTIAL_VALUE ? undefined : Number(value))
  }

  function handleCredentialSaved(credential: CredentialResponse) {
    if (tab === 'Ftp') {
      setFtpCredentialId(credential.id)
    } else if (tab === 'S3') {
      setS3CredentialId(credential.id)
    }
  }

  const isLocalValid = path.trim().length > 0
  const isFtpValid = ftpHost.trim().length > 0 && ftpRemotePath.trim().length > 0 && ftpCredentialId !== undefined
  const isS3Valid = s3Bucket.trim().length > 0 && s3Region.trim().length > 0 && s3CredentialId !== undefined
  const canAdd = tab === 'Local' ? isLocalValid : tab === 'Ftp' ? isFtpValid : isS3Valid

  function handleAdd() {
    if (!canAdd) {
      return
    }

    if (tab === 'Local') {
      onAdd({
        backendType: 'Local',
        path: path.trim(),
        credentialId: undefined,
        encrypted: false,
        region: undefined,
        storageClass: undefined,
        serverSideEncryption: false,
      })
    } else if (tab === 'Ftp') {
      const port = ftpPort.trim()
      const hostAndPort = port && port !== '21' ? `${ftpHost.trim()}:${port}` : ftpHost.trim()
      onAdd({
        backendType: 'Ftp',
        path: `${hostAndPort}/${ftpRemotePath.trim().replace(/^\/+/, '')}`,
        credentialId: ftpCredentialId,
        encrypted: ftpEncrypted,
        region: undefined,
        storageClass: undefined,
        serverSideEncryption: false,
      })
    } else {
      const prefix = s3Prefix.trim().replace(/^\/+/, '')
      onAdd({
        backendType: 'S3',
        path: prefix ? `${s3Bucket.trim()}/${prefix}` : s3Bucket.trim(),
        credentialId: s3CredentialId,
        encrypted: false,
        region: s3Region.trim(),
        storageClass: s3StorageClass === STORAGE_CLASS_DEFAULT ? undefined : s3StorageClass,
        serverSideEncryption: s3ServerSideEncryption,
      })
    }

    resetAll()
    onOpenChange(false)
  }

  const isGlacierSelected = s3StorageClass === 'GLACIER' || s3StorageClass === 'DEEP_ARCHIVE'

  return (
    <Modal open={open} onOpenChange={onOpenChange} title={strings.wizard.files.dialogTitle}>
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap gap-2">
          <Button variant={tab === 'Local' ? 'primary' : 'ghost'} onClick={() => setTab('Local')}>
            {strings.wizard.files.addLocalTab}
          </Button>
          <Button variant={tab === 'Ftp' ? 'primary' : 'ghost'} onClick={() => setTab('Ftp')}>
            {strings.wizard.files.addFtp}
          </Button>
          <Button variant={tab === 'S3' ? 'primary' : 'ghost'} onClick={() => setTab('S3')}>
            {strings.wizard.files.addS3}
          </Button>
        </div>

        {tab === 'Local' ? (
          <>
            <div className="flex flex-wrap gap-2">
              {nativeBridgeAvailable ? (
                <>
                  <Button onClick={() => void handlePickFolder()}>{strings.wizard.files.addExplorerFolder}</Button>
                  <Button onClick={() => void handlePickFile()}>{strings.wizard.files.addExplorerFile}</Button>
                </>
              ) : (
                <Button onClick={handleExplorerClick}>{strings.wizard.files.addExplorer}</Button>
              )}
            </div>
            {nativeBridgeAvailable ? null : (
              <input
                ref={fileInputRef}
                type="file"
                className="hidden"
                // @ts-expect-error -- webkitdirectory no está en el tipo estándar de input, pero sí en los navegadores basados en Chromium/Firefox/Safari modernos.
                webkitdirectory=""
                onChange={handleFileInputChange}
              />
            )}

            <div
              onDragOver={(event) => event.preventDefault()}
              onDrop={handleDrop}
              className="rounded-jm border-2 border-dashed border-brand p-6 text-center text-sm text-fg-muted"
            >
              {strings.wizard.files.dropHint}
            </div>

            <Input
              label={strings.wizard.files.manualPathLabel}
              value={path}
              onChange={(event) => {
                setPath(event.target.value)
                setShowBrowserNotice(false)
              }}
            />
            {showBrowserNotice ? <p className="text-xs text-warning">{strings.wizard.files.dropBrowserNotice}</p> : null}
          </>
        ) : null}

        {tab === 'Ftp' ? (
          <>
            <div className="flex gap-2">
              <div className="flex-1">
                <Input label={strings.wizard.files.ftpHost} value={ftpHost} onChange={(event) => setFtpHost(event.target.value)} />
              </div>
              <div className="w-24">
                <Input
                  label={strings.wizard.files.ftpPort}
                  type="number"
                  value={ftpPort}
                  onChange={(event) => setFtpPort(event.target.value)}
                />
              </div>
            </div>
            <Input
              label={strings.wizard.files.ftpRemotePath}
              value={ftpRemotePath}
              onChange={(event) => setFtpRemotePath(event.target.value)}
            />
            <Switch
              id="ftp-encrypted"
              checked={ftpEncrypted}
              onCheckedChange={setFtpEncrypted}
              label={strings.wizard.files.ftpEncrypted}
            />
            <Select
              label={strings.wizard.files.credentialLabel}
              value={ftpCredentialId !== undefined ? String(ftpCredentialId) : NO_CREDENTIAL_VALUE}
              onValueChange={(value) => handleCredentialChange(setFtpCredentialId, value)}
              options={credentialOptionsFor('Ftp')}
            />
          </>
        ) : null}

        {tab === 'S3' ? (
          <>
            <Input label={strings.wizard.files.s3Bucket} value={s3Bucket} onChange={(event) => setS3Bucket(event.target.value)} />
            <Input label={strings.wizard.files.s3Prefix} value={s3Prefix} onChange={(event) => setS3Prefix(event.target.value)} />
            <Input label={strings.wizard.files.s3Region} value={s3Region} onChange={(event) => setS3Region(event.target.value)} />
            <Select
              label={strings.wizard.files.s3StorageClass}
              value={s3StorageClass}
              onValueChange={setS3StorageClass}
              options={[
                { value: STORAGE_CLASS_DEFAULT, label: strings.wizard.files.s3StorageClassStandard },
                ...STORAGE_CLASSES.map((value) => ({ value, label: strings.wizard.files.s3StorageClassLabels[value] })),
              ]}
            />
            {isGlacierSelected ? <p className="text-xs text-warning">{strings.wizard.files.s3GlacierWarning}</p> : null}
            <Switch
              id="s3-sse"
              checked={s3ServerSideEncryption}
              onCheckedChange={setS3ServerSideEncryption}
              label={strings.wizard.files.s3ServerSideEncryption}
            />
            <Select
              label={strings.wizard.files.credentialLabel}
              value={s3CredentialId !== undefined ? String(s3CredentialId) : NO_CREDENTIAL_VALUE}
              onValueChange={(value) => handleCredentialChange(setS3CredentialId, value)}
              options={credentialOptionsFor('S3')}
            />
          </>
        ) : null}

        <div className="flex justify-end">
          <Button variant="primary" onClick={handleAdd} disabled={!canAdd}>
            {strings.wizard.add}
          </Button>
        </div>
      </div>

      <CredentialDialog
        open={credentialDialogOpen}
        onOpenChange={setCredentialDialogOpen}
        onSaved={handleCredentialSaved}
        fixedBackendType={tab === 'Ftp' || tab === 'S3' ? tab : undefined}
      />
    </Modal>
  )
}
