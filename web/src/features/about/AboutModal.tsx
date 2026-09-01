import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Button } from '../../components/Button'
import { Modal } from '../../components/Modal'
import { useStrings } from '../../i18n'
import { apiClient } from '../../lib/apiClient'
import type { UpdateCheckResponse } from '../../lib/api-client'

interface AboutModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AboutModal({ open, onOpenChange }: AboutModalProps) {
  const strings = useStrings()
  const webUrl = `${window.location.protocol}//${window.location.host}`

  const versionQuery = useQuery({
    queryKey: ['version'],
    queryFn: () => apiClient.version(),
    enabled: open,
  })

  const [checkResult, setCheckResult] = useState<UpdateCheckResponse | null>(null)
  const [isChecking, setIsChecking] = useState(false)

  async function handleCheckForUpdates() {
    setIsChecking(true)
    try {
      setCheckResult(await apiClient.check())
    } finally {
      setIsChecking(false)
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange} title={strings.about.title}>
      <dl className="flex flex-col gap-2 text-sm text-fg">
        <div className="flex justify-between gap-4">
          <dt className="text-fg-muted">{strings.about.version}</dt>
          <dd>{versionQuery.data?.version ?? '—'}</dd>
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

      <div className="mt-4 flex flex-col gap-2 border-t border-border-subtle pt-4">
        <Button disabled={isChecking} onClick={() => void handleCheckForUpdates()}>
          {isChecking ? strings.about.checkingForUpdates : strings.about.checkForUpdates}
        </Button>
        {checkResult ? <CheckResultNotice result={checkResult} strings={strings} /> : null}
      </div>
    </Modal>
  )
}

function CheckResultNotice({ result, strings }: { result: UpdateCheckResponse; strings: ReturnType<typeof useStrings> }) {
  if (!result.configured) {
    return <p className="text-sm text-fg-muted">{strings.about.updatesNotConfigured}</p>
  }
  if (result.errorMessage) {
    return (
      <p className="text-sm text-danger">
        {strings.about.checkFailed} {result.errorMessage}
      </p>
    )
  }
  if (!result.updateAvailable) {
    return <p className="text-sm text-success">{strings.about.upToDate}</p>
  }

  return (
    <p className="text-sm text-fg">
      {strings.about.updateAvailable}: {result.latestVersion}
      {result.downloadUrl ? (
        <>
          {' — '}
          <a href={result.downloadUrl} target="_blank" rel="noreferrer" className="text-accent underline">
            {strings.about.updateDownloadLink}
          </a>
        </>
      ) : null}
    </p>
  )
}
