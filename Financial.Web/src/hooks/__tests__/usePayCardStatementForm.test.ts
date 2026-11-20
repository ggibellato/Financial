import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CardStatementDto, PayableStatementDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { payableCardsOf, usePayCardStatementForm } from '../usePayCardStatementForm'
import { never } from '../../test-utils/deferred'

const { getPayableStatementsMock, markCardStatementPaidMock } = vi.hoisted(() => ({
  getPayableStatementsMock: vi.fn<FinancialApiClient['getPayableStatements']>(),
  markCardStatementPaidMock: vi.fn<FinancialApiClient['markCardStatementPaid']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getPayableStatements: getPayableStatementsMock,
    markCardStatementPaid: markCardStatementPaidMock,
  } as Partial<FinancialApiClient>,
}))

const STATEMENTS: PayableStatementDto[] = [
  { id: 'amex-oct', creditCardId: 'amex', creditCardName: 'Amex', year: 2026, month: 10, total: 75, currency: 'GBP' },
  { id: 'amex-nov', creditCardId: 'amex', creditCardName: 'Amex', year: 2026, month: 11, total: 12, currency: 'GBP' },
  { id: 'visa-oct', creditCardId: 'visa', creditCardName: 'Visa', year: 2026, month: 10, total: 20, currency: 'GBP' },
]

const PAID: CardStatementDto = {
  id: 'amex-oct',
  creditCardId: 'amex',
  creditCardName: 'Amex',
  year: 2026,
  month: 10,
  isPaid: true,
  outstandingTotal: 0,
  accumulatedOutstandingTotal: 12,
}

const BARCLAYS = { id: 'barclays', name: 'Barclays', currency: 'GBP' }

const REOPENERS = [
  ['opened again', (result: { current: ReturnType<typeof usePayCardStatementForm> }) => act(() => result.current.open())],
  ['cancelled', (result: { current: ReturnType<typeof usePayCardStatementForm> }) => act(() => result.current.cancel())],
] as const

function renderForm(onPaid = vi.fn()) {
  return renderHook(() => usePayCardStatementForm(BARCLAYS, onPaid))
}

async function openLoaded(result: { current: ReturnType<typeof usePayCardStatementForm> }) {
  act(() => result.current.open())
  await waitFor(() => expect(result.current.cards).toHaveLength(2))
}

describe('payableCardsOf', () => {
  it('lists each card once, in the order the statements come, named by its first statement', () => {
    expect(payableCardsOf([...STATEMENTS, { ...STATEMENTS[0], id: 'amex-dec', creditCardName: 'Amex renamed' }])).toEqual([
      { id: 'amex', name: 'Amex' },
      { id: 'visa', name: 'Visa' },
    ])
  })
})

describe('usePayCardStatementForm', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getPayableStatementsMock.mockReset().mockResolvedValue(STATEMENTS)
    markCardStatementPaidMock.mockReset().mockResolvedValue(PAID)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads nothing until opened', () => {
    renderForm()

    expect(getPayableStatementsMock).not.toHaveBeenCalled()
  })

  it('[AC P60-F08-statement-payment-and-card-screen-04] pays the chosen statement through mark-paid with this bank, as the card side does', async () => {
    const onPaid = vi.fn()
    const { result } = renderForm(onPaid)
    await openLoaded(result)

    act(() => result.current.setField('cardId', 'amex'))
    expect(result.current.statements.map((statement) => statement.id)).toEqual(['amex-oct', 'amex-nov'])
    expect(result.current.values.statementId).toBe('')
    act(() => result.current.setField('statementId', 'amex-oct'))
    expect(result.current.statement?.total).toBe(75)
    act(() => {
      expect(result.current.submit()).toBeNull()
    })

    await waitFor(() => expect(onPaid).toHaveBeenCalledTimes(1))
    expect(markCardStatementPaidMock).toHaveBeenCalledWith('amex-oct', { paymentSourceBankId: 'barclays', date: '2026-10-15' })
    expect(result.current.isOpen).toBe(false)
  })

  it('offers only cards in the bank currency, since a payment cannot cross currencies', async () => {
    getPayableStatementsMock.mockResolvedValue([
      ...STATEMENTS,
      { id: 'nubank-oct', creditCardId: 'nubank', creditCardName: 'Nubank', year: 2026, month: 10, total: 300, currency: 'BRL' },
    ])
    const { result } = renderForm()

    await openLoaded(result)

    expect(result.current.cards.map((card) => card.id)).toEqual(['amex', 'visa'])
  })

  it('has nothing to pay when every payable statement is in another currency', async () => {
    getPayableStatementsMock.mockResolvedValue([
      { id: 'nubank-oct', creditCardId: 'nubank', creditCardName: 'Nubank', year: 2026, month: 10, total: 300, currency: 'BRL' },
    ])
    const { result } = renderForm()

    act(() => result.current.open())

    await waitFor(() => expect(result.current.hasNothingToPay).toBe(true))
  })

  it('selects the statement of a card that has only one, and clears it when the card changes', async () => {
    const { result } = renderForm()
    await openLoaded(result)

    act(() => result.current.setField('cardId', 'visa'))
    expect(result.current.values.statementId).toBe('visa-oct')
    act(() => result.current.setField('cardId', 'amex'))
    expect(result.current.values.statementId).toBe('')
  })

  it('returns the first invalid field and its messages without calling the API', async () => {
    const { result } = renderForm()
    await openLoaded(result)

    act(() => {
      expect(result.current.submit()).toBe('cardId')
    })

    expect(result.current.fieldErrors).toEqual({ cardId: 'Select a card.', statementId: 'Select a statement.' })
    act(() => result.current.setField('cardId', 'visa'))
    act(() => result.current.setField('date', '2026-10-16'))
    act(() => {
      expect(result.current.submit()).toBe('date')
    })
    expect(result.current.fieldErrors.date).toBe('Date must not be in the future.')
    act(() => result.current.setField('date', ''))
    act(() => {
      result.current.submit()
    })
    expect(result.current.fieldErrors.date).toBe('Enter a date.')
    expect(markCardStatementPaidMock).not.toHaveBeenCalled()
  })

  it('keeps the values with the server message, reloads the list, and drops a statement that is gone', async () => {
    markCardStatementPaidMock.mockRejectedValue(new ApiError('This statement is already paid.', 409))
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('cardId', 'visa'))
    getPayableStatementsMock.mockResolvedValue(STATEMENTS.filter((statement) => statement.id !== 'visa-oct'))

    act(() => {
      result.current.submit()
    })
    expect(result.current.isSaving).toBe(true)

    await waitFor(() => expect(result.current.saveError).toBe('This statement is already paid.'))
    await waitFor(() => expect(result.current.cards).toHaveLength(1))
    expect(result.current.isSaving).toBe(false)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.values.cardId).toBe('')
    expect(result.current.values.statementId).toBe('')

    act(() => {
      expect(result.current.submit()).toBe('cardId')
    })
    expect(result.current.saveError).toBeNull()
  })

  it('reports a load error with retry, and nothing to pay on an empty list', async () => {
    getPayableStatementsMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([])
    const { result } = renderForm()

    act(() => result.current.open())
    await waitFor(() => expect(result.current.loadError).toBe('offline'))
    expect(result.current.hasNothingToPay).toBe(false)
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.hasNothingToPay).toBe(true))
    expect(result.current.loadError).toBeNull()
  })

  it('is clean again after cancel', async () => {
    const { result } = renderForm()
    await openLoaded(result)
    expect(result.current.isDirty).toBe(false)

    act(() => result.current.setField('date', '2026-10-14'))
    expect(result.current.isDirty).toBe(true)
    act(() => result.current.cancel())

    expect(result.current.isDirty).toBe(false)
    expect(result.current.isOpen).toBe(false)
  })

  it.each(REOPENERS)('starts over with today and no field messages when %s', async (_, reopen) => {
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('date', '2026-10-14'))
    act(() => {
      result.current.submit()
    })

    reopen(result)

    expect(result.current.values.date).toBe('2026-10-15')
    expect(result.current.fieldErrors).toEqual({})
  })

  it('drops the save message when opened again', async () => {
    markCardStatementPaidMock.mockRejectedValue(new ApiError('Bank is inactive.', 400))
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('cardId', 'visa'))
    act(() => {
      result.current.submit()
    })
    await waitFor(() => expect(result.current.saveError).toBe('Bank is inactive.'))

    act(() => result.current.open())

    expect(result.current.saveError).toBeNull()
  })

  it('clears the save message when a field changes and when a payment starts', async () => {
    markCardStatementPaidMock.mockRejectedValue(new ApiError('Bank is inactive.', 400))
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('cardId', 'visa'))
    const failPayment = async () => {
      act(() => {
        result.current.submit()
      })
      await waitFor(() => expect(result.current.saveError).toBe('Bank is inactive.'))
    }

    await failPayment()
    act(() => result.current.setField('date', '2026-10-14'))
    const afterEdit = result.current.saveError
    await failPayment()
    markCardStatementPaidMock.mockReturnValue(never())
    act(() => {
      result.current.submit()
    })

    expect(afterEdit).toBeNull()
    expect(result.current.saveError).toBeNull()
  })

  it('clears the card and statement messages when the card changes, and only the date message when the date changes', async () => {
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('date', ''))
    act(() => {
      result.current.submit()
    })

    act(() => result.current.setField('date', '2026-10-14'))
    const afterDate = result.current.fieldErrors
    act(() => result.current.setField('cardId', 'amex'))

    expect(afterDate).toEqual({ cardId: 'Select a card.', statementId: 'Select a statement.', date: undefined })
    expect(result.current.fieldErrors).toEqual({ cardId: undefined, statementId: undefined, date: undefined })
  })

  it('has nothing to pay only once an open form has loaded an empty list', async () => {
    let resolveLoad: (statements: PayableStatementDto[]) => void = () => {}
    getPayableStatementsMock.mockReturnValue(new Promise((resolve) => { resolveLoad = resolve }))
    const { result } = renderForm()
    const closed = result.current.hasNothingToPay
    act(() => result.current.open())
    const loading = result.current.hasNothingToPay

    await act(async () => resolveLoad([]))
    const loaded = result.current.hasNothingToPay
    act(() => result.current.cancel())

    expect({ closed, loading, loaded, cancelled: result.current.hasNothingToPay }).toEqual({ closed: false, loading: false, loaded: true, cancelled: false })
  })

  it.each([
    ['a card with several statements', 'cardId', 'amex'],
    ['a statement', 'statementId', 'amex-oct'],
    ['another date', 'date', '2026-10-14'],
  ] as const)('is dirty once %s is chosen', async (_, field, value) => {
    const { result } = renderForm()
    await openLoaded(result)

    act(() => result.current.setField(field, value))

    expect(result.current.isDirty).toBe(true)
  })

  it('is clean again once a card with several statements is unchosen', async () => {
    const { result } = renderForm()
    await openLoaded(result)
    act(() => result.current.setField('cardId', 'amex'))

    act(() => result.current.setField('cardId', ''))

    expect(result.current.isDirty).toBe(false)
  })
})
