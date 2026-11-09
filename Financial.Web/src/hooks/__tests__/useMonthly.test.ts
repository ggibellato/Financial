import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountGroupMemberDto, AccountGroupSummaryDto, CategoryTotalDto, TitheSummaryDto } from '../../api/types'
import { useMonthly } from '../useMonthly'

const {
  getCategoryTotalsByMonthMock,
  getDefaultGroupSummaryMock,
  getTitheSummaryByMonthMock,
  updateTitheCarryForwardMock,
} = vi.hoisted(() => ({
  getCategoryTotalsByMonthMock: vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>(),
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    updateTitheCarryForward: updateTitheCarryForwardMock,
  } as Partial<FinancialApiClient>,
}))

function bank(accountId: string, name: string, balance: number, roundUpTotal: number | null): AccountGroupMemberDto {
  return { accountId, name, templateName: 'Bank', currency: 'GBP', balance, currencyBalances: [{ currency: 'GBP', balance }], otherParticipant: null, roundUpTotal, statement: null, balanceIsOwed: false, isActive: true, totalOwed: null, nextStatement: null }
}

const BAAMEX: AccountGroupMemberDto = { accountId: 'card-baamex', name: 'BaAmex', templateName: 'CreditCard', currency: 'GBP', balance: -812.33, currencyBalances: [{ currency: 'GBP', balance: -812.33 }], otherParticipant: null, roundUpTotal: null, statement: { outstandingTotal: 64.1, isPaid: false }, balanceIsOwed: true, isActive: true, totalOwed: 812.33, nextStatement: null }

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const SUMMARY: AccountGroupSummaryDto = {
  year: 2026,
  month: 7,
  members: [bank('bank-barclays', 'Barclays', 1875.32, 0), bank('bank-chase', 'Chase', -50, 0.1), BAAMEX],
  totals: [{ currency: 'GBP', balance: 1825.32, roundUpTotal: 0.1, cardOutstandingTotal: 64.1 }],
  categoryTotalsSum: 1234.56,
  incomeTotals: [{ incomeSourceId: '2', name: 'Ariana', netValue: 720, grossValue: 820 }],
  totalIncoming: 720,
}

const TITHE_SUMMARY: TitheSummaryDto = { calculatedTithe: 245, titheBalance: 245, carryForward: null }

async function renderLoaded() {
  const rendered = renderHook(() => useMonthly())
  await waitFor(() => expect(rendered.result.current.isLoading).toBe(false))
  return rendered
}

describe('useMonthly', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date('2026-07-15T12:00:00Z'))
    getCategoryTotalsByMonthMock.mockReset().mockResolvedValue(CATEGORY_TOTALS)
    getDefaultGroupSummaryMock.mockReset().mockResolvedValue(SUMMARY)
    getTitheSummaryByMonthMock.mockReset().mockResolvedValue(TITHE_SUMMARY)
    updateTitheCarryForwardMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-02] makes only the three overview reads for the viewed month', async () => {
    const { result } = renderHook(() => useMonthly())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 7)
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledExactlyOnceWith(2026, 7)
    expect(getTitheSummaryByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 7)
    expect(result.current.monthInputValue).toBe('2026-07')
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.titheSummary).toEqual(TITHE_SUMMARY)
  })

  it('exposes the load error and reloads on retry', async () => {
    getDefaultGroupSummaryMock.mockRejectedValueOnce(new Error('Network down'))
    const { result } = await renderLoaded()
    expect(result.current.error).toBe('Network down')

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.error).toBeNull())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2)
    expect(result.current.bankTotals).toHaveLength(2)
  })

  it('[AC P60-F07-credit-card-template-and-card-migration-09] reads the card total from the summary and makes no statement sum', async () => {
    const { result } = await renderLoaded()

    expect(result.current.currencyTotals).toEqual(SUMMARY.totals)
    expect(result.current).not.toHaveProperty('adjustmentTotal')
  })

  it('splits the members into bank rows and card rows', async () => {
    const { result } = await renderLoaded()

    expect(result.current.bankTotals.map((row) => row.bank)).toEqual(['Barclays', 'Chase'])
    expect(result.current.cardMembers).toEqual([BAAMEX])
  })

  it('[AC P60-F04-default-account-group-and-summary-query-06] reads the summary figures from one summary request without summing in the browser', async () => {
    const { result } = await renderLoaded()

    expect(getDefaultGroupSummaryMock).toHaveBeenCalledOnce()
    expect(result.current.categoryTotalsSum).toBe(1234.56)
    expect(result.current.bankTotals).toEqual([
      { bankId: 'bank-barclays', bank: 'Barclays', currency: 'GBP', balance: 1875.32, roundUpTotal: 0 },
      { bankId: 'bank-chase', bank: 'Chase', currency: 'GBP', balance: -50, roundUpTotal: 0.1 },
    ])
    expect(result.current.incomeTotals).toEqual([{ incomeSourceId: '2', name: 'Ariana', netValue: 720, grossValue: 820 }])
    expect(result.current.totalIncoming).toBe(720)
  })

  it('keys bank rows by the member account Id, not by matching a bank name', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({ ...SUMMARY, members: [bank('bank-new', 'Barclays', 10, 0)] })
    const { result } = await renderLoaded()

    expect(result.current.bankTotals).toEqual([{ bankId: 'bank-new', bank: 'Barclays', currency: 'GBP', balance: 10, roundUpTotal: 0 }])
  })

  it('shows a member without a round-up total as 0', async () => {
    getDefaultGroupSummaryMock.mockResolvedValue({ ...SUMMARY, members: [bank('bank-barclays', 'Barclays', 10, null)] })
    const { result } = await renderLoaded()

    expect(result.current.bankTotals[0].roundUpTotal).toBe(0)
  })

  it('exposes empty summary figures before the summary has loaded', () => {
    getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))
    const { result } = renderHook(() => useMonthly())

    expect(result.current.bankTotals).toEqual([])
    expect(result.current.cardMembers).toEqual([])
    expect(result.current.currencyTotals).toEqual([])
    expect(result.current.incomeTotals).toEqual([])
    expect(result.current.categoryTotalsSum).toBe(0)
    expect(result.current.totalIncoming).toBe(0)
  })

  it('re-fetches for a new month when the month input changes', async () => {
    const { result } = await renderLoaded()

    act(() => result.current.setMonthInputValue('2026-08'))

    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 8))
    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledWith(2026, 8)
    expect(getTitheSummaryByMonthMock).toHaveBeenCalledWith(2026, 8)
  })

  it('toggles carry-forward inclusion and re-fetches the tithe summary without a reload', async () => {
    updateTitheCarryForwardMock.mockResolvedValue(TITHE_SUMMARY)
    const { result } = await renderLoaded()

    act(() => result.current.updateCarryForwardInclusion(false))

    expect(result.current.isLoading).toBe(false)
    await waitFor(() => expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(2026, 7, { included: false }))
    await waitFor(() => expect(getTitheSummaryByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('sets carryForwardUpdating while the toggle request is in flight, then clears it', async () => {
    let resolveUpdate!: (value: TitheSummaryDto) => void
    updateTitheCarryForwardMock.mockReturnValue(new Promise((resolve) => { resolveUpdate = resolve }))
    const { result } = await renderLoaded()

    act(() => result.current.updateCarryForwardInclusion(false))
    expect(result.current.carryForwardUpdating).toBe(true)

    await act(async () => resolveUpdate(TITHE_SUMMARY))

    await waitFor(() => expect(result.current.carryForwardUpdating).toBe(false))
  })

  it('exposes a failed carry-forward toggle as an action error, clears the busy flag and clears the error on the next attempt', async () => {
    updateTitheCarryForwardMock.mockRejectedValueOnce(new Error('No carry-forward is available for this month.'))
    updateTitheCarryForwardMock.mockReturnValueOnce(new Promise(() => {}))
    const { result } = await renderLoaded()

    act(() => result.current.updateCarryForwardInclusion(false))

    await waitFor(() => expect(result.current.carryForwardError).toBe('No carry-forward is available for this month.'))
    expect(result.current.carryForwardUpdating).toBe(false)

    act(() => result.current.updateCarryForwardInclusion(false))

    expect(result.current.carryForwardError).toBeNull()
  })
})
