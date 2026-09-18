export type Status = 'ok' | 'warning' | 'error' | 'running' | 'idle' | 'attention'

const STATUS_CLASSES: Record<Status, string> = {
  ok: 'bg-success',
  warning: 'bg-warning',
  error: 'bg-danger',
  running: 'bg-accent animate-pulse',
  idle: 'bg-idle',
  // Ni éxito ni error: la tarea corrió pero el origen no tenía nada que respaldar.
  // Color propio (violeta) para no confundirlo con "completado con errores" (ámbar).
  attention: 'bg-attention',
}

interface StatusDotProps {
  status: Status
  label?: string
}

export function StatusDot({ status, label }: StatusDotProps) {
  return (
    <span className="inline-flex items-center gap-2">
      <span className={`h-2.5 w-2.5 rounded-full ${STATUS_CLASSES[status]}`} aria-hidden="true" />
      {label ? <span className="text-sm text-fg">{label}</span> : <span className="sr-only">{status}</span>}
    </span>
  )
}
