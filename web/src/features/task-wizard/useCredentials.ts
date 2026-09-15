import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type { CredentialRequest, CredentialUpdateRequest } from '../../lib/api-client'

const CREDENTIALS_QUERY_KEY = ['credentials']

export function useCredentials() {
  return useQuery({
    queryKey: CREDENTIALS_QUERY_KEY,
    queryFn: () => apiClient.credentialsAll(),
  })
}

export function useCreateCredential() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CredentialRequest) => apiClient.credentialsPOST(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CREDENTIALS_QUERY_KEY }),
  })
}

export function useUpdateCredential() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: CredentialUpdateRequest }) => apiClient.credentialsPUT(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CREDENTIALS_QUERY_KEY }),
  })
}

export function useDeleteCredential() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => apiClient.credentialsDELETE(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CREDENTIALS_QUERY_KEY }),
  })
}
