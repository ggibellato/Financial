import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, NETWORK_FAILURE_STATUS } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountStatementDto } from '../../api/types'
import { NETWORK_SAVE_ERROR } from '../../utils/formatters'
import { dueDateRangeOf, useStatementDueDateForm } from '../useStatementDueDateForm'

const { setStatementDueDateMock } = vi.hoisted(() => ({
  setStatementDueDateMock: vi.fn<FinancialApiClient['setStatementDueDate']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { setStatementDueDate: setStatementDueDateMock } as Partial<FinancialApiClient>,
}))

const CARD_ID = 'card-amex'

const OCTOBER: AccountStatementDto = {
  id: 's-oct', year: 2026, month: 10, total: 210.45, creditIn: 0, outstandingTotal: 210.45, creditCarried: 0, status: 'Unpaid', isPaid: false, dueDate: '2026-10-25', paidOn: null, paidAmount: null, paidFromAccountName: null,
}
const NOVEMBER_UNSTORED: AccountStatementDto = { ...OCTOBER, id: null, month: 11, status: 'NothingDue', dueDate: null }

function renderForm(statement: AccountStatementDto = OCTOBER, cardId: string | null = CARD_ID) {
  const onSaved = vi.fn()
  const hook = renderHook(() => useStatementDueDateForm(cardId, onSaved))
  act(() => hook.result.current.open(statement))
  return { ...hook, onSaved }
}

describe('useStatementDueDateForm', () => {
  beforeEach(() => {
    setStatementDueDateMock.mockReset().mockResolvedValue(undefined)
  })

  it('limits the date to the first and last day of the statement month', () => {
    expect(dueDateRangeOf(OCTOBER)).toEqual({ min: '2026-10-01', max: '2026-10-31' })
    expect(dueDateRangeOf({ ...OCTOBER, year: 2028, month: 2 })).toEqual({ min: '2028-02-01', max: '2028-02-29' })
  })

  it('opens pre-filled with the row date and is dirty only once the date differs', () => {
    const { result } = renderForm()

    expect(result.current.form?.date).toBe('2026-10-25')
    expect(result.current.isDirty).toBe(false)

    act(() => result.current.setDate('2026-10-20'))
    expect(result.current.isDirty).toBe(true)

    act(() => result.current.setDate('2026-10-25'))
    expect(result.current.isDirty).toBe(false)
  })

  it('opens empty on a month with no date', () => {
    const { result } = renderForm(NOVEMBER_UNSTORED)

    expect(result.current.form?.date).toBe('')
    expect(result.current.isDirty).toBe(false)
  })

  it('refuses an empty date without calling the server', () => {
    const { result } = renderForm(NOVEMBER_UNSTORED)

    act(() => result.current.save())

    expect(result.current.form?.dateError).toBe('Enter a due date.')
    expect(result.current.failedSubmits).toBe(1)
    expect(setStatementDueDateMock).not.toHaveBeenCalled()
  })

  it.each(['2026-09-30', '2026-11-01'])('refuses %s, outside October, and clears the message when the date changes', (date) => {
    const { result } = renderForm()
    act(() => result.current.setDate(date))

    act(() => result.current.save())

    expect(result.current.form?.dateError).toBe('The due date must be in October 2026.')
    expect(setStatementDueDateMock).not.toHaveBeenCalled()

    act(() => result.current.setDate('2026-10-20'))
    expect(result.current.form?.dateError).toBeUndefined()
  })

  it('saves the date by year and month, closes and reports it', async () => {
    const { result, onSaved } = renderForm(NOVEMBER_UNSTORED)
    act(() => result.current.setDate('2026-11-25'))

    await act(async () => result.current.save())

    expect(setStatementDueDateMock).toHaveBeenCalledWith(CARD_ID, 2026, 11, { dueDate: '2026-11-25' })
    expect(result.current.form).toBeNull()
    expect(onSaved).toHaveBeenCalledWith('Due date saved')
  })

  it('clears with a null date, closes and reports it', async () => {
    const { result, onSaved } = renderForm()

    await act(async () => result.current.clear())

    expect(setStatementDueDateMock).toHaveBeenCalledWith(CARD_ID, 2026, 10, { dueDate: null })
    expect(result.current.form).toBeNull()
    expect(onSaved).toHaveBeenCalledWith('Due date cleared')
  })

  it('marks which action is pending and ignores a second submit or opening another row', () => {
    setStatementDueDateMock.mockReturnValue(new Promise(() => {}))
    const { result } = renderForm()

    act(() => result.current.clear())
    expect(result.current.form?.pendingAction).toBe('clear')

    act(() => result.current.save())
    act(() => result.current.clear())
    act(() => result.current.open(NOVEMBER_UNSTORED))

    expect(setStatementDueDateMock).toHaveBeenCalledTimes(1)
    expect(result.current.form?.statement).toBe(OCTOBER)
  })

  it("keeps the form and its value and shows the server's reason on a refusal", async () => {
    setStatementDueDateMock.mockRejectedValue(new ApiError("The October 2026 statement is paid, so its due date can't change.", 400))
    const { result, onSaved } = renderForm()
    act(() => result.current.setDate('2026-10-20'))

    await act(async () => result.current.save())

    expect(result.current.form?.error).toBe("The October 2026 statement is paid, so its due date can't change.")
    expect(result.current.form?.date).toBe('2026-10-20')
    expect(result.current.form?.pendingAction).toBeNull()
    expect(onSaved).not.toHaveBeenCalled()
  })

  it('leaves a form reopened on another month alone when the earlier request settles', async () => {
    let rejectSave: (reason: unknown) => void = () => {}
    setStatementDueDateMock.mockReturnValue(new Promise((_, reject) => { rejectSave = reject }))
    const { result } = renderForm()
    act(() => result.current.clear())
    act(() => result.current.close())
    act(() => result.current.open(NOVEMBER_UNSTORED))

    await act(async () => rejectSave(new ApiError('Refused.', 400)))

    expect(result.current.form?.statement).toBe(NOVEMBER_UNSTORED)
    expect(result.current.form?.error).toBeNull()
  })

  it('says so on a network failure', async () => {
    setStatementDueDateMock.mockRejectedValue(new ApiError('Failed to fetch', NETWORK_FAILURE_STATUS))
    const { result } = renderForm()

    await act(async () => result.current.save())

    expect(result.current.form?.error).toBe(NETWORK_SAVE_ERROR)
  })

  it('sends nothing without a card', () => {
    const { result } = renderForm(OCTOBER, null)

    act(() => result.current.save())

    expect(setStatementDueDateMock).not.toHaveBeenCalled()
  })
})
