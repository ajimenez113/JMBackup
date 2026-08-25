import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { apiClient, onUnauthorized } from '../../lib/apiClient'
import type { LoginRequest } from '../../lib/api-client'
import { markAuthRequired } from './authGate'

const SESSION_QUERY_KEY = ['session']

export function useSession() {
  const queryClient = useQueryClient()

  const sessionQuery = useQuery({
    queryKey: SESSION_QUERY_KEY,
    queryFn: () => apiClient.session(),
  })

  useEffect(
    () =>
      onUnauthorized(() => {
        markAuthRequired()
        void queryClient.invalidateQueries({ queryKey: SESSION_QUERY_KEY })
      }),
    [queryClient],
  )

  const loginMutation = useMutation({
    mutationFn: (request: LoginRequest) => apiClient.login(request),
    onSuccess: (session) => queryClient.setQueryData(SESSION_QUERY_KEY, session),
  })

  const logoutMutation = useMutation({
    mutationFn: () => apiClient.logout(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: SESSION_QUERY_KEY })
    },
  })

  return {
    session: sessionQuery.data,
    isLoading: sessionQuery.isLoading,
    login: loginMutation.mutateAsync,
    isLoggingIn: loginMutation.isPending,
    logout: logoutMutation.mutateAsync,
  }
}
