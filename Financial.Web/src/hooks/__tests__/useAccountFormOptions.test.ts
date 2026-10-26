import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto, CategoryDto, IncomeSourceDto } from '../../api/types'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../useAccountFormOptions'

const { getBanksMock, getCategoriesMock, getIncomeSourcesMock, getTransferDestinationsMock, getTransferSourcesMock } =
  vi.hoisted(() => ({
    getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
    getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
    getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
    getTransferDestinationsMock: vi.fn<FinancialApiClient['getTransferDestinations']>(),
    getTransferSourcesMock: vi.fn<FinancialApiClient['getTransferSources']>(),
  }))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
    getIncomeSources: getIncomeSourcesMock,
    getTransferDestinations: getTransferDestinationsMock,
    getTransferSources: getTransferSourcesMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS = [{ id: 'barclays', name: 'Barclays' }] as BankDto[]
const GROCERIES = { id: 'groceries', name: 'Groceries', active: true } as CategoryDto
const RETIRED = { id: 'retired', name: 'Retired', active: false } as CategoryDto
const SALARY = [{ id: 'salary', name: 'Salary' }] as IncomeSourceDto[]
const CHASE = { id: 'chase', name: 'Chase', currency: 'GBP' }
const WISE = { id: 'wise', name: 'Wise', currency: 'EUR' }

describe('useAccountFormOptions', () => {
  beforeEach(() => {
    for (const mock of [getBanksMock, getCategoriesMock, getIncomeSourcesMock, getTransferDestinationsMock, getTransferSourcesMock]) {
      mock.mockReset()
    }
    getBanksMock.mockResolvedValue(BANKS)
    getCategoriesMock.mockResolvedValue([GROCERIES, RETIRED])
    getIncomeSourcesMock.mockResolvedValue(SALARY)
    getTransferDestinationsMock.mockResolvedValue([CHASE])
    getTransferSourcesMock.mockResolvedValue([WISE])
  })

  it('loads nothing and is not ready while no form needs the options', () => {
    const { result } = renderHook(() => useAccountFormOptions(false, null))

    expect(getBanksMock).not.toHaveBeenCalled()
    expect(getTransferDestinationsMock).not.toHaveBeenCalled()
    expect(result.current.isReady).toBe(false)
    expect(result.current.areCounterpartsReady).toBe(false)
    expect(result.current.options).toEqual({ banks: [], categories: [], incomeSources: [], destinations: [], sources: [] })
  })

  it('loads the shared options and keeps only the active categories', async () => {
    const { result } = renderHook(() => useAccountFormOptions(true, null))

    await waitFor(() => expect(result.current.isReady).toBe(true))

    expect(result.current.options.banks).toEqual(BANKS)
    expect(result.current.options.categories).toEqual([GROCERIES])
    expect(result.current.options.incomeSources).toEqual(SALARY)
    expect(result.current.areCounterpartsReady).toBe(false)
    expect(getTransferDestinationsMock).not.toHaveBeenCalled()
  })

  it('loads the transfer counterparts of the given account', async () => {
    const { result } = renderHook(() => useAccountFormOptions(false, 'barclays'))

    await waitFor(() => expect(result.current.areCounterpartsReady).toBe(true))

    expect(getTransferDestinationsMock).toHaveBeenCalledWith('barclays')
    expect(getTransferSourcesMock).toHaveBeenCalledWith('barclays')
    expect(result.current.options.destinations).toEqual([CHASE])
    expect(result.current.options.sources).toEqual([WISE])
    expect(result.current.isReady).toBe(false)
  })

  it('reports the load error and retries only the failed request', async () => {
    getBanksMock.mockRejectedValueOnce({ status: 503 })
    const { result } = renderHook(() => useAccountFormOptions(true, 'barclays'))

    await waitFor(() => expect(result.current.error).toBe(FORM_OPTIONS_LOAD_ERROR))
    await waitFor(() => expect(result.current.areCounterpartsReady).toBe(true))
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.isReady).toBe(true))
    expect(result.current.error).toBeNull()
    expect(getBanksMock).toHaveBeenCalledTimes(2)
    expect(getTransferDestinationsMock).toHaveBeenCalledTimes(1)
  })

  it('reports a counterparts error and retries only the counterparts', async () => {
    getTransferSourcesMock.mockRejectedValueOnce({ status: 503 })
    const { result } = renderHook(() => useAccountFormOptions(true, 'barclays'))

    await waitFor(() => expect(result.current.error).toBe(FORM_OPTIONS_LOAD_ERROR))
    await waitFor(() => expect(result.current.isReady).toBe(true))
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.areCounterpartsReady).toBe(true))
    expect(result.current.error).toBeNull()
    expect(getTransferSourcesMock).toHaveBeenCalledTimes(2)
    expect(getBanksMock).toHaveBeenCalledTimes(1)
  })
})
