import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountStatementDto, ExpenseDto } from '../../api/types'
import { MOVE_TARGETS_LOAD_ERROR, moveTargetsFor, useMoveChargeForm } from '../useMoveChargeForm'

const { getAccountStatementsMock, moveChargeToStatementMock } = vi.hoisted(() => ({
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
  moveChargeToStatementMock: vi.fn<FinancialApiClient['moveChargeToStatement']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccountStatements: getAccountStatementsMock,
    moveChargeToStatement: moveChargeToStatementMock,
  } as Partial<FinancialApiClient>,
}))

function statement(year: number, month: number, isPaid: boolean): AccountStatementDto {
  return { id: `s-${year}-${month}`, year, month, total: 10, outstandingTotal: isPaid ? 0 : 10, isPaid, dueDate: null, paidOn: null, paidFromAccountName: null }
}

const CHARGE: ExpenseDto = {
  id: 'c1',
  date: '2026-11-03',
  description: 'Taxi',
  value: 30,
  categoryId: 'category-mercado',
  categoryName: 'Mercado',
  paymentSourceBankId: null,
  paymentSourceBankName: null,
  creditCardId: 'card-amex',
  creditCardName: 'BA Amex',
  chargeDate: '2026-11-03',
  invoiceDate: '2026-11-01',
  paymentStatus: 'CreditCardCharge',
  roundUpAmount: null,
  suggestedRoundUpAmount: null,
  countsAsTithe: true,
}

describe('moveTargetsFor', () => {
  it('offers the 12 months after the current statement, across the year end, marking months with no statement', () => {
    const targets = moveTargetsFor('2026-11-01', [statement(2026, 12, false)])

    expect(targets).toHaveLength(12)
    expect(targets[0]).toEqual({ value: '2026-12', label: 'December 2026' })
    expect(targets[1]).toEqual({ value: '2027-01', label: 'January 2027 (new)' })
    expect(targets[11]).toEqual({ value: '2027-11', label: 'November 2027 (new)' })
  })

  it('leaves paid statements out', () => {
    const targets = moveTargetsFor('2026-11-01', [statement(2026, 12, true), statement(2026, 11, false)])

    expect(targets.map((target) => target.value)).not.toContain('2026-12')
    expect(targets).toHaveLength(11)
  })
})

describe('useMoveChargeForm', () => {
  let onMoved: (message: string) => void

  beforeEach(() => {
    getAccountStatementsMock.mockReset().mockResolvedValue([statement(2026, 12, false)])
    moveChargeToStatementMock.mockReset()
    onMoved = vi.fn<(message: string) => void>()
  })

  it('loads the card statements when opened and lists the targets', async () => {
    const { result } = renderHook(() => useMoveChargeForm(onMoved))

    act(() => result.current.open(CHARGE))

    expect(result.current.isOpen).toBe(true)
    expect(result.current.isDirty).toBe(false)
    await waitFor(() => expect(result.current.isLoadingTargets).toBe(false))
    expect(getAccountStatementsMock).toHaveBeenCalledWith('card-amex')
    expect(result.current.targets[0]).toEqual({ value: '2026-12', label: 'December 2026' })
  })

  it('shows the load error and retries', async () => {
    getAccountStatementsMock.mockRejectedValueOnce('offline')
    const { result } = renderHook(() => useMoveChargeForm(onMoved))

    act(() => result.current.open(CHARGE))

    await waitFor(() => expect(result.current.targetsError).toBe(MOVE_TARGETS_LOAD_ERROR))
    act(() => result.current.retryTargets())
    await waitFor(() => expect(result.current.targetsError).toBeNull())
    expect(getAccountStatementsMock).toHaveBeenCalledTimes(2)
  })

  it('asks for a statement before saving', () => {
    const { result } = renderHook(() => useMoveChargeForm(onMoved))
    act(() => result.current.open(CHARGE))

    act(() => result.current.submit())

    expect(result.current.form?.targetError).toBe('Select a statement.')
    expect(result.current.failedSubmitCount).toBe(1)
    expect(moveChargeToStatementMock).not.toHaveBeenCalled()
  })

  it('moves the charge, closes and reports it', async () => {
    moveChargeToStatementMock.mockResolvedValue({ ...CHARGE, invoiceDate: '2027-01-01' })
    const { result } = renderHook(() => useMoveChargeForm(onMoved))
    act(() => result.current.open(CHARGE))
    act(() => result.current.setTarget('2027-01'))

    expect(result.current.isDirty).toBe(true)
    act(() => result.current.submit())
    expect(result.current.form?.isSaving).toBe(true)

    await waitFor(() => expect(result.current.isOpen).toBe(false))
    expect(moveChargeToStatementMock).toHaveBeenCalledWith('c1', { year: 2027, month: 1 })
    expect(onMoved).toHaveBeenCalledWith('Charge moved')
  })

  it('keeps the form open with the server message when the move fails', async () => {
    moveChargeToStatementMock.mockRejectedValue(new ApiError('Charges can only move to a later, unpaid statement.', 400))
    const { result } = renderHook(() => useMoveChargeForm(onMoved))
    act(() => result.current.open(CHARGE))
    act(() => result.current.setTarget('2026-12'))

    act(() => result.current.submit())

    await waitFor(() => expect(result.current.form?.error).toBe('Charges can only move to a later, unpaid statement.'))
    expect(result.current.form?.isSaving).toBe(false)
    expect(onMoved).not.toHaveBeenCalled()
  })

  it('reports no targets when every later month is paid', async () => {
    getAccountStatementsMock.mockResolvedValue(
      Array.from({ length: 12 }, (_, index) => statement(2026 + Math.floor((11 + index) / 12), ((11 + index) % 12) + 1, true)),
    )
    const { result } = renderHook(() => useMoveChargeForm(onMoved))

    act(() => result.current.open(CHARGE))

    await waitFor(() => expect(result.current.hasNoTargets).toBe(true))
  })

  it('reloads the statements after a refused move', async () => {
    moveChargeToStatementMock.mockRejectedValue(new ApiError('Charges can only move to a later, unpaid statement.', 400))
    const { result } = renderHook(() => useMoveChargeForm(onMoved))
    act(() => result.current.open(CHARGE))
    await waitFor(() => expect(result.current.isLoadingTargets).toBe(false))
    act(() => result.current.setTarget('2026-12'))

    act(() => result.current.submit())

    await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(2))
  })

  it('closes on cancel', () => {
    const { result } = renderHook(() => useMoveChargeForm(onMoved))
    act(() => result.current.open(CHARGE))

    act(() => result.current.cancel())

    expect(result.current.isOpen).toBe(false)
  })
})
