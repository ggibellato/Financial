import { apiClient } from '../api/financialApiClient'
import type { DataQualityReportDto } from '../api/types'
import { useAsyncResource } from './useAsyncResource'

export interface DataQualityReportData {
  report: DataQualityReportDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

export function useDataQualityReport(): DataQualityReportData {
  const { data, isLoading, error, retry } = useAsyncResource<DataQualityReportDto>(
    () => apiClient.getDataQualityReport(),
    [],
    'Unable to load the data-quality report',
  )

  return { report: data, isLoading, error, retry }
}
