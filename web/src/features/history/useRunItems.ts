import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'

export function useBackedUpItems(runId: number | undefined) {
  return useQuery({
    queryKey: ['runs', runId, 'backed-up'],
    queryFn: () => apiClient.backedUp(runId!),
    enabled: runId !== undefined,
  })
}

export function useErrorItems(runId: number | undefined) {
  return useQuery({
    queryKey: ['runs', runId, 'errors'],
    queryFn: () => apiClient.errors(runId!),
    enabled: runId !== undefined,
  })
}
