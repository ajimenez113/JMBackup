import { useEffect, useRef, useState, type DragEvent } from 'react'
import { Button } from '../../../components/Button'
import { Input } from '../../../components/Input'
import { Modal } from '../../../components/Modal'
import { useStrings } from '../../../i18n'
import { isNativeBridgeAvailable, onDroppedPaths, pickFile, pickFolder } from '../../../lib/nativeBridge'

interface AddPathDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onAdd: (path: string) => void
}

/**
 * RF-21/RF-23: en el navegador ni el explorador ni arrastrar-y-soltar entregan una
 * ruta absoluta real (por seguridad, el navegador solo expone el nombre) — ahí se
 * precarga el campo de ruta manual con el nombre obtenido y se avisa que hay que
 * completarla a mano. Dentro del shell de escritorio (fase 4), el puente nativo
 * entrega la ruta absoluta real en los dos casos, así que el campo queda listo tal
 * cual.
 */
export function AddPathDialog({ open, onOpenChange, onAdd }: AddPathDialogProps) {
  const strings = useStrings()
  const [path, setPath] = useState('')
  const [showBrowserNotice, setShowBrowserNotice] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const nativeBridgeAvailable = isNativeBridgeAvailable()

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

  function handleAdd() {
    if (!path.trim()) {
      return
    }

    onAdd(path.trim())
    setPath('')
    setShowBrowserNotice(false)
    onOpenChange(false)
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange} title={strings.wizard.files.dialogTitle}>
      <div className="flex flex-col gap-4">
        <div className="flex flex-wrap gap-2">
          {nativeBridgeAvailable ? (
            <>
              <Button onClick={handlePickFolder}>{strings.wizard.files.addExplorerFolder}</Button>
              <Button onClick={handlePickFile}>{strings.wizard.files.addExplorerFile}</Button>
            </>
          ) : (
            <Button onClick={handleExplorerClick}>{strings.wizard.files.addExplorer}</Button>
          )}
          <Button variant="ghost" disabled>
            {strings.wizard.files.addFtp}
          </Button>
          <Button variant="ghost" disabled>
            {strings.wizard.files.addS3}
          </Button>
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

        <div className="flex justify-end">
          <Button variant="primary" onClick={handleAdd}>
            {strings.wizard.add}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
