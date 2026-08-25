import { Navigate, Outlet } from 'react-router-dom'
import { useSession } from '../features/auth/useSession'
import { useAuthRequired } from '../features/auth/authGate'

/**
 * Sin credencial configurada (RF-105) la API no exige sesión: por eso esto no
 * redirige a /login hasta que una respuesta 401 real demuestre que hace falta
 * (ver authGate.ts). El costo es que, si sí hace falta, la primera pantalla
 * protegida hace un pedido de más antes de redirigir — el servidor ya lo rechaza
 * igual, esto es una comodidad de navegación, no el control de acceso real.
 */
export function ProtectedRoute() {
  const authRequired = useAuthRequired()
  const { session } = useSession()

  if (authRequired && !session?.isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
