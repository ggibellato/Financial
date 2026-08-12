import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { CreditCardDto } from '../api/types'
import { useCreditCards } from './useCreditCards'

const getCreditCardsMock = vi.fn<FinancialApiClient['getCreditCards']>()
const updateCreditCardMock = vi.fn<FinancialApiClient['updateCreditCard']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCreditCards: getCreditCardsMock,
    updateCreditCard: updateCreditCardMock,
  }),
}))

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null },
  { id: 'card-paypal', name: 'PaypalCredit', isActive: false, nextInvoiceDueDate: '2026-09-05' },
]

describe('useCreditCards', () => {
  beforeEach(() => {
    getCreditCardsMock.mockReset()
    updateCreditCardMock.mockReset()
    getCreditCardsMock.mockResolvedValue(CREDIT_CARDS)
  })

  it('fetches the credit card list on mount', async () => {
    const { result } = renderHook(() => useCreditCards())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCreditCardsMock).toHaveBeenCalledOnce()
    expect(result.current.creditCards).toEqual(CREDIT_CARDS)
  })

  it('surfaces a fetch error', async () => {
    getCreditCardsMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useCreditCards())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.error).toBe('Network down')
  })

  it('updates a card and re-fetches, leaving other cards untouched', async () => {
    updateCreditCardMock.mockResolvedValue({ ...CREDIT_CARDS[0], isActive: false, nextInvoiceDueDate: '2026-09-05' })
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateCreditCard('card-baamex', { nextInvoiceDueDate: '2026-09-05', isActive: false }))

    await waitFor(() =>
      expect(updateCreditCardMock).toHaveBeenCalledWith('card-baamex', {
        nextInvoiceDueDate: '2026-09-05',
        isActive: false,
      }),
    )
    await waitFor(() => expect(getCreditCardsMock).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(result.current.updatingCardId).toBeNull())
  })

  it('tracks which card is updating while the request is in flight', async () => {
    let resolveUpdate: (value: CreditCardDto) => void = () => {}
    updateCreditCardMock.mockReturnValue(
      new Promise((resolve) => {
        resolveUpdate = resolve
      }),
    )
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateCreditCard('card-baamex', { nextInvoiceDueDate: null, isActive: false }))

    await waitFor(() => expect(result.current.updatingCardId).toBe('card-baamex'))

    act(() => resolveUpdate({ ...CREDIT_CARDS[0], isActive: false }))

    await waitFor(() => expect(result.current.updatingCardId).toBeNull())
  })

  it('surfaces an update error without crashing', async () => {
    updateCreditCardMock.mockRejectedValue(new Error('Credit card was not found.'))
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.updateCreditCard('unknown-id', { nextInvoiceDueDate: null, isActive: true }))

    await waitFor(() => expect(result.current.updateError).toBe('Credit card was not found.'))
    expect(result.current.updatingCardId).toBeNull()
  })
})
