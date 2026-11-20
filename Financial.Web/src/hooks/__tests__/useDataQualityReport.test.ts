import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { DataQualityReportDto } from '../../api/types'
import { useDataQualityReport } from '../useDataQualityReport'

const { getDataQualityReportMock } = vi.hoisted(() => ({
  getDataQualityReportMock: vi.fn<FinancialApiClient['getDataQualityReport']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDataQualityReport: getDataQualityReportMock,
  } as Partial<FinancialApiClient>,
}))

const REPORT: DataQualityReportDto = {
  salesExceedPurchases: [],
  unpricedOpenHoldings: [],
  openHoldingsMissingCostBasis: [],
  unresolvedTaxClassifications: [],
  staleValuationCount: 0,
  historicHoldingsStillOpen: [],
  unclassifiedHoldings: [],
  unclassifiedAndUnpricedOpenHoldings: [],
  corporateActionsAwaitingTaxReview: [],
}

describe('useDataQualityReport', () => {
  beforeEach(() => {
    getDataQualityReportMock.mockReset()
  })

  it('fetches_the_report_once_on_mount', async () => {
    getDataQualityReportMock.mockResolvedValue(REPORT)

    const { result } = renderHook(() => useDataQualityReport())

    await waitFor(() => expect(result.current.report).toEqual(REPORT))
    expect(getDataQualityReportMock).toHaveBeenCalledTimes(1)
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('exposes_the_failure_message_and_no_report_when_the_request_rejects', async () => {
    getDataQualityReportMock.mockRejectedValue(new Error('Service unavailable'))

    const { result } = renderHook(() => useDataQualityReport())

    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))
    expect(result.current.report).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })
})
