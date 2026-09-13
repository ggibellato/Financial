import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { useReportingCurrency } from '../useReportingCurrency'

const { getReportingCurrencyMock, setReportingCurrencyMock } = vi.hoisted(() => ({
  getReportingCurrencyMock: vi.fn<FinancialApiClient['getReportingCurrency']>(),
  setReportingCurrencyMock: vi.fn<FinancialApiClient['setReportingCurrency']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getReportingCurrency: getReportingCurrencyMock,
    setReportingCurrency: setReportingCurrencyMock,
  } as Partial<FinancialApiClient>,
}))

describe('useReportingCurrency', () => {
  beforeEach(() => {
    getReportingCurrencyMock.mockReset()
    setReportingCurrencyMock.mockReset()
    getReportingCurrencyMock.mockResolvedValue({ currency: 'GBP' })
  })

  it('loads the current value on mount', async () => {
    const { result } = renderHook(() => useReportingCurrency())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getReportingCurrencyMock).toHaveBeenCalledOnce()
    expect(result.current.currency).toBe('GBP')
  })

  it('surfaces a load error', async () => {
    getReportingCurrencyMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useReportingCurrency())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('setCurrency calls the PUT endpoint and updates local state on success', async () => {
    setReportingCurrencyMock.mockResolvedValue({ currency: 'BRL' })
    const { result } = renderHook(() => useReportingCurrency())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.setCurrency('BRL')
    })

    expect(setReportingCurrencyMock).toHaveBeenCalledWith({ currency: 'BRL' })
    expect(result.current.currency).toBe('BRL')
    expect(result.current.isSaving).toBe(false)
    expect(result.current.saveError).toBeNull()
  })

  it('a save failure surfaces saveError without discarding the last-known-good value', async () => {
    setReportingCurrencyMock.mockRejectedValue(new Error('Currency not recognized'))
    const { result } = renderHook(() => useReportingCurrency())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.setCurrency('BRL')
    })

    expect(result.current.saveError).toBe('Currency not recognized')
    expect(result.current.currency).toBe('GBP')
    expect(result.current.isSaving).toBe(false)
  })
})
