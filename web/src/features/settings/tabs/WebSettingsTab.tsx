import { useEffect, useState } from 'react'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Input } from '../../../components/Input'
import { useStrings } from '../../../i18n'
import { useCertificateInfo, usePortCheck, useSecuritySettings, useWebSettings } from '../useSettings'

export function WebSettingsTab() {
  const strings = useStrings()
  const { data, save, isSaving } = useWebSettings()
  const certificateQuery = useCertificateInfo()
  const securityQuery = useSecuritySettings()

  const [listenAddress, setListenAddress] = useState('127.0.0.1')
  const [port, setPort] = useState('8483')
  const [checkedPort, setCheckedPort] = useState<number>()
  const [savedNotice, setSavedNotice] = useState(false)

  useEffect(() => {
    if (!data) {
      return
    }

    setListenAddress(data.listenAddress)
    setPort(String(data.port))
  }, [data])

  const portCheckQuery = usePortCheck(checkedPort ?? 0, listenAddress, checkedPort !== undefined)

  async function handleSave() {
    await save({ listenAddress, port: Number(port) })
    setSavedNotice(true)
  }

  const hasCredential = Boolean(securityQuery.data?.username)

  return (
    <div className="flex flex-col gap-4">
      {!hasCredential ? (
        <p className="rounded-jm border border-warning bg-warning/10 px-3 py-2 text-sm text-fg">
          {strings.settings.web.noCredentialWarning}
        </p>
      ) : null}
      <p className="text-xs text-fg-muted">{strings.settings.web.restartRequiredNotice}</p>

      <Input label={strings.settings.web.listenAddress} value={listenAddress} onChange={(event) => setListenAddress(event.target.value)} />

      <Input
        type="number"
        label={strings.settings.web.port}
        value={port}
        onChange={(event) => setPort(event.target.value)}
        onBlur={() => setCheckedPort(Number(port))}
      />
      {checkedPort !== undefined ? (
        <p className="text-xs text-fg-muted">
          {portCheckQuery.isLoading
            ? strings.settings.web.portChecking
            : portCheckQuery.data?.isAvailable
              ? strings.settings.web.portAvailable
              : `${strings.settings.web.portTaken} ${portCheckQuery.data?.suggestedPort ? `${strings.settings.web.portSuggestion}: ${portCheckQuery.data.suggestedPort}` : ''}`}
        </p>
      ) : null}

      <div className="flex items-center gap-2">
        <Button variant="primary" disabled={isSaving} onClick={() => void handleSave()}>
          {strings.settings.save}
        </Button>
        {savedNotice ? <span className="text-sm text-success">{strings.settings.saved}</span> : null}
      </div>

      <hr className="border-border-subtle" />

      <Card title={strings.settings.web.certificateTitle}>
        {certificateQuery.data ? (
          <dl className="flex flex-col gap-1 text-sm">
            <div className="flex justify-between gap-4">
              <dt className="text-fg-muted">{strings.settings.web.certificateSubject}</dt>
              <dd className="truncate text-fg">{certificateQuery.data.subject}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-fg-muted">{strings.settings.web.certificateThumbprint}</dt>
              <dd className="truncate text-fg">{certificateQuery.data.thumbprint}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-fg-muted">{strings.settings.web.certificateExpiry}</dt>
              <dd className="text-fg">{new Date(certificateQuery.data.notAfter).toLocaleDateString()}</dd>
            </div>
          </dl>
        ) : null}
        <a href="/api/settings/certificate/download" className="mt-3 inline-block text-sm text-accent underline">
          {strings.settings.web.certificateDownload}
        </a>
        <p className="mt-2 text-xs text-fg-muted">{strings.settings.web.certificateNotice}</p>
      </Card>
    </div>
  )
}
