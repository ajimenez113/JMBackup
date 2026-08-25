import { Client } from './api-client'

interface AntiforgeryTokenResponse {
  token: string
}

const MUTATING_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])
const ANTIFORGERY_EXEMPT_PATHS = ['/api/auth/login']

let antiforgeryToken: string | null = null
const unauthorizedListeners = new Set<() => void>()

/** El shell de la app se suscribe acá para reaccionar apenas una respuesta da 401. */
export function onUnauthorized(listener: () => void): () => void {
  unauthorizedListeners.add(listener)
  return () => {
    unauthorizedListeners.delete(listener)
  }
}

async function fetchAntiforgeryToken(): Promise<string> {
  const response = await fetch('/api/antiforgery/token', { credentials: 'include' })
  const body = (await response.json()) as AntiforgeryTokenResponse
  return body.token
}

async function ensureAntiforgeryToken(): Promise<string> {
  antiforgeryToken ??= await fetchAntiforgeryToken()
  return antiforgeryToken
}

const httpAdapter = {
  async fetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
    const method = (init?.method ?? 'GET').toUpperCase()
    const path = typeof url === 'string' ? url : url.url
    const headers = new Headers(init?.headers)

    if (MUTATING_METHODS.has(method) && !ANTIFORGERY_EXEMPT_PATHS.some((exempt) => path.includes(exempt))) {
      headers.set('X-XSRF-TOKEN', await ensureAntiforgeryToken())
    }

    const response = await fetch(url, { ...init, headers, credentials: 'include' })

    if (response.status === 401) {
      unauthorizedListeners.forEach((listener) => listener())
    }

    return response
  },
}

export const apiClient = new Client('', httpAdapter)
