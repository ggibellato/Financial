import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTransferDto, CardCreditDto } from '../../api/types'
import { account, cardAccount, potsAccount } from '../../test/account'
import { pinDate } from '../../test-utils/pinDate'
import { useCardCreditRefundForm } from '../useCardCreditRefundForm'

const { getAccountsMock, getCardsInCreditMock, createCardCreditRefundMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getCardsInCreditMock: vi.fn<FinancialApiClient['getCardsInCredit']>(),
  createCardCreditRefundMock: vi.fn<FinancialApiClient['createCardCreditRefund']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getCardsInCredit: getCardsInCreditMock,
    createCardCreditRefund: createCardCreditRefundMock,
  } as Partial<FinancialApiClient>,
}))

const AMEX = { id: 'amex', name: 'BA Amex', currency: 'GBP' }
const BARCLAYS = account({ id: 'barclays', name: 'Barclays' })
const CHASE = account({ id: 'chase', name: 'Chase' })
const ACCOUNTS = [
  CHASE,
  BARCLAYS,
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
  return renderHook(() => useCardCreditRefundForm('card', AMEX, credit, onRefunded))
}

async function openLoaded(result: { current: ReturnType<typeof useCardCreditRefundForm> }) {
  act(() => result.current.open())
  await waitFor(() => expect(result.current.counterparts).toHaveLength(2))
}

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

  it('lists active accounts that receive transfers in the card currency, by name', async () => {
    const { result } = renderForm()

    await openLoaded(result)

    expect(result.current.counterparts.map((bank) => bank.id)).toEqual(['barclays', 'chase'])
  })

  it('opens with the amount at the credit, today, and no bank chosen', async () => {
    const { result } = renderForm()

    await openLoaded(result)

    expect(result.current.values).toEqual({ counterpartId: '', amount: '35.00', date: '2026-11-05' })
    expect(result.current.isDirty).toBe(false)
  })

  it('[AC P60-F15-card-refunds-and-credit-05] posts one credit refund from the card to the chosen bank', async () => {
    const onRefunded = vi.fn()
    const { result } = renderForm(onRefunded)
    await openLoaded(result)

    act(() => result.current.setField('counterpartId', 'barclays'))
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
    act(() => result.current.setField('counterpartId', 'barclays'))

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
      expect(result.current.submit()).toBe('counterpartId')
    })

    expect(result.current.fieldErrors).toEqual({
      counterpartId: 'Select a bank.',
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
    act(() => result.current.setField('counterpartId', 'chase'))
    act(() => result.current.setField('date', '2026-11-01'))
    getAccountsMock.mockResolvedValue([BARCLAYS])

    act(() => {
      result.current.submit()
    })

    await waitFor(() => expect(result.current.saveError).toBe('On that date the card had less credit than this refund; choose a later date.'))
    await waitFor(() => expect(result.current.counterparts).toHaveLength(1))
    expect(result.current.isSaving).toBe(false)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.values).toEqual({ counterpartId: '', amount: '35.00', date: '2026-11-01' })
  })

  it('reports a load error with retry, then no banks on an empty list', async () => {
    getAccountsMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([])
    const { result } = renderForm()

    act(() => result.current.open())
    await waitFor(() => expect(result.current.loadError).toBe('offline'))
    expect(result.current.hasNoCounterparts).toBe(false)
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.hasNoCounterparts).toBe(true))
    expect(result.current.loadError).toBeNull()
  })

  it.each([
    ['counterpartId', 'barclays'],
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
    const { result, rerender } = renderHook(({ credit }) => useCardCreditRefundForm('card', AMEX, credit, onRefunded), { initialProps: { credit: 35 } })
    await openLoaded(result)
    act(() => result.current.cancel())

    rerender({ credit: 12.5 })
    await openLoaded(result)

    expect(result.current.values.amount).toBe('12.50')
  })

  it('does nothing on submit with no card', () => {
    const { result } = renderHook(() => useCardCreditRefundForm('card', undefined, 0, vi.fn()))

    act(() => {
      expect(result.current.submit()).toBeNull()
    })

    expect(createCardCreditRefundMock).not.toHaveBeenCalled()
  })
})

const BARCLAYS_BANK = { id: 'barclays', name: 'Barclays', currency: 'GBP' }
const CARDS_IN_CREDIT: CardCreditDto[] = [
  { accountId: 'amex', name: 'BA Amex', currency: 'GBP', isActive: true, credit: 35 },
  { accountId: 'itau-card', name: 'Itau Visa', currency: 'BRL', isActive: true, credit: 80 },
  { accountId: 'visa', name: 'Platinum Visa', currency: 'GBP', isActive: false, credit: 12.5 },
]

function renderBankForm(onRefunded = vi.fn()) {
  return renderHook(() => useCardCreditRefundForm('bank', BARCLAYS_BANK, 0, onRefunded))
}

async function openBankLoaded(result: { current: ReturnType<typeof useCardCreditRefundForm> }) {
  act(() => result.current.open())
  await waitFor(() => expect(result.current.counterparts).toHaveLength(2))
}

describe('useCardCreditRefundForm in bank mode', () => {
  beforeEach(() => {
    pinDate('2026-11-05T12:00:00Z')
    getAccountsMock.mockReset()
    getCardsInCreditMock.mockReset().mockResolvedValue(CARDS_IN_CREDIT)
    createCardCreditRefundMock.mockReset().mockResolvedValue(SAVED)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('[AC P60-F15-card-refunds-and-credit-05] lists the cards in credit in the bank currency, inactive ones marked, with no amount until a card is chosen', async () => {
    const { result } = renderBankForm()

    await openBankLoaded(result)

    expect(getAccountsMock).not.toHaveBeenCalled()
    expect(result.current.counterparts).toEqual([
      { id: 'amex', label: 'BA Amex · Credit 35.00', currency: 'GBP', credit: 35 },
      { id: 'visa', label: 'Platinum Visa (inactive) · Credit 12.50', currency: 'GBP', credit: 12.5 },
    ])
    expect(result.current.values).toEqual({ counterpartId: '', amount: '', date: '2026-11-05' })
    expect(result.current.isDirty).toBe(false)
  })

  it('defaults the amount to the chosen card credit and caps it there', async () => {
    const { result } = renderBankForm()
    await openBankLoaded(result)

    act(() => result.current.setField('counterpartId', 'visa'))
    expect(result.current.values.amount).toBe('12.50')
    act(() => result.current.setField('counterpartId', 'amex'))
    expect(result.current.values.amount).toBe('35.00')

    act(() => result.current.setField('amount', '35.01'))
    act(() => {
      expect(result.current.submit()).toBe('amount')
    })
    expect(result.current.fieldErrors.amount).toBe("The card's credit is 35.00 GBP.")
    expect(createCardCreditRefundMock).not.toHaveBeenCalled()
  })

  it('keeps an amount the user typed when the card changes', async () => {
    const { result } = renderBankForm()
    await openBankLoaded(result)

    act(() => result.current.setField('counterpartId', 'amex'))
    act(() => result.current.setField('amount', '10'))
    act(() => result.current.setField('counterpartId', 'visa'))

    expect(result.current.values.amount).toBe('10')
  })

  it('[AC P60-F15-card-refunds-and-credit-05] posts one credit refund from the chosen card to the bank', async () => {
    const onRefunded = vi.fn()
    const { result } = renderBankForm(onRefunded)
    await openBankLoaded(result)

    act(() => result.current.setField('counterpartId', 'visa'))
    act(() => {
      expect(result.current.submit()).toBeNull()
    })

    await waitFor(() => expect(onRefunded).toHaveBeenCalledTimes(1))
    expect(createCardCreditRefundMock).toHaveBeenCalledWith({
      date: '2026-11-05',
      sourceAccountId: 'visa',
      destinationAccountId: 'barclays',
      amount: 12.5,
      destinationAmount: null,
      note: null,
    })
    expect(result.current.isOpen).toBe(false)
  })

  it('asks for a card first', async () => {
    const { result } = renderBankForm()
    await openBankLoaded(result)

    act(() => {
      expect(result.current.submit()).toBe('counterpartId')
    })

    expect(result.current.fieldErrors).toEqual({
      counterpartId: 'Select a card.',
      amount: 'Amount must be greater than zero.',
      date: undefined,
    })
  })

  it('keeps the server message and reloads the cards', async () => {
    createCardCreditRefundMock.mockRejectedValue(new Error('boom'))
    const { result } = renderBankForm()
    await openBankLoaded(result)
    act(() => result.current.setField('counterpartId', 'amex'))
    getCardsInCreditMock.mockResolvedValue([CARDS_IN_CREDIT[0]])

    act(() => {
      result.current.submit()
    })

    await waitFor(() => expect(result.current.saveError).toBe('boom'))
    await waitFor(() => expect(result.current.counterparts).toHaveLength(1))
    expect(result.current.values.counterpartId).toBe('amex')
  })

  it('reports the cards load error, then no card when none in the currency has credit', async () => {
    getCardsInCreditMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([CARDS_IN_CREDIT[1]])
    const { result } = renderBankForm()

    act(() => result.current.open())
    await waitFor(() => expect(result.current.loadError).toBe('offline'))
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.hasNoCounterparts).toBe(true))
  })

  it('is dirty once a card is chosen', async () => {
    const { result } = renderBankForm()
    await openBankLoaded(result)

    act(() => result.current.setField('counterpartId', 'amex'))

    expect(result.current.isDirty).toBe(true)
  })
})
