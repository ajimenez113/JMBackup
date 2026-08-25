import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type { FilterRequest } from '../../lib/api-client'

function filtersKey(taskId: number) {
  return ['tasks', taskId, 'filters']
}

export function useFilters(taskId: number | undefined) {
  return useQuery({
    queryKey: taskId !== undefined ? filtersKey(taskId) : ['tasks', 'filters', 'none'],
    queryFn: () => apiClient.filtersAll(taskId!),
    enabled: taskId !== undefined,
  })
}

export function useFilterCrud(taskId: number) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: filtersKey(taskId) })

  const add = useMutation({
    mutationFn: (request: FilterRequest) => apiClient.filtersPOST(taskId, request),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ filterId, request }: { filterId: number; request: FilterRequest }) =>
      apiClient.filtersPUT(taskId, filterId, request),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (filterId: number) => apiClient.filtersDELETE(taskId, filterId),
    onSuccess: invalidate,
  })

  return { add: add.mutateAsync, update: update.mutateAsync, remove: remove.mutateAsync }
}
