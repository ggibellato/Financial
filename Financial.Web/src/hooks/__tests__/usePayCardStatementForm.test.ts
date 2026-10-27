import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CardStatementDto, PayableStatementDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { payableCardsOf, usePayCardStatementForm } from '../usePayCardStatementForm'

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

const BARCLAYS = { id: 'barclays', name: 'Barclays' }

function renderForm(onPaid = vi.fn()) {
  return renderHook(() => usePayCardStatementForm(BARCLAYS, onPaid))
}

async function openLoaded(result: { current: ReturnType<typeof usePayCardStatementForm> }) {
  act(() => result.current.open())
  await waitFor(() => expect(result.current.cards).toHaveLength(2))
}

describe('payableCardsOf', () => {
  it('lists each card once, in the order the statements come', () => {
    expect(payableCardsOf(STATEMENTS)).toEqual([
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

  it('is dirty once a card is chosen or the date changes, and clean again after cancel', async () => {
    const { result } = renderForm()
    await openLoaded(result)
    expect(result.current.isDirty).toBe(false)

    act(() => result.current.setField('date', '2026-10-14'))
    expect(result.current.isDirty).toBe(true)
    act(() => result.current.cancel())

    expect(result.current.isDirty).toBe(false)
    expect(result.current.isOpen).toBe(false)
  })
})
