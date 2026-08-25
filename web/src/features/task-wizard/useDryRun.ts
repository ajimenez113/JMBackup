import { useMutation } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'

export function useDryRun(taskId: number) {
  return useMutation({
    mutationFn: () => apiClient.dryRun(taskId),
  })
}
