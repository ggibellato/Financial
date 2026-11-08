import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryDto, IncomeSourceDto } from '../../api/types'
import { account, cardAccount, potsAccount } from '../../test/account'
import { FORM_OPTIONS_LOAD_ERROR, useAccountFormOptions } from '../useAccountFormOptions'

const { getAccountsMock, getCategoriesMock, getIncomeSourcesMock, getTransferDestinationsMock, getTransferSourcesMock } =
  vi.hoisted(() => ({
    getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
    getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
    getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
    getTransferDestinationsMock: vi.fn<FinancialApiClient['getTransferDestinations']>(),
    getTransferSourcesMock: vi.fn<FinancialApiClient['getTransferSources']>(),
  }))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getCategories: getCategoriesMock,
    getIncomeSources: getIncomeSourcesMock,
    getTransferDestinations: getTransferDestinationsMock,
    getTransferSources: getTransferSourcesMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS = account({ id: 'barclays', name: 'Barclays' })
const CLOSED = account({ id: 'closed', name: 'Closed', isActive: false })
const ACCOUNTS = [BARCLAYS, cardAccount({ id: 'amex', name: 'Amex' }), CLOSED, potsAccount({ id: 'reserva', name: 'Reserva' })]
const GROCERIES = { id: 'groceries', name: 'Groceries', active: true } as CategoryDto
const RETIRED = { id: 'retired', name: 'Retired', active: false } as CategoryDto
const SALARY = [{ id: 'salary', name: 'Salary' }] as IncomeSourceDto[]
const CHASE = { id: 'chase', name: 'Chase', currency: 'GBP' }
const WISE = { id: 'wise', name: 'Wise', currency: 'EUR' }

describe('useAccountFormOptions', () => {
  beforeEach(() => {
    for (const mock of [getAccountsMock, getCategoriesMock, getIncomeSourcesMock, getTransferDestinationsMock, getTransferSourcesMock]) {
      mock.mockReset()
    }
    getAccountsMock.mockResolvedValue(ACCOUNTS)
    getCategoriesMock.mockResolvedValue([GROCERIES, RETIRED])
    getIncomeSourcesMock.mockResolvedValue(SALARY)
    getTransferDestinationsMock.mockResolvedValue([CHASE])
    getTransferSourcesMock.mockResolvedValue([WISE])
  })

  it('loads nothing and is not ready while no form needs the options', () => {
    const { result } = renderHook(() => useAccountFormOptions(false, null))

    expect(getAccountsMock).not.toHaveBeenCalled()
    expect(getTransferDestinationsMock).not.toHaveBeenCalled()
    expect(result.current.isReady).toBe(false)
    expect(result.current.areCounterpartsReady).toBe(false)
    expect(result.current.options).toEqual({ banks: [], categories: [], incomeSources: [], destinations: [], sources: [] })
  })

  it('loads the shared options, with every account that can pay bills (inactive included) and only the active categories', async () => {
    const { result } = renderHook(() => useAccountFormOptions(true, null))

    await waitFor(() => expect(result.current.isReady).toBe(true))

    expect(result.current.options.banks).toEqual([BARCLAYS, CLOSED])
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
    getAccountsMock.mockRejectedValueOnce({ status: 503 })
    const { result } = renderHook(() => useAccountFormOptions(true, 'barclays'))

    await waitFor(() => expect(result.current.error).toBe(FORM_OPTIONS_LOAD_ERROR))
    await waitFor(() => expect(result.current.areCounterpartsReady).toBe(true))
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.isReady).toBe(true))
    expect(result.current.error).toBeNull()
    expect(getAccountsMock).toHaveBeenCalledTimes(2)
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
    expect(getAccountsMock).toHaveBeenCalledTimes(1)
  })
})
