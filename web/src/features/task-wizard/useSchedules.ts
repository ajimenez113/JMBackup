import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type { ScheduleRequest } from '../../lib/api-client'

function schedulesKey(taskId: number) {
  return ['tasks', taskId, 'schedules']
}

export function useSchedules(taskId: number | undefined) {
  return useQuery({
    queryKey: taskId !== undefined ? schedulesKey(taskId) : ['tasks', 'schedules', 'none'],
    queryFn: () => apiClient.schedulesAll(taskId!),
    enabled: taskId !== undefined,
  })
}

export function useScheduleCrud(taskId: number) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: schedulesKey(taskId) })

  const add = useMutation({
    mutationFn: (request: ScheduleRequest) => apiClient.schedulesPOST(taskId, request),
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: ({ scheduleId, request }: { scheduleId: number; request: ScheduleRequest }) =>
      apiClient.schedulesPUT(taskId, scheduleId, request),
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: (scheduleId: number) => apiClient.schedulesDELETE(taskId, scheduleId),
    onSuccess: invalidate,
  })

  return { add: add.mutateAsync, update: update.mutateAsync, remove: remove.mutateAsync }
}
