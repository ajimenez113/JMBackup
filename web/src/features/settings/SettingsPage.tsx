import { Card } from '../../components/Card'
import { Tabs } from '../../components/Tabs'
import { useStrings } from '../../i18n'
import { GeneralSettingsTab } from './tabs/GeneralSettingsTab'
import { SecuritySettingsTab } from './tabs/SecuritySettingsTab'
import { TransferSettingsTab } from './tabs/TransferSettingsTab'
import { WebSettingsTab } from './tabs/WebSettingsTab'

export function SettingsPage() {
  const strings = useStrings()

  return (
    <div className="p-4">
      <Card title={strings.settings.title}>
        <Tabs
          items={[
            { value: 'general', label: strings.settings.tabGeneral, content: <GeneralSettingsTab /> },
            { value: 'transfer', label: strings.settings.tabTransfer, content: <TransferSettingsTab /> },
            { value: 'security', label: strings.settings.tabSecurity, content: <SecuritySettingsTab /> },
            { value: 'web', label: strings.settings.tabWeb, content: <WebSettingsTab /> },
          ]}
        />
      </Card>
    </div>
  )
}
