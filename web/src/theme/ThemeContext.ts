import { createContext } from 'react'

export type ThemeSetting = 'light' | 'dark' | 'system'
export type EffectiveTheme = 'light' | 'dark'

export interface ThemeContextValue {
  setting: ThemeSetting
  effective: EffectiveTheme
  setSetting: (setting: ThemeSetting) => void
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)
