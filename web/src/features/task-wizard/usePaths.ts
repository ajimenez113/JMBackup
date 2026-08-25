import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type { TaskPathRequest } from '../../lib/api-client'

function pathsKey(taskId: number) {
  return ['tasks', taskId, 'paths']
}

export function usePaths(taskId: number | undefined) {
  return useQuery({
    queryKey: taskId !== undefined ? pathsKey(taskId) : ['tasks', 'paths', 'none'],
    queryFn: () => apiClient.pathsAll(taskId!),
    enabled: taskId !== undefined,
  })
}

export function usePathCrud(taskId: number) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: pathsKey(taskId) })

  const add = useMutation({
    mutationFn: (request: TaskPathRequest) => apiClient.pathsPOST(taskId, request),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ pathId, request }: { pathId: number; request: TaskPathRequest }) =>
      apiClient.pathsPUT(taskId, pathId, request),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (pathId: number) => apiClient.pathsDELETE(taskId, pathId),
    onSuccess: invalidate,
  })

  return { add: add.mutateAsync, update: update.mutateAsync, remove: remove.mutateAsync }
}

export function useTestConnection(taskId: number) {
  return useMutation({
    mutationFn: (pathId: number) => apiClient.testConnection(taskId, pathId),
  })
}
