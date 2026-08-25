import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'

export function useRuns(taskId: number | undefined, from: string | undefined, until: string | undefined) {
  return useQuery({
    queryKey: ['runs', taskId, from, until],
    queryFn: () => apiClient.runsAll(taskId, from, until),
  })
}

export function useTasksForFilter() {
  return useQuery({ queryKey: ['tasks', 'all'], queryFn: () => apiClient.tasksAll() })
}
