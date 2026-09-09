import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'

export const SUMMARY_QUERY_KEY = ['tasks', 'summary']
const GROUPS_QUERY_KEY = ['groups']

export function useTaskSummaries() {
  return useQuery({
    queryKey: SUMMARY_QUERY_KEY,
    queryFn: () => apiClient.summary(),
    // El evento "runFinished" de useLiveProgress ya invalida esto apenas una tarea
    // termina; el sondeo periódico es solo respaldo (reconexión de SignalR perdida,
    // horario disparado sin que la pestaña esté abierta, etc.).
    refetchInterval: 15_000,
  })
}

export function useTaskGroups() {
  return useQuery({
    queryKey: GROUPS_QUERY_KEY,
    queryFn: () => apiClient.groupsAll(),
  })
}

export function useTaskActions() {
  const queryClient = useQueryClient()
  const invalidateSummary = () => queryClient.invalidateQueries({ queryKey: SUMMARY_QUERY_KEY })

  const runAll = useMutation({ mutationFn: () => apiClient.runAll(), onSuccess: invalidateSummary })
  const run = useMutation({ mutationFn: (taskId: number) => apiClient.run(taskId), onSuccess: invalidateSummary })
  const pause = useMutation({ mutationFn: (taskId: number) => apiClient.pause(taskId) })
  const cancel = useMutation({ mutationFn: (taskId: number) => apiClient.cancel(taskId), onSuccess: invalidateSummary })
  const pauseAll = useMutation({ mutationFn: () => apiClient.pauseAll() })
  const cancelAll = useMutation({ mutationFn: () => apiClient.cancelAll(), onSuccess: invalidateSummary })
  const deleteTask = useMutation({ mutationFn: (taskId: number) => apiClient.tasksDELETE(taskId), onSuccess: invalidateSummary })

  return {
    runAll: runAll.mutateAsync,
    run: run.mutateAsync,
    pause: pause.mutateAsync,
    cancel: cancel.mutateAsync,
    pauseAll: pauseAll.mutateAsync,
    cancelAll: cancelAll.mutateAsync,
    deleteTask: deleteTask.mutateAsync,
  }
}
