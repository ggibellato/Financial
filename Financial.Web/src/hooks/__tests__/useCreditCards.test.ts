import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CreditCardDto } from '../../api/types'
import { useCreditCards } from '../useCreditCards'

const { getCreditCardsMock, createCreditCardMock, updateCreditCardMock, deleteCreditCardMock } = vi.hoisted(() => ({
  getCreditCardsMock: vi.fn<FinancialApiClient['getCreditCards']>(),
  createCreditCardMock: vi.fn<FinancialApiClient['createCreditCard']>(),
  updateCreditCardMock: vi.fn<FinancialApiClient['updateCreditCard']>(),
  deleteCreditCardMock: vi.fn<FinancialApiClient['deleteCreditCard']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCreditCards: getCreditCardsMock,
    createCreditCard: createCreditCardMock,
    updateCreditCard: updateCreditCardMock,
    deleteCreditCard: deleteCreditCardMock,
  } as Partial<FinancialApiClient>,
}))

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
  { id: 'card-paypal', name: 'PaypalCredit', isActive: false, nextInvoiceDueDate: '2026-09-05', latestInvoiceDate: null, hasReferences: true },
]

describe('useCreditCards', () => {
  beforeEach(() => {
    getCreditCardsMock.mockReset()
    createCreditCardMock.mockReset()
    updateCreditCardMock.mockReset()
    deleteCreditCardMock.mockReset()
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

  it('createCreditCard calls the API and re-fetches the list', async () => {
    createCreditCardMock.mockResolvedValue({ id: 'card-nubank', name: 'Nubank', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false })
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.createCreditCard({ name: 'Nubank', isActive: true })
    })

    expect(createCreditCardMock).toHaveBeenCalledWith({ name: 'Nubank', isActive: true })
    await waitFor(() => expect(getCreditCardsMock).toHaveBeenCalledTimes(2))
  })

  it('createCreditCard propagates a rejected promise to the caller without swallowing it', async () => {
    createCreditCardMock.mockRejectedValue(new Error('A credit card named "BaAmex" already exists.'))
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await expect(result.current.createCreditCard({ name: 'BaAmex', isActive: true })).rejects.toThrow(
      'A credit card named "BaAmex" already exists.',
    )
  })

  it('updates a card and re-fetches, leaving other cards untouched', async () => {
    updateCreditCardMock.mockResolvedValue({ ...CREDIT_CARDS[0], isActive: false, nextInvoiceDueDate: '2026-09-05' })
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await result.current.updateCreditCard('card-baamex', { name: 'BaAmex', nextInvoiceDueDate: '2026-09-05', isActive: false })
    })

    expect(updateCreditCardMock).toHaveBeenCalledWith('card-baamex', {
      name: 'BaAmex',
      nextInvoiceDueDate: '2026-09-05',
      isActive: false,
    })
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

    let updatePromise!: Promise<unknown>
    act(() => {
      updatePromise = result.current.updateCreditCard('card-baamex', { name: 'BaAmex', nextInvoiceDueDate: null, isActive: false })
    })

    await waitFor(() => expect(result.current.updatingCardId).toBe('card-baamex'))

    act(() => resolveUpdate({ ...CREDIT_CARDS[0], isActive: false }))
    await act(async () => {
      await updatePromise
    })

    expect(result.current.updatingCardId).toBeNull()
  })

  it('surfaces an update error and rethrows it to the caller', async () => {
    updateCreditCardMock.mockRejectedValue(new Error('Credit card was not found.'))
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await expect(
        result.current.updateCreditCard('unknown-id', { name: 'X', nextInvoiceDueDate: null, isActive: true }),
      ).rejects.toThrow('Credit card was not found.')
    })

    expect(result.current.updateError).toBe('Credit card was not found.')
    expect(result.current.updatingCardId).toBeNull()
  })

  it('deleteCreditCard calls the API and re-fetches the list', async () => {
    deleteCreditCardMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteCreditCard('card-baamex'))

    await waitFor(() => expect(result.current.deletingId).toBeNull())
    expect(deleteCreditCardMock).toHaveBeenCalledWith('card-baamex')
    await waitFor(() => expect(getCreditCardsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without re-fetching', async () => {
    deleteCreditCardMock.mockRejectedValue(new Error('Cannot delete a credit card that is still referenced by a statement or expense.'))
    const { result } = renderHook(() => useCreditCards())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteCreditCard('card-paypal'))

    await waitFor(() =>
      expect(result.current.deleteError).toBe('Cannot delete a credit card that is still referenced by a statement or expense.'),
    )
    expect(getCreditCardsMock).toHaveBeenCalledTimes(1)
  })
})
