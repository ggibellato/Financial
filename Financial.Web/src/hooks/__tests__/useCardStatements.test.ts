import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountStatementDto, BankDto, CardStatementDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { STATEMENTS_LOAD_ERROR, useCardStatements } from '../useCardStatements'

const { getAccountStatementsMock, getBanksMock, markCardStatementPaidMock, unmarkCardStatementPaidMock } = vi.hoisted(() => ({
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  markCardStatementPaidMock: vi.fn<FinancialApiClient['markCardStatementPaid']>(),
  unmarkCardStatementPaidMock: vi.fn<FinancialApiClient['unmarkCardStatementPaid']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccountStatements: getAccountStatementsMock,
    getBanks: getBanksMock,
    markCardStatementPaid: markCardStatementPaidMock,
    unmarkCardStatementPaid: unmarkCardStatementPaidMock,
  } as Partial<FinancialApiClient>,
}))

const CARD_ID = 'card-amex'

const OCTOBER: AccountStatementDto = {
  id: 's-oct', year: 2026, month: 10, total: 210.45, isPaid: false, dueDate: '2026-11-03', paidOn: null, paidFromAccountName: null,
}
const SEPTEMBER: AccountStatementDto = {
  id: 's-sep', year: 2026, month: 9, total: 512.3, isPaid: true, dueDate: null, paidOn: '2026-10-12', paidFromAccountName: 'Barclays',
}

const BANKS: BankDto[] = [
  { id: 'barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
  { id: 'closed', name: 'Closed', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: false },
]

const PAID_STATEMENT: CardStatementDto = {
  id: 's-oct', creditCardId: CARD_ID, creditCardName: 'BA Amex', year: 2026, month: 10, isPaid: true, outstandingTotal: 0, accumulatedOutstandingTotal: 0,
}

async function renderLoaded(onChanged = vi.fn()) {
  const hook = renderHook(() => useCardStatements(CARD_ID, onChanged))
  await waitFor(() => expect(hook.result.current.statements).toHaveLength(2))
  return hook
}

describe('useCardStatements', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getAccountStatementsMock.mockReset().mockResolvedValue([OCTOBER, SEPTEMBER])
    getBanksMock.mockReset().mockResolvedValue(BANKS)
    markCardStatementPaidMock.mockReset()
    unmarkCardStatementPaidMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads nothing without a card', () => {
    const { result } = renderHook(() => useCardStatements(null, vi.fn()))

    expect(result.current.isLoading).toBe(false)
    expect(getAccountStatementsMock).not.toHaveBeenCalled()
    expect(getBanksMock).not.toHaveBeenCalled()
  })

  it('loads the statements and offers only active banks as payers', async () => {
    const { result } = await renderLoaded()

    expect(getAccountStatementsMock).toHaveBeenCalledWith(CARD_ID)
    await waitFor(() => expect(result.current.payers.map((bank) => bank.id)).toEqual(['barclays']))
  })

  it('reports a failed load and reloads on retry', async () => {
    getAccountStatementsMock.mockRejectedValueOnce('offline')
    const { result } = renderHook(() => useCardStatements(CARD_ID, vi.fn()))

    await waitFor(() => expect(result.current.error).toBe(STATEMENTS_LOAD_ERROR))
    act(() => result.current.retry())

    await waitFor(() => expect(result.current.statements).toHaveLength(2))
  })

  it('opens the pay form on today, clean until a field changes', async () => {
    const { result } = await renderLoaded()

    act(() => result.current.openPayForm(OCTOBER))

    expect(result.current.payForm).toMatchObject({ bankId: '', date: '2026-10-15' })
    expect(result.current.isPayFormDirty).toBe(false)

    act(() => result.current.setPayField('bankId', 'barclays'))

    expect(result.current.isPayFormDirty).toBe(true)
  })

  it('asks for a bank and refuses a future date without calling the API', async () => {
    const { result } = await renderLoaded()
    act(() => result.current.openPayForm(OCTOBER))
    act(() => result.current.setPayField('date', '2026-10-16'))

    act(() => result.current.submitPayForm())

    expect(result.current.payForm?.fieldErrors).toEqual({ bankId: 'Select a bank', date: 'Date must not be in the future.' })
    expect(markCardStatementPaidMock).not.toHaveBeenCalled()

    act(() => result.current.setPayField('date', ''))
    act(() => result.current.submitPayForm())

    expect(result.current.payForm?.fieldErrors.date).toBe('Enter a date')
  })

  it('pays with the chosen bank and date, then closes, reloads and reports it', async () => {
    const onChanged = vi.fn()
    markCardStatementPaidMock.mockResolvedValue(PAID_STATEMENT)
    const { result } = await renderLoaded(onChanged)
    act(() => result.current.openPayForm(OCTOBER))
    act(() => result.current.setPayField('bankId', 'barclays'))
    act(() => result.current.setPayField('date', '2026-10-12'))

    act(() => result.current.submitPayForm())

    expect(result.current.payForm?.isSaving).toBe(true)
    await waitFor(() => expect(result.current.payForm).toBeNull())
    expect(markCardStatementPaidMock).toHaveBeenCalledWith('s-oct', { paymentSourceBankId: 'barclays', date: '2026-10-12' })
    expect(onChanged).toHaveBeenCalledWith('Statement paid')
    expect(getAccountStatementsMock).toHaveBeenCalledTimes(2)
  })

  it('keeps the form and shows the reason when the payment is refused', async () => {
    markCardStatementPaidMock.mockRejectedValue(new ApiError('This statement is already paid.', 409))
    const { result } = await renderLoaded()
    act(() => result.current.openPayForm(OCTOBER))
    act(() => result.current.setPayField('bankId', 'barclays'))

    act(() => result.current.submitPayForm())

    await waitFor(() => expect(result.current.payForm?.error).toBe('This statement is already paid.'))
    expect(result.current.payForm?.isSaving).toBe(false)
    expect(result.current.payForm?.bankId).toBe('barclays')
  })

  it('unmarks after confirmation, then reloads and reports it', async () => {
    const onChanged = vi.fn()
    unmarkCardStatementPaidMock.mockResolvedValue({ ...PAID_STATEMENT, id: 's-sep', isPaid: false })
    const { result } = await renderLoaded(onChanged)

    act(() => result.current.requestUnmark(SEPTEMBER))
    act(() => result.current.confirmUnmark())

    await waitFor(() => expect(result.current.unmark).toBeNull())
    expect(unmarkCardStatementPaidMock).toHaveBeenCalledWith('s-sep')
    expect(onChanged).toHaveBeenCalledWith('Statement unmarked')
  })

  it('keeps the unmark request open with the reason when it fails, and cancels it', async () => {
    unmarkCardStatementPaidMock.mockRejectedValue(new ApiError('The payment for this statement was changed elsewhere. Reload and try again.', 409))
    const { result } = await renderLoaded()
    act(() => result.current.requestUnmark(SEPTEMBER))

    act(() => result.current.confirmUnmark())

    await waitFor(() => expect(result.current.unmark?.error).toBe('The payment for this statement was changed elsewhere. Reload and try again.'))

    act(() => result.current.cancelUnmark())

    expect(result.current.unmark).toBeNull()
  })
})
