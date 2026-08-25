import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/apiClient'
import type {
  ExportedConfiguration,
  GeneralSettingsRequest,
  SecuritySettingsRequest,
  TransferSettingsRequest,
  WebSettingsRequest,
} from '../../lib/api-client'

export function useGeneralSettings() {
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['settings', 'general'], queryFn: () => apiClient.generalGET() })
  const save = useMutation({
    mutationFn: (request: GeneralSettingsRequest) => apiClient.generalPUT(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['settings', 'general'] }),
  })

  return { ...query, save: save.mutateAsync, isSaving: save.isPending }
}

export function useTransferSettings() {
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['settings', 'transfer'], queryFn: () => apiClient.transferGET() })
  const save = useMutation({
    mutationFn: (request: TransferSettingsRequest) => apiClient.transferPUT(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['settings', 'transfer'] }),
  })

  return { ...query, save: save.mutateAsync, isSaving: save.isPending }
}

export function useSecuritySettings() {
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['settings', 'security'], queryFn: () => apiClient.securityGET() })
  const save = useMutation({
    mutationFn: (request: SecuritySettingsRequest) => apiClient.securityPUT(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['settings', 'security'] }),
  })

  return { ...query, save: save.mutateAsync, isSaving: save.isPending }
}

export function useWebSettings() {
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['settings', 'web'], queryFn: () => apiClient.webGET() })
  const save = useMutation({
    mutationFn: (request: WebSettingsRequest) => apiClient.webPUT(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['settings', 'web'] }),
  })

  return { ...query, save: save.mutateAsync, isSaving: save.isPending }
}

export function useCertificateInfo() {
  return useQuery({ queryKey: ['settings', 'certificate'], queryFn: () => apiClient.certificate() })
}

export function usePortCheck(port: number, address: string, enabled: boolean) {
  return useQuery({
    queryKey: ['settings', 'web', 'port-check', port, address],
    queryFn: () => apiClient.portCheck(port, address),
    enabled,
  })
}

export function useConfigExportImport() {
  const exportConfig = useMutation({ mutationFn: () => apiClient.export() })
  const importConfig = useMutation({ mutationFn: (configuration: ExportedConfiguration) => apiClient.import(configuration) })

  return { exportConfig: exportConfig.mutateAsync, importConfig: importConfig.mutateAsync, isImporting: importConfig.isPending }
}
