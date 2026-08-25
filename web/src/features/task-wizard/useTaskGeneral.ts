import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type { CreateTaskRequest, TaskGroupRequest, UpdateTaskRequest } from '../../lib/api-client'

const SUMMARY_QUERY_KEY = ['tasks', 'summary']
const GROUPS_QUERY_KEY = ['groups']

export function useTask(taskId: number | undefined) {
  return useQuery({
    queryKey: ['tasks', taskId],
    queryFn: () => apiClient.tasksGET(taskId!),
    enabled: taskId !== undefined,
  })
}

export function useTaskCrud() {
  const queryClient = useQueryClient()
  const invalidateSummary = () => queryClient.invalidateQueries({ queryKey: SUMMARY_QUERY_KEY })

  const create = useMutation({
    mutationFn: (request: CreateTaskRequest) => apiClient.tasksPOST(request),
    onSuccess: invalidateSummary,
  })

  const update = useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateTaskRequest }) => apiClient.tasksPUT(id, request),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: ['tasks', id] })
      void invalidateSummary()
    },
  })

  return { create: create.mutateAsync, update: update.mutateAsync }
}

export function useTaskGroupCrud() {
  const queryClient = useQueryClient()

  const create = useMutation({
    mutationFn: (request: TaskGroupRequest) => apiClient.groupsPOST(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: GROUPS_QUERY_KEY }),
  })

  return { create: create.mutateAsync }
}
