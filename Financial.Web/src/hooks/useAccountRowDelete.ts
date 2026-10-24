import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { AccountTransactionDto } from '../api/types'
import { getDeleteErrorMessage } from '../utils/formatters'

function deleteSourceRecord(row: AccountTransactionDto): Promise<void> {
  if (row.expense) return apiClient.deleteExpense(row.expense.id)
  if (row.income) return apiClient.deleteIncome(row.income.id)
  if (row.transfer) return apiClient.deleteAccountTransfer(row.transfer.id)
  if (row.adjustment) return apiClient.deleteBalanceAdjustment(row.adjustment.bankId, row.adjustment.id)
  return Promise.reject(new Error('This row has no record to delete.'))
}

export function useAccountRowDelete(onDeleted: (row: AccountTransactionDto) => void) {
  const [row, setRow] = useState<AccountTransactionDto | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const requestDelete = (requested: AccountTransactionDto) => {
    setRow(requested)
    setIsOpen(true)
    setError(null)
  }

  const cancel = () => {
    if (!isDeleting) setIsOpen(false)
  }

  const confirm = () => {
    if (!row || !isOpen) return
    const deleted = row
    setIsDeleting(true)
    setError(null)
    void deleteSourceRecord(deleted)
      .then(() => {
        setIsOpen(false)
        onDeleted(deleted)
      })
      .catch((err: unknown) => {
        setError(getDeleteErrorMessage(err, 'Failed to delete'))
      })
      .finally(() => setIsDeleting(false))
  }

  return { row, isOpen, isDeleting, error, requestDelete, cancel, confirm }
}
