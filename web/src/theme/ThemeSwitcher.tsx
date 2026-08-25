import { Select } from '../components/Select'
import { useStrings } from '../i18n'
import { useTheme } from './useTheme'
import type { ThemeSetting } from './ThemeContext'

export function ThemeSwitcher() {
  const strings = useStrings()
  const { setting, setSetting } = useTheme()

  return (
    <Select
      value={setting}
      onValueChange={(value) => setSetting(value as ThemeSetting)}
      options={[
        { value: 'light', label: strings.theme.light },
        { value: 'dark', label: strings.theme.dark },
        { value: 'system', label: strings.theme.system },
      ]}
    />
  )
}
