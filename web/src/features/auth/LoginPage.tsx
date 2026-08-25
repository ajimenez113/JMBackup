import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { Input } from '../../components/Input'
import { useStrings } from '../../i18n'
import { SwaggerException } from '../../lib/api-client'
import { useSession } from './useSession'

export function LoginPage() {
  const strings = useStrings()
  const navigate = useNavigate()
  const { login, isLoggingIn } = useSession()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    try {
      await login({ username, password })
      navigate('/', { replace: true })
    } catch (err) {
      if (SwaggerException.isSwaggerException(err) && err.status === 401) {
        setError(strings.auth.invalidCredentials)
      } else if (SwaggerException.isSwaggerException(err) && err.status === 429) {
        setError(strings.auth.tooManyAttempts)
      } else {
        setError(strings.common.error)
      }
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-bg-canvas p-4">
      <Card title={strings.auth.loginTitle} className="w-full max-w-sm">
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            label={strings.auth.username}
            autoComplete="username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            required
          />
          <Input
            label={strings.auth.password}
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
          {error ? <p className="text-sm text-danger">{error}</p> : null}
          <Button type="submit" variant="primary" disabled={isLoggingIn}>
            {strings.auth.submit}
          </Button>
        </form>
      </Card>
    </div>
  )
}
