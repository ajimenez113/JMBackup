import * as RadixSelect from '@radix-ui/react-select'

export interface SelectOption {
  value: string
  label: string
}

interface SelectProps {
  value: string
  onValueChange: (value: string) => void
  options: SelectOption[]
  label?: string
  placeholder?: string
  disabled?: boolean
}

export function Select({ value, onValueChange, options, label, placeholder, disabled }: SelectProps) {
  return (
    <div className="flex flex-col gap-1">
      {label ? <span className="text-sm font-medium text-fg">{label}</span> : null}
      <RadixSelect.Root value={value} onValueChange={onValueChange} disabled={disabled}>
        <RadixSelect.Trigger
          className="flex items-center justify-between gap-2 rounded-jm border border-brand bg-bg-elevated
            px-3 py-2 text-sm text-fg
            focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent
            disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <RadixSelect.Value placeholder={placeholder} />
          <RadixSelect.Icon>▾</RadixSelect.Icon>
        </RadixSelect.Trigger>
        <RadixSelect.Portal>
          <RadixSelect.Content className="z-50 overflow-hidden rounded-jm border border-brand bg-bg-elevated shadow-jm-lg">
            <RadixSelect.Viewport className="p-1">
              {options.map((option) => (
                <RadixSelect.Item
                  key={option.value}
                  value={option.value}
                  className="cursor-pointer rounded-jm px-3 py-2 text-sm text-fg outline-none
                    data-[highlighted]:bg-border-subtle data-[state=checked]:text-accent"
                >
                  <RadixSelect.ItemText>{option.label}</RadixSelect.ItemText>
                </RadixSelect.Item>
              ))}
            </RadixSelect.Viewport>
          </RadixSelect.Content>
        </RadixSelect.Portal>
      </RadixSelect.Root>
    </div>
  )
}
