import { useEffect, useState } from 'react'

/**
 * Sin credencial configurada (RF-105), la API es accesible sin sesión — por eso
 * `ProtectedRoute` no puede decidir solo con `session.isAuthenticated`. Esto queda en
 * `false` hasta que una respuesta 401 real demuestre que hace falta iniciar sesión.
 */
let authRequired = false
const listeners = new Set<() => void>()

export function markAuthRequired(): void {
  if (authRequired) {
    return
  }

  authRequired = true
  listeners.forEach((listener) => listener())
}

export function useAuthRequired(): boolean {
  const [value, setValue] = useState(authRequired)

  useEffect(() => {
    const listener = () => setValue(true)
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  }, [])

  return value
}
