import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type { ExclusionRequest } from '../../lib/api-client'

function exclusionsKey(taskId: number) {
  return ['tasks', taskId, 'exclusions']
}

export function useExclusions(taskId: number | undefined) {
  return useQuery({
    queryKey: taskId !== undefined ? exclusionsKey(taskId) : ['tasks', 'exclusions', 'none'],
    queryFn: () => apiClient.exclusionsAll(taskId!),
    enabled: taskId !== undefined,
  })
}

export function useExclusionCrud(taskId: number) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: exclusionsKey(taskId) })

  const add = useMutation({
    mutationFn: (request: ExclusionRequest) => apiClient.exclusionsPOST(taskId, request),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ exclusionId, request }: { exclusionId: number; request: ExclusionRequest }) =>
      apiClient.exclusionsPUT(taskId, exclusionId, request),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (exclusionId: number) => apiClient.exclusionsDELETE(taskId, exclusionId),
    onSuccess: invalidate,
  })

  return { add: add.mutateAsync, update: update.mutateAsync, remove: remove.mutateAsync }
}
