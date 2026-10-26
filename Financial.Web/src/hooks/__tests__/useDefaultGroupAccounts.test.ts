import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountGroupMemberDto, AccountGroupSummaryDto } from '../../api/types'
import { account } from '../../test/account'
import { DEFAULT_GROUP_LOAD_ERROR, useDefaultGroupAccounts } from '../useDefaultGroupAccounts'

const { getDefaultGroupSummaryMock, getAccountsMock } = vi.hoisted(() => ({
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getAccounts: getAccountsMock,
  } as Partial<FinancialApiClient>,
}))

function member(accountId: string, name: string): AccountGroupMemberDto {
  return { accountId, name, balance: 100, balanceIsOwed: false, currency: 'GBP', templateName: 'Bank', roundUpTotal: null, statement: null }
}

const TOTALS = [{ currency: 'GBP', balance: 200, cardOutstandingTotal: 0, roundUpTotal: 0 }]
const SUMMARY = { members: [member('barclays', 'Barclays'), member('old-bank', 'Old bank')], totals: TOTALS } as AccountGroupSummaryDto

describe('useDefaultGroupAccounts', () => {
  beforeEach(() => {
    getDefaultGroupSummaryMock.mockReset()
    getAccountsMock.mockReset()
    getDefaultGroupSummaryMock.mockResolvedValue(SUMMARY)
    getAccountsMock.mockResolvedValue([
      account({ id: 'barclays', name: 'Barclays' }),
      account({ id: 'old-bank', name: 'Old bank', isActive: false }),
    ])
  })

  it('starts loading and marks the members whose account is inactive', async () => {
    const { result } = renderHook(() => useDefaultGroupAccounts(2026, 10))

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 10)
    expect(result.current.data?.members.map((m) => [m.accountId, m.isActive])).toEqual([
      ['barclays', true],
      ['old-bank', false],
    ])
    expect(result.current.data?.totals).toEqual(TOTALS)
  })

  it('reloads the summary when the month changes', async () => {
    const { result, rerender } = renderHook(({ month }) => useDefaultGroupAccounts(2026, month), { initialProps: { month: 10 } })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    rerender({ month: 11 })

    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenLastCalledWith(2026, 11))
  })

  it('reports the load error when the accounts cannot be read', async () => {
    getAccountsMock.mockRejectedValue({ status: 503 })
    const { result } = renderHook(() => useDefaultGroupAccounts(2026, 10))

    await waitFor(() => expect(result.current.error).toBe(DEFAULT_GROUP_LOAD_ERROR))
    expect(result.current.data).toBeNull()
  })
})
