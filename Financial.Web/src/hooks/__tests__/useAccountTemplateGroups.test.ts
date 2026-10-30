import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTemplateGroupDto } from '../../api/types'
import { ACCOUNTS_LOAD_ERROR, useAccountTemplateGroups } from '../useAccountTemplateGroups'

const { getAccountTemplateGroupsMock } = vi.hoisted(() => ({
  getAccountTemplateGroupsMock: vi.fn<FinancialApiClient['getAccountTemplateGroups']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getAccountTemplateGroups: getAccountTemplateGroupsMock } as Partial<FinancialApiClient>,
}))

const GROUPS: AccountTemplateGroupDto[] = [
  {
    templateName: 'Bank',
    members: [
      { accountId: 'barclays', name: 'Barclays', templateName: 'Bank', currency: 'GBP', balance: 100, roundUpTotal: null, statement: null, balanceIsOwed: false, isActive: true, totalOwed: null, nextStatement: null },
    ],
    totals: [{ currency: 'GBP', total: 100 }],
  },
]

describe('useAccountTemplateGroups', () => {
  beforeEach(() => {
    getAccountTemplateGroupsMock.mockReset().mockResolvedValue(GROUPS)
  })

  it('starts loading and returns the groups from one request', async () => {
    const { result } = renderHook(() => useAccountTemplateGroups(2026, 10))

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getAccountTemplateGroupsMock).toHaveBeenCalledTimes(1)
    expect(getAccountTemplateGroupsMock).toHaveBeenCalledWith(2026, 10)
    expect(result.current.data).toEqual(GROUPS)
  })

  it('reports the load error when the groups cannot be read', async () => {
    getAccountTemplateGroupsMock.mockRejectedValue({ status: 503 })
    const { result } = renderHook(() => useAccountTemplateGroups(2026, 10))

    await waitFor(() => expect(result.current.error).toBe(ACCOUNTS_LOAD_ERROR))
    expect(result.current.data).toBeNull()
  })

  it('reloads when the month changes', async () => {
    const { result, rerender } = renderHook(({ month }) => useAccountTemplateGroups(2026, month), { initialProps: { month: 10 } })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    rerender({ month: 11 })

    await waitFor(() => expect(getAccountTemplateGroupsMock).toHaveBeenLastCalledWith(2026, 11))
  })
})
