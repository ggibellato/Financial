import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CreditCardCalendarSyncStatusDto, CreditCardDto } from '../../api/types'
import { useCalendarSyncStatuses } from '../useCalendarSyncStatuses'

const { getCreditCardsMock, getCalendarSyncStatusesMock, resyncCreditCardCalendarMock } = vi.hoisted(() => ({
  getCreditCardsMock: vi.fn<FinancialApiClient['getCreditCards']>(),
  getCalendarSyncStatusesMock: vi.fn<FinancialApiClient['getCalendarSyncStatuses']>(),
  resyncCreditCardCalendarMock: vi.fn<FinancialApiClient['resyncCreditCardCalendar']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCreditCards: getCreditCardsMock,
    getCalendarSyncStatuses: getCalendarSyncStatusesMock,
    resyncCreditCardCalendar: resyncCreditCardCalendarMock,
  } as Partial<FinancialApiClient>,
}))

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-due', name: 'BaAmex', isActive: true, nextInvoiceDueDate: '2026-09-10', latestInvoiceDate: null, hasReferences: false },
  { id: 'card-never-synced', name: 'Nubank', isActive: true, nextInvoiceDueDate: '2026-09-15', latestInvoiceDate: null, hasReferences: false },
  { id: 'card-no-due-date', name: 'PaypalCredit', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
  { id: 'card-inactive', name: 'OldCard', isActive: false, nextInvoiceDueDate: '2026-09-01', latestInvoiceDate: null, hasReferences: false },
]

const STATUSES: CreditCardCalendarSyncStatusDto[] = [
  { creditCardId: 'card-due', state: 'Synced', lastSuccessfulSyncUtc: '2026-09-01T10:00:00Z', lastError: null },
]

describe('useCalendarSyncStatuses', () => {
  beforeEach(() => {
    getCreditCardsMock.mockReset()
    getCalendarSyncStatusesMock.mockReset()
    resyncCreditCardCalendarMock.mockReset()
    getCreditCardsMock.mockResolvedValue(CREDIT_CARDS)
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

  it('excludes inactive cards and cards with no due date', async () => {
    const { result } = renderHook(() => useCalendarSyncStatuses())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    const ids = result.current.rows.map((row) => row.creditCardId)
    expect(ids).not.toContain('card-no-due-date')
    expect(ids).not.toContain('card-inactive')
  })

  it('surfaces a fetch error', async () => {
    getCreditCardsMock.mockRejectedValue(new Error('Network down'))
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

  it('surfaces a resync error without touching the row', async () => {
    resyncCreditCardCalendarMock.mockRejectedValue(new Error('Sync failed'))
    const { result } = renderHook(() => useCalendarSyncStatuses())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.resyncCard('card-due'))

    await waitFor(() => expect(result.current.retryError).toBe('Sync failed'))
    expect(result.current.retryingCardId).toBeNull()
  })
})
