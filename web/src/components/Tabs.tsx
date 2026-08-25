import * as RadixTabs from '@radix-ui/react-tabs'
import type { ReactNode } from 'react'

interface TabItem {
  value: string
  label: string
  content: ReactNode
}

interface TabsProps {
  items: TabItem[]
  defaultValue?: string
  value?: string
  onValueChange?: (value: string) => void
}

export function Tabs({ items, defaultValue, value, onValueChange }: TabsProps) {
  const firstValue = items[0]?.value
  return (
    <RadixTabs.Root
      defaultValue={defaultValue ?? firstValue}
      value={value}
      onValueChange={onValueChange}
      className="flex flex-col gap-4"
    >
      <RadixTabs.List className="flex gap-1 overflow-x-auto border-b border-border-subtle">
        {items.map((item) => (
          <RadixTabs.Trigger
            key={item.value}
            value={item.value}
            className="shrink-0 rounded-t-jm px-4 py-2 text-sm font-medium text-fg-muted
              border-b-2 border-transparent
              hover:text-fg
              data-[state=active]:border-brand data-[state=active]:text-accent
              focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            {item.label}
          </RadixTabs.Trigger>
        ))}
      </RadixTabs.List>
      {items.map((item) => (
        <RadixTabs.Content key={item.value} value={item.value}>
          {item.content}
        </RadixTabs.Content>
      ))}
    </RadixTabs.Root>
  )
}
