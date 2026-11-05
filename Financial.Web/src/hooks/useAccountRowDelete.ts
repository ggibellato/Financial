import { apiClient } from '../api/financialApiClient'
import type { AccountTransactionDto } from '../api/types'
import { isCreditRefund } from '../components/accountActions'
import { useConfirmedDelete } from './useConfirmedDelete'

function deleteSourceRecord(row: AccountTransactionDto): Promise<void> {
  if (row.expense) return apiClient.deleteExpense(row.expense.id)
  if (row.income) return apiClient.deleteIncome(row.income.id)
  if (row.transfer) return isCreditRefund(row) ? apiClient.deleteCardCreditRefund(row.transfer.id) : apiClient.deleteAccountTransfer(row.transfer.id)
  if (row.adjustment) return apiClient.deleteBalanceAdjustment(row.adjustment.bankId, row.adjustment.id)
  if (row.movement) return apiClient.deleteReserveMovement(row.movement.id)
  return Promise.reject(new Error('This row has no record to delete.'))
}

export function useAccountRowDelete(onDeleted: (row: AccountTransactionDto) => void) {
  const { target, request, ...rowDelete } = useConfirmedDelete(deleteSourceRecord, () => 'Failed to delete', onDeleted)
  return { ...rowDelete, row: target, requestDelete: request }
}
