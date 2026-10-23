import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTransactionDto, BalanceAdjustmentDto, ExpenseDto, IncomeDto } from '../../api/types'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { NETWORK_DELETE_ERROR, useAccountRowDelete } from '../useAccountRowDelete'

const { deleteExpenseMock, deleteIncomeMock, deleteAccountTransferMock, deleteBalanceAdjustmentMock } = vi.hoisted(() => ({
  deleteExpenseMock: vi.fn<FinancialApiClient['deleteExpense']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
  deleteAccountTransferMock: vi.fn<FinancialApiClient['deleteAccountTransfer']>(),
  deleteBalanceAdjustmentMock: vi.fn<FinancialApiClient['deleteBalanceAdjustment']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    deleteExpense: deleteExpenseMock,
    deleteIncome: deleteIncomeMock,
    deleteAccountTransfer: deleteAccountTransferMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
  } as Partial<FinancialApiClient>,
}))

const EXPENSE_ROW = accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', amount: -42.1, expense: { id: 'e1' } as ExpenseDto })
const INCOME_ROW = accountTransactionRow({ id: 'i1', kind: 'Income', date: '2026-10-12', amount: 2500, income: { id: 'i1' } as IncomeDto })
const TRANSFER_ROW = accountTransactionRow({
  id: 't1',
  kind: 'TransferIn',
  date: '2026-10-05',
  amount: 200,
  transfer: { id: 't1' } as AccountTransactionDto['transfer'],
})
const ADJUSTMENT_ROW = accountTransactionRow({
  id: 'a1',
  kind: 'Adjustment',
  date: '2026-10-01',
  amount: -1.5,
  adjustment: { id: 'a1', bankId: 'barclays' } as BalanceAdjustmentDto,
})

async function confirmDelete(row: AccountTransactionDto, onDeleted = vi.fn()) {
  const { result } = renderHook(() => useAccountRowDelete(onDeleted))
  act(() => result.current.requestDelete(row))
  await act(async () => result.current.confirm())
  return { result, onDeleted }
}

describe('useAccountRowDelete', () => {
  beforeEach(() => {
    deleteExpenseMock.mockReset().mockResolvedValue(undefined)
    deleteIncomeMock.mockReset().mockResolvedValue(undefined)
    deleteAccountTransferMock.mockReset().mockResolvedValue(undefined)
    deleteBalanceAdjustmentMock.mockReset().mockResolvedValue(undefined)
  })

  it('opens for the row to delete and keeps the row while it closes, so the dialog text stays', () => {
    const { result } = renderHook(() => useAccountRowDelete(vi.fn()))

    act(() => result.current.requestDelete(EXPENSE_ROW))
    expect(result.current.isOpen).toBe(true)
    expect(result.current.row).toBe(EXPENSE_ROW)

    act(() => result.current.cancel())
    expect(result.current.isOpen).toBe(false)
    expect(result.current.row).toBe(EXPENSE_ROW)
    expect(deleteExpenseMock).not.toHaveBeenCalled()
  })

  it.each([
    ['an expense', EXPENSE_ROW, () => expect(deleteExpenseMock).toHaveBeenCalledWith('e1')],
    ['an income', INCOME_ROW, () => expect(deleteIncomeMock).toHaveBeenCalledWith('i1')],
    ['a transfer', TRANSFER_ROW, () => expect(deleteAccountTransferMock).toHaveBeenCalledWith('t1')],
    ['an adjustment', ADJUSTMENT_ROW, () => expect(deleteBalanceAdjustmentMock).toHaveBeenCalledWith('barclays', 'a1')],
  ])('deletes %s through its own endpoint and reports the row', async (_, row, assertCall) => {
    const { result, onDeleted } = await confirmDelete(row)

    assertCall()
    expect(onDeleted).toHaveBeenCalledWith(row)
    expect(result.current.isOpen).toBe(false)
  })

  it('keeps the row and shows the connection message when the delete cannot reach the server', async () => {
    deleteExpenseMock.mockRejectedValue(new ApiError('Failed to fetch', 0))

    const { result, onDeleted } = await confirmDelete(EXPENSE_ROW)

    expect(result.current.error).toBe(NETWORK_DELETE_ERROR)
    expect(result.current.isOpen).toBe(true)
    expect(result.current.isDeleting).toBe(false)
    expect(onDeleted).not.toHaveBeenCalled()
  })

  it('shows the server message when the delete is refused', async () => {
    deleteIncomeMock.mockRejectedValue(new ApiError('Income not found.', 404))

    const { result } = await confirmDelete(INCOME_ROW)

    expect(result.current.error).toBe('Income not found.')
  })
})
