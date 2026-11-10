import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CreditCardCalendarSyncStatusDto } from '../../api/types'
import { account, cardAccount } from '../../test/account'
import { useCalendarSyncStatuses } from '../useCalendarSyncStatuses'

const { getAccountsMock, getCalendarSyncStatusesMock, resyncCreditCardCalendarMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getCalendarSyncStatusesMock: vi.fn<FinancialApiClient['getCalendarSyncStatuses']>(),
  resyncCreditCardCalendarMock: vi.fn<FinancialApiClient['resyncCreditCardCalendar']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getCalendarSyncStatuses: getCalendarSyncStatusesMock,
    resyncCreditCardCalendar: resyncCreditCardCalendarMock,
  } as Partial<FinancialApiClient>,
}))

const ACCOUNTS = [
  cardAccount({ id: 'card-due', name: 'BaAmex', dueDate: '2026-09-10' }),
  cardAccount({ id: 'card-never-synced', name: 'Nubank', dueDate: '2026-09-15' }),
  cardAccount({ id: 'card-no-due-date', name: 'PaypalCredit', dueDate: null }),
  cardAccount({ id: 'card-inactive', name: 'OldCard', isActive: false, dueDate: '2026-09-01' }),
  account({ id: 'bank-with-date', name: 'Barclays', dueDate: '2026-09-20' }),
]

const STATUSES: CreditCardCalendarSyncStatusDto[] = [
  { creditCardId: 'card-due', state: 'Synced', lastSuccessfulSyncUtc: '2026-09-01T10:00:00Z', lastError: null },
]

describe('useCalendarSyncStatuses', () => {
  beforeEach(() => {
    getAccountsMock.mockReset()
    getCalendarSyncStatusesMock.mockReset()
    resyncCreditCardCalendarMock.mockReset()
    getAccountsMock.mockResolvedValue(ACCOUNTS)
    getCalendarSyncStatusesMock.mockResolvedValue(STATUSES)
  })

  it('joins active, due-dated cards with their sync status', async () => {
    const { result } = renderHook(() => useCalendarSyncStatuses())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.rows).toEqual([
      { creditCardId: 'card-due', name: 'BaAmex', dueDate: '2026-09-10', state: 'Synced', lastSuccessfulSyncUtc: '2026-09-01T10:00:00Z', lastError: null },
      { creditCardId: 'card-never-synced', name: 'Nubank', dueDate: '2026-09-15', state: 'Pending', lastSuccessfulSyncUtc: null, lastError: null },
    ])
  })

  it('excludes inactive cards, cards with no due date and accounts without statements', async () => {
    const { result } = renderHook(() => useCalendarSyncStatuses())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const ids = result.current.rows.map((row) => row.creditCardId)
    expect(ids).not.toContain('card-no-due-date')
    expect(ids).not.toContain('card-inactive')
    expect(ids).not.toContain('bank-with-date')
  })

  it('surfaces a fetch error', async () => {
    getAccountsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useCalendarSyncStatuses())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('resyncCard calls the API and updates only that row', async () => {
    resyncCreditCardCalendarMock.mockResolvedValue({
      creditCardId: 'card-never-synced',
      state: 'Synced',
      lastSuccessfulSyncUtc: '2026-09-02T08:00:00Z',
      lastError: null,
    })
    const { result } = renderHook(() => useCalendarSyncStatuses())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.resyncCard('card-never-synced'))

    await waitFor(() => expect(result.current.retryingCardId).toBeNull())
    const updated = result.current.rows.find((row) => row.creditCardId === 'card-never-synced')
    expect(updated?.state).toBe('Synced')
    expect(updated?.lastSuccessfulSyncUtc).toBe('2026-09-02T08:00:00Z')
    const untouched = result.current.rows.find((row) => row.creditCardId === 'card-due')
    expect(untouched?.state).toBe('Synced')
  })

  it('surfaces a resync error for the card it belongs to without touching the row', async () => {
    resyncCreditCardCalendarMock.mockRejectedValue(new Error('Sync failed'))
    const { result } = renderHook(() => useCalendarSyncStatuses())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.resyncCard('card-due'))

    await waitFor(() => expect(result.current.retryFailure).toEqual({ creditCardId: 'card-due', message: 'Sync failed' }))
    expect(result.current.retryingCardId).toBeNull()
  })

  it('clears the previous resync error when the next attempt starts', async () => {
    resyncCreditCardCalendarMock.mockRejectedValueOnce(new Error('Sync failed')).mockReturnValue(new Promise(() => {}))
    const { result } = renderHook(() => useCalendarSyncStatuses())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    act(() => result.current.resyncCard('card-due'))
    await waitFor(() => expect(result.current.retryFailure).not.toBeNull())

    act(() => result.current.resyncCard('card-due'))

    expect(result.current.retryFailure).toBeNull()
    expect(result.current.retryingCardId).toBe('card-due')
  })
})
