import * as RadixSwitch from '@radix-ui/react-switch'

interface SwitchProps {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  disabled?: boolean
  label?: string
  id?: string
}

export function Switch({ checked, onCheckedChange, disabled, label, id }: SwitchProps) {
  return (
    <label htmlFor={id} className="inline-flex items-center gap-2 text-sm text-fg">
      <RadixSwitch.Root
        id={id}
        checked={checked}
        onCheckedChange={onCheckedChange}
        disabled={disabled}
        className="relative h-6 w-11 rounded-full border border-brand bg-transparent
          data-[state=checked]:bg-brand
          disabled:opacity-50 disabled:cursor-not-allowed
          focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
      >
        <RadixSwitch.Thumb
          className="block h-4 w-4 translate-x-1 rounded-full bg-brand transition-transform
            data-[state=checked]:translate-x-6 data-[state=checked]:bg-white"
        />
      </RadixSwitch.Root>
      {label ? <span>{label}</span> : null}
    </label>
  )
}
