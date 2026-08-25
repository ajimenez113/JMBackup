import { useEffect, useState } from 'react'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Input } from '../../../components/Input'
import { Select } from '../../../components/Select'
import { Switch } from '../../../components/Switch'
import { useStrings } from '../../../i18n'
import { SwaggerException } from '../../../lib/api-client'
import { useSecuritySettings } from '../useSettings'

const RISK_PHRASE = 'ENTIENDO EL RIESGO'

function isPasswordValid(password: string): boolean {
  return password.length >= 8 && /[A-Z]/.test(password) && /[a-z]/.test(password) && /[0-9]/.test(password)
}

export function SecuritySettingsTab() {
  const strings = useStrings()
  const { data, save, isSaving } = useSecuritySettings()

  const [username, setUsername] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [requireCredentialFor, setRequireCredentialFor] = useState('Both')
  const [allowUnauthenticatedLan, setAllowUnauthenticatedLan] = useState(false)
  const [riskConfirmationPhrase, setRiskConfirmationPhrase] = useState('')
  const [sessionInactivityMinutes, setSessionInactivityMinutes] = useState('30')
  const [error, setError] = useState<string>()
  const [savedNotice, setSavedNotice] = useState(false)

  useEffect(() => {
    if (!data) {
      return
    }

    setUsername(data.username ?? '')
    setRequireCredentialFor(data.requireCredentialFor)
    setAllowUnauthenticatedLan(data.allowUnauthenticatedLan)
    setSessionInactivityMinutes(String(data.sessionInactivityMinutes))
  }, [data])

  const passwordTouched = newPassword.length > 0
  const passwordValid = !passwordTouched || isPasswordValid(newPassword)
  const showRiskConfirmation = requireCredentialFor === 'None' && allowUnauthenticatedLan

  async function handleSave() {
    setError(undefined)
    setSavedNotice(false)

    try {
      await save({
        username: username || undefined,
        newPassword: newPassword || undefined,
        requireCredentialFor,
        sessionInactivityMinutes: Number(sessionInactivityMinutes),
        allowUnauthenticatedLan,
        riskConfirmationPhrase: showRiskConfirmation ? riskConfirmationPhrase : undefined,
      })
      setNewPassword('')
      setSavedNotice(true)
    } catch (err) {
      setError(SwaggerException.isSwaggerException(err) ? err.message : strings.common.error)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <Input label={strings.settings.security.username} value={username} onChange={(event) => setUsername(event.target.value)} />

      <Input
        type="password"
        label={strings.settings.security.newPassword}
        value={newPassword}
        onChange={(event) => setNewPassword(event.target.value)}
      />
      <p className={`text-xs ${passwordTouched && !passwordValid ? 'text-danger' : 'text-fg-muted'}`}>
        {passwordTouched && passwordValid ? strings.settings.security.passwordOk : strings.settings.security.passwordPolicy}
      </p>

      <Select
        label={strings.settings.security.requireCredentialFor}
        value={requireCredentialFor}
        onValueChange={setRequireCredentialFor}
        options={[
          { value: 'None', label: strings.settings.security.scopeNone },
          { value: 'WebOnly', label: strings.settings.security.scopeWebOnly },
          { value: 'AppOnly', label: strings.settings.security.scopeAppOnly },
          { value: 'Both', label: strings.settings.security.scopeBoth },
        ]}
      />

      {requireCredentialFor === 'None' ? (
        <Card className="border-danger bg-bg-canvas">
          <p className="text-sm text-danger">{strings.settings.security.riskWarning}</p>
          <div className="mt-2">
            <Switch
              id="allow-unauthenticated-lan"
              checked={allowUnauthenticatedLan}
              onCheckedChange={setAllowUnauthenticatedLan}
              label={strings.settings.security.allowUnauthenticatedLan}
            />
          </div>
          {showRiskConfirmation ? (
            <Input
              className="mt-2"
              label={strings.settings.security.riskConfirmationLabel}
              value={riskConfirmationPhrase}
              onChange={(event) => setRiskConfirmationPhrase(event.target.value)}
              placeholder={RISK_PHRASE}
            />
          ) : null}
        </Card>
      ) : null}

      <Input
        type="number"
        label={strings.settings.security.sessionInactivityMinutes}
        value={sessionInactivityMinutes}
        onChange={(event) => setSessionInactivityMinutes(event.target.value)}
      />

      {error ? <p className="text-sm text-danger">{error}</p> : null}

      <div className="flex items-center gap-2">
        <Button variant="primary" disabled={isSaving || (passwordTouched && !passwordValid)} onClick={() => void handleSave()}>
          {strings.settings.save}
        </Button>
        {savedNotice ? <span className="text-sm text-success">{strings.settings.saved}</span> : null}
      </div>
    </div>
  )
}
