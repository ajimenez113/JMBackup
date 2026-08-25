import type { HTMLAttributes, ReactNode } from 'react'

interface CardProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  title?: ReactNode
  actions?: ReactNode
  noPadding?: boolean
}

export function Card({ title, actions, children, className = '', noPadding = false, ...props }: CardProps) {
  return (
    <div
      className={`rounded-jm border border-border-subtle bg-bg-elevated shadow-jm ${className}`}
      {...props}
    >
      {(title ?? actions) ? (
        <div className="flex items-center justify-between gap-3 border-b border-border-subtle px-5 py-3">
          {title ? <h2 className="text-sm font-semibold text-fg">{title}</h2> : <span />}
          {actions}
        </div>
      ) : null}
      <div className={noPadding ? '' : 'p-5'}>{children}</div>
    </div>
  )
}
