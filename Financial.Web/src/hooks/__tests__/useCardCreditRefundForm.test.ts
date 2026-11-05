import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTransferDto } from '../../api/types'
import { account, cardAccount, potsAccount } from '../../test/account'
import { pinDate } from '../../test-utils/pinDate'
import { refundableBanksOf, useCardCreditRefundForm } from '../useCardCreditRefundForm'

const { getAccountsMock, createCardCreditRefundMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  createCardCreditRefundMock: vi.fn<FinancialApiClient['createCardCreditRefund']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    createCardCreditRefund: createCardCreditRefundMock,
  } as Partial<FinancialApiClient>,
}))

const AMEX = { id: 'amex', name: 'BA Amex', currency: 'GBP' }
const BARCLAYS = account({ id: 'barclays', name: 'Barclays' })
const CHASE = account({ id: 'chase', name: 'Chase' })
const ACCOUNTS = [
  BARCLAYS,
  CHASE,
  account({ id: 'closed', name: 'Closed', isActive: false }),
  account({ id: 'itau', name: 'Itau', currency: 'BRL' }),
  cardAccount({ id: 'amex', name: 'BA Amex' }),
  potsAccount({ id: 'reserva', name: 'Reserva' }),
]

const SAVED: AccountTransferDto = {
  id: 't7',
  date: '2026-11-05',
  sourceAccountId: 'amex',
  sourceAccountName: 'BA Amex',
  sourceCurrency: 'GBP',
  destinationAccountId: 'barclays',
  destinationAccountName: 'Barclays',
  destinationCurrency: 'GBP',
  amount: 35,
  destinationAmount: 35,
  note: null,
}

function renderForm(onRefunded = vi.fn(), credit = 35) {
  return renderHook(() => useCardCreditRefundForm(AMEX, credit, onRefunded))
}

async function openLoaded(result: { current: ReturnType<typeof useCardCreditRefundForm> }) {
  act(() => result.current.open())
  await waitFor(() => expect(result.current.banks).toHaveLength(2))
}

describe('refundableBanksOf', () => {
  it('keeps active accounts that receive transfers in the card currency', () => {
    expect(refundableBanksOf(ACCOUNTS, 'GBP').map((bank) => bank.id)).toEqual(['barclays', 'chase'])
  })
})

describe('useCardCreditRefundForm', () => {
  beforeEach(() => {
    pinDate('2026-11-05T12:00:00Z')
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    createCardCreditRefundMock.mockReset().mockResolvedValue(SAVED)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads nothing until opened', () => {
    renderForm()

    expect(getAccountsMock).not.toHaveBeenCalled()
  })

  it('opens with the amount at the credit, today, and no bank chosen', async () => {
    const { result } = renderForm()

    await openLoaded(result)

    expect(result.current.values).toEqual({ destinationAccountId: '', amount: '35.00', date: '2026-11-05' })
    expect(result.current.isDirty).toBe(false)
  })

  it('[AC P60-F15-card-refunds-and-credit-05] posts one credit refund from the card to the chosen bank', async () => {
    const onRefunded = vi.fn()
    const { result } = renderForm(onRefunded)
    await openLoaded(result)

    act(() => result.current.setField('destinationAccountId', 'barclays'))
    act(() => result.current.setField('amount', '20.50'))
    act(() => {
      expect(result.current.submit()).toBeNull()
    })
    expect(result.current.isSaving).toBe(true)

    await waitFor(() => expect(onRefunded).toHaveBeenCalledTimes(1))
    expect(createCardCreditRefundMock).toHaveBeenCalledWith({
      date: '2026-11-05',
      sourceAccountId: 'amex',
      destinationAccountId: 'barclays',
      amount: 20.5,
      destinationAmount: null,
      note: null,
    })
    expect(result.current.isOpen).toBe(false)
    await waitFor(() => expect(result.current.isSaving).toBe(false))
  })

  it('[AC P60-F15-card-refunds-and-credit-05] refuses an amount above the credit, naming the credit, and accepts exactly the credit', async () => {
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('destinationAccountId', 'barclays'))

    act(() => result.current.setField('amount', '35.01'))
    act(() => {
      expect(result.current.submit()).toBe('amount')
    })
    expect(result.current.fieldErrors.amount).toBe("The card's credit is 35.00 GBP.")
    expect(createCardCreditRefundMock).not.toHaveBeenCalled()

    act(() => result.current.setField('amount', '35'))
    act(() => {
      expect(result.current.submit()).toBeNull()
    })
    await waitFor(() => expect(createCardCreditRefundMock).toHaveBeenCalledTimes(1))
  })

  it('returns the first invalid field and each message without calling the API', async () => {
    const { result } = renderForm()
    await openLoaded(result)

    act(() => result.current.setField('amount', '0'))
    act(() => result.current.setField('date', '2026-11-06'))
    act(() => {
      expect(result.current.submit()).toBe('destinationAccountId')
    })

    expect(result.current.fieldErrors).toEqual({
      destinationAccountId: 'Select a bank.',
      amount: 'Amount must be greater than zero.',
      date: 'Date must not be in the future.',
    })
    act(() => result.current.setField('date', ''))
    expect(result.current.fieldErrors.date).toBeUndefined()
    act(() => {
      result.current.submit()
    })
    expect(result.current.fieldErrors.date).toBe('Enter a date.')
    expect(createCardCreditRefundMock).not.toHaveBeenCalled()
  })

  it('keeps the values with the server message, reloads the list, and drops a bank that is gone', async () => {
    createCardCreditRefundMock.mockRejectedValue(new ApiError('On that date the card had less credit than this refund; choose a later date.', 400))
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('destinationAccountId', 'chase'))
    act(() => result.current.setField('date', '2026-11-01'))
    getAccountsMock.mockResolvedValue([BARCLAYS])

    act(() => {
      result.current.submit()
    })

    await waitFor(() => expect(result.current.saveError).toBe('On that date the card had less credit than this refund; choose a later date.'))
    await waitFor(() => expect(result.current.banks).toHaveLength(1))
    expect(result.current.isSaving).toBe(false)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.values).toEqual({ destinationAccountId: '', amount: '35.00', date: '2026-11-01' })
  })

  it('reports a load error with retry, then no banks on an empty list', async () => {
    getAccountsMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([])
    const { result } = renderForm()

    act(() => result.current.open())
    await waitFor(() => expect(result.current.loadError).toBe('offline'))
    expect(result.current.hasNoBanks).toBe(false)
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.hasNoBanks).toBe(true))
    expect(result.current.loadError).toBeNull()
  })

  it.each([
    ['destinationAccountId', 'barclays'],
    ['amount', '10'],
    ['date', '2026-11-04'],
  ] as const)('is dirty once %s changes, and clean again after cancel', async (field, value) => {
    const { result } = renderForm()
    await openLoaded(result)

    act(() => result.current.setField(field, value))
    expect(result.current.isDirty).toBe(true)
    act(() => result.current.cancel())

    expect(result.current.isDirty).toBe(false)
    expect(result.current.isOpen).toBe(false)
  })

  it('reopens with the credit as it is then', async () => {
    const onRefunded = vi.fn()
    const { result, rerender } = renderHook(({ credit }) => useCardCreditRefundForm(AMEX, credit, onRefunded), { initialProps: { credit: 35 } })
    await openLoaded(result)
    act(() => result.current.cancel())

    rerender({ credit: 12.5 })
    await openLoaded(result)

    expect(result.current.values.amount).toBe('12.50')
  })

  it('does nothing on submit with no card', () => {
    const { result } = renderHook(() => useCardCreditRefundForm(undefined, 0, vi.fn()))

    act(() => {
      expect(result.current.submit()).toBeNull()
    })

    expect(createCardCreditRefundMock).not.toHaveBeenCalled()
  })
})
