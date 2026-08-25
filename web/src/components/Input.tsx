import { forwardRef, useId, type InputHTMLAttributes } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, error, id, className = '', ...props },
  ref,
) {
  const generatedId = useId()
  const inputId = id ?? generatedId

  return (
    <div className="flex flex-col gap-1">
      {label ? (
        <label htmlFor={inputId} className="text-sm font-medium text-fg">
          {label}
        </label>
      ) : null}
      <input
        ref={ref}
        id={inputId}
        className={`rounded-jm border px-3 py-2 text-sm text-fg bg-bg-elevated
          placeholder:text-fg-muted
          focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent
          disabled:opacity-50 disabled:cursor-not-allowed
          ${error ? 'border-danger' : 'border-brand'} ${className}`}
        aria-invalid={error ? true : undefined}
        {...props}
      />
      {error ? <span className="text-xs text-danger">{error}</span> : null}
    </div>
  )
})
