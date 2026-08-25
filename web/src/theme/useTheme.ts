import { useContext } from 'react'
import { ThemeContext } from './ThemeContext'

export function useTheme() {
  const context = useContext(ThemeContext)
  if (!context) {
    throw new Error('useTheme se tiene que usar dentro de <ThemeProvider>.')
  }

  return context
}
