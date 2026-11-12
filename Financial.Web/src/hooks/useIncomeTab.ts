import { useRef, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import { isNotFound } from '../api/apiError'
import type { AccountDto, IncomeDto, IncomeSourceDto } from '../api/types'
import { deleteFailedMessage } from '../utils/formatters'
import { useAsyncResource } from './useAsyncResource'
import { useConfirmedDelete } from './useConfirmedDelete'
import { useDiscardPrompt } from './useDiscardPrompt'
import { incomeSavedMessage, useIncomeForm } from './useIncomeForm'
import { useSuccessToast } from './useSuccessToast'

export const INCOMES_LOAD_ERROR = 'Unable to load the incomes.'
export const INCOME_GONE_WARNING = 'This income no longer exists; the list was refreshed.'

interface IncomeTabData {
  incomes: IncomeDto[]
  incomeSources: IncomeSourceDto[]
  banks: AccountDto[]
}

const NO_INCOMES: IncomeDto[] = []
const NO_SOURCES: IncomeSourceDto[] = []
const NO_BANKS: AccountDto[] = []

function loadIncomeTab(year: number, month: number): Promise<IncomeTabData> {
  return Promise.all([apiClient.getIncomesByMonth(year, month), apiClient.getIncomeSources(), apiClient.getAccounts()]).then(
    ([incomes, incomeSources, accounts]) => ({
      incomes,
      incomeSources,
      banks: accounts.filter((account) => account.capabilities.includes('ReceivesIncome')).sort((a, b) => a.name.localeCompare(b.name)),
    }),
  )
}

export function useIncomeTab(year: number, month: number) {
  const { data, isLoading, error, retry, refresh } = useAsyncResource(
    () => loadIncomeTab(year, month),
    [year, month],
    INCOMES_LOAD_ERROR,
    { initialLoading: true },
  )
  const { showSuccess } = useSuccessToast()
  const [isGoneNoticeShown, setIsGoneNoticeShown] = useState(false)
  const deletedIncomeWasGone = useRef(false)
  const incomeSources = data?.incomeSources ?? NO_SOURCES
  const banks = data?.banks ?? NO_BANKS

  const refreshAfter = (successMessage: string | null) => {
    setIsGoneNoticeShown(successMessage === null)
    if (successMessage) showSuccess(successMessage)
    void refresh()
  }

  const incomeForm = useIncomeForm(
    incomeSources,
    banks,
    (_saved, isEdit, potsAccountName) => refreshAfter(isEdit ? 'Income updated' : incomeSavedMessage(potsAccountName)),
    { viewedMonth: { year, month }, noBankByDefault: true, onNotFound: () => refreshAfter(null) },
  )
  const discardPrompt = useDiscardPrompt(incomeForm.isIncomeDirty)

  const removeIncome = (income: IncomeDto) => {
    deletedIncomeWasGone.current = false
    return apiClient.deleteIncome(income.id).catch((err: unknown) => {
      if (!isNotFound(err)) throw err
      deletedIncomeWasGone.current = true
    })
  }

  const incomeDelete = useConfirmedDelete(removeIncome, () => deleteFailedMessage('income'), (income) => {
    if (incomeForm.editingIncomeId === income.id) incomeForm.cancelIncomeForm()
    refreshAfter(deletedIncomeWasGone.current ? null : 'Income deleted')
  })

  return {
    incomes: data?.incomes ?? NO_INCOMES,
    incomeSources,
    banks,
    isLoading,
    error,
    retry,
    isGoneNoticeShown,
    dismissGoneNotice: () => setIsGoneNoticeShown(false),
    incomeForm,
    incomeDelete,
    discardPrompt,
    openCreate: () => discardPrompt.confirmDiscard(incomeForm.showCreateIncomeForm),
    openEdit: (income: IncomeDto) => discardPrompt.confirmDiscard(() => incomeForm.showEditIncomeForm(income)),
    closeForm: () => discardPrompt.confirmDiscard(incomeForm.cancelIncomeForm),
  }
}
