import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ThemeContext, type EffectiveTheme, type ThemeContextValue, type ThemeSetting } from './ThemeContext'

const STORAGE_KEY = 'jmbackup.theme'

function readStoredSetting(): ThemeSetting {
  const stored = localStorage.getItem(STORAGE_KEY)
  return stored === 'light' || stored === 'dark' || stored === 'system' ? stored : 'system'
}

function systemPrefersDark(): boolean {
  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [setting, setSettingState] = useState<ThemeSetting>(readStoredSetting)
  const [systemIsDark, setSystemIsDark] = useState<boolean>(systemPrefersDark)

  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = (event: MediaQueryListEvent) => setSystemIsDark(event.matches)
    media.addEventListener('change', onChange)
    return () => media.removeEventListener('change', onChange)
  }, [])

  useEffect(() => {
    const root = document.documentElement
    if (setting === 'system') {
      // Sin atributo, el CSS resuelve solo por prefers-color-scheme (tokens.css):
      // un cambio de tema del sistema se refleja sin que este efecto vuelva a correr.
      root.removeAttribute('data-theme')
    } else {
      root.setAttribute('data-theme', setting)
    }
  }, [setting])

  const setSetting = useCallback((next: ThemeSetting) => {
    localStorage.setItem(STORAGE_KEY, next)
    setSettingState(next)
  }, [])

  const effective: EffectiveTheme = setting === 'system' ? (systemIsDark ? 'dark' : 'light') : setting

  const value = useMemo<ThemeContextValue>(() => ({ setting, effective, setSetting }), [setting, effective, setSetting])

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}
