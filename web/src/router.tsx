import { Route, Routes } from 'react-router-dom'
import { LoginPage } from './features/auth/LoginPage'
import { HistoryPage } from './features/history/HistoryPage'
import { SettingsPage } from './features/settings/SettingsPage'
import { HomePage } from './features/tasks/HomePage'
import { TaskWizardPage } from './features/task-wizard/TaskWizardPage'
import { AppShell } from './layout/AppShell'
import { ProtectedRoute } from './layout/ProtectedRoute'

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/settings" element={<SettingsPage />} />
          <Route path="/history" element={<HistoryPage />} />
          <Route path="/tasks/new" element={<TaskWizardPage />} />
          <Route path="/tasks/:id/edit" element={<TaskWizardPage />} />
        </Route>
      </Route>
    </Routes>
  )
}
