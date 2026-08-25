import * as RadixDialog from '@radix-ui/react-dialog'
import type { ReactNode } from 'react'
import { useStrings } from '../i18n'

interface ModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  children?: ReactNode
  footer?: ReactNode
}

export function Modal({ open, onOpenChange, title, description, children, footer }: ModalProps) {
  const strings = useStrings()

  return (
    <RadixDialog.Root open={open} onOpenChange={onOpenChange}>
      <RadixDialog.Portal>
        <RadixDialog.Overlay className="fixed inset-0 bg-black/50 data-[state=open]:animate-in data-[state=open]:fade-in" />
        <RadixDialog.Content
          className="fixed left-1/2 top-1/2 w-[min(90vw,32rem)] -translate-x-1/2 -translate-y-1/2
            rounded-jm border border-brand bg-bg-elevated p-6 shadow-jm-lg focus:outline-none"
        >
          <RadixDialog.Title className="text-base font-semibold text-fg">{title}</RadixDialog.Title>
          {description ? (
            <RadixDialog.Description className="mt-1 text-sm text-fg-muted">{description}</RadixDialog.Description>
          ) : null}
          <div className="mt-4">{children}</div>
          <div className="mt-6 flex justify-end gap-2">
            {footer ?? (
              <RadixDialog.Close asChild>
                <button
                  type="button"
                  className="rounded-jm border border-brand px-4 py-2 text-sm font-medium text-accent hover:bg-border-subtle"
                >
                  {strings.common.close}
                </button>
              </RadixDialog.Close>
            )}
          </div>
        </RadixDialog.Content>
      </RadixDialog.Portal>
    </RadixDialog.Root>
  )
}
