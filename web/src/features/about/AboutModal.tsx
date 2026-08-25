import { Modal } from '../../components/Modal'
import { useStrings } from '../../i18n'

interface AboutModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AboutModal({ open, onOpenChange }: AboutModalProps) {
  const strings = useStrings()
  const webUrl = `${window.location.protocol}//${window.location.host}`

  return (
    <Modal open={open} onOpenChange={onOpenChange} title={strings.about.title}>
      <dl className="flex flex-col gap-2 text-sm text-fg">
        <div className="flex justify-between gap-4">
          <dt className="text-fg-muted">{strings.about.version}</dt>
          <dd>Hito 1 — fase 3</dd>
        </div>
        <div className="flex justify-between gap-4">
          <dt className="text-fg-muted">{strings.about.license}</dt>
          <dd>Uso interno</dd>
        </div>
        <div className="flex justify-between gap-4">
          <dt className="text-fg-muted">{strings.about.credits}</dt>
          <dd>JMBackup</dd>
        </div>
      </dl>
      <a href={webUrl} target="_blank" rel="noreferrer" className="mt-4 block text-sm text-accent underline">
        {strings.about.webLink}
      </a>
    </Modal>
  )
}
