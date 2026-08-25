import { NavLink, Outlet } from 'react-router-dom'
import { Button } from '../components/Button'
import { useStrings } from '../i18n'
import { ThemeSwitcher } from '../theme/ThemeSwitcher'
import { useSession } from '../features/auth/useSession'

function NavItem({ to, label }: { to: string; label: string }) {
  return (
    <NavLink
      to={to}
      end={to === '/'}
      className={({ isActive }) =>
        `rounded-jm px-2 py-1 text-sm font-medium ${isActive ? 'text-accent' : 'text-fg-muted hover:text-fg'}`
      }
    >
      {label}
    </NavLink>
  )
}

export function AppShell() {
  const strings = useStrings()
  const { session, logout } = useSession()

  return (
    <div className="flex min-h-screen flex-col bg-bg-canvas">
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-brand bg-bg-elevated px-4 py-2">
        <div className="flex flex-wrap items-center gap-4">
          <span className="text-sm font-semibold text-accent">JMBackup</span>
          <nav className="flex items-center gap-1">
            <NavItem to="/" label={strings.tasks.title} />
            <NavItem to="/history" label={strings.history.title} />
          </nav>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <ThemeSwitcher />
          {session?.isAuthenticated ? (
            <div className="flex items-center gap-2 text-sm text-fg-muted">
              <span>
                {strings.auth.loggedInAs} {session.username}
              </span>
              <Button variant="ghost" size="sm" onClick={() => void logout()}>
                {strings.auth.logout}
              </Button>
            </div>
          ) : null}
        </div>
      </header>
      <main className="flex flex-1 flex-col">
        <Outlet />
      </main>
    </div>
  )
}
