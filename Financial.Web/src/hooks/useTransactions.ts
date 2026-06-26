import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AssetDetailsDto, TransactionDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'

export type TransactionFormField = 'formDate' | 'formType' | 'formQuantity' | 'formUnitPrice' | 'formFees'

interface TransactionsState {
  asset: AssetDetailsDto | null
  isLoading: boolean
  error: string | null
  retryCount: number
  isFormVisible: boolean
  editingId: string | null
  formDate: string
  formType: string
  formQuantity: string
  formUnitPrice: string
  formFees: string
  isSaving: boolean
  saveError: string | null
  deleteError: string | null
}

type TransactionsAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SHOW_NEW_FORM' }
  | { type: 'SHOW_EDIT_FORM'; payload: TransactionDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FORM_FIELD'; payload: { field: TransactionFormField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'DELETE_SUCCESS'; payload: AssetDetailsDto }
  | { type: 'DELETE_ERROR'; payload: string }

const BLANK_FORM = {
  isFormVisible: false,
  editingId: null,
  formDate: '',
  formType: 'Buy',
  formQuantity: '',
  formUnitPrice: '',
  formFees: '',
  isSaving: false,
  saveError: null,
} as const

const INITIAL_STATE: TransactionsState = {
  asset: null,
  isLoading: false,
  error: null,
  retryCount: 0,
  ...BLANK_FORM,
  deleteError: null,
}

function toInputDate(isoString: string): string {
  return isoString.split('T')[0]
}

function reducer(state: TransactionsState, action: TransactionsAction): TransactionsState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...INITIAL_STATE, isLoading: true, retryCount: state.retryCount }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, asset: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1, error: null }
    case 'SHOW_NEW_FORM':
      return {
        ...state,
        isFormVisible: true,
        editingId: null,
        formDate: '',
        formType: 'Buy',
        formQuantity: '',
        formUnitPrice: '',
        formFees: '',
        saveError: null,
        isSaving: false,
      }
    case 'SHOW_EDIT_FORM': {
      const t = action.payload
      return {
        ...state,
        isFormVisible: true,
        editingId: t.id,
        formDate: toInputDate(t.date),
        formType: t.type,
        formQuantity: String(t.quantity),
        formUnitPrice: String(t.unitPrice),
        formFees: String(t.fees),
        saveError: null,
        isSaving: false,
      }
    }
    case 'CANCEL_FORM':
      return { ...state, ...BLANK_FORM }
    case 'SET_FORM_FIELD':
      return { ...state, [action.payload.field]: action.payload.value }
    case 'SAVE_START':
      return { ...state, isSaving: true, saveError: null }
    case 'SAVE_SUCCESS':
      return { ...state, ...BLANK_FORM, asset: action.payload, deleteError: state.deleteError }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    case 'DELETE_SUCCESS':
      return { ...state, asset: action.payload, deleteError: null }
    case 'DELETE_ERROR':
      return { ...state, deleteError: action.payload }
    default:
      return state
  }
}

export interface TransactionsData {
  asset: AssetDetailsDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
  transactions: TransactionDto[]
  isFormVisible: boolean
  editingId: string | null
  formDate: string
  formType: string
  formQuantity: string
  formUnitPrice: string
  formFees: string
  isSaving: boolean
  saveError: string | null
  deleteError: string | null
  nodeType: string | undefined
  showNewForm: () => void
  showEditForm: (transaction: TransactionDto) => void
  cancelForm: () => void
  setFormField: (field: TransactionFormField, value: string) => void
  saveForm: () => void
  deleteTransaction: (id: string) => void
}

export function useTransactions(): TransactionsData {
  const { selectedNode } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  const isAsset =
    selectedNode?.nodeType === 'Asset' &&
    !!selectedNode.portfolioName &&
    !!selectedNode.assetName

  useEffect(() => {
    if (!isAsset || !selectedNode) {
      dispatch({ type: 'RESET' })
      return
    }

    const { brokerName, portfolioName, assetName } = selectedNode

    if (!portfolioName || !assetName) {
      dispatch({ type: 'RESET' })
      return
    }

    dispatch({ type: 'FETCH_START' })

    void apiClient
      .getAssetDetails(brokerName, portfolioName, assetName)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: err instanceof Error ? err.message : 'Unable to load asset details',
        })
      })
  }, [selectedNode, isAsset, apiClient, state.retryCount])

  const transactions = useMemo(() => {
    if (!state.asset) return []
    return [...state.asset.transactions].sort(
      (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
    )
  }, [state.asset])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const showNewForm = useCallback(() => dispatch({ type: 'SHOW_NEW_FORM' }), [])

  const showEditForm = useCallback((transaction: TransactionDto) => {
    dispatch({ type: 'SHOW_EDIT_FORM', payload: transaction })
  }, [])

  const cancelForm = useCallback(() => dispatch({ type: 'CANCEL_FORM' }), [])

  const setFormField = useCallback((field: TransactionFormField, value: string) => {
    dispatch({ type: 'SET_FORM_FIELD', payload: { field, value } })
  }, [])

  const saveForm = useCallback(() => {
    if (!selectedNode?.portfolioName || !selectedNode.assetName) return

    const { formDate, formType, formQuantity, formUnitPrice, formFees, editingId } = state

    if (!formDate.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Date is required' })
      return
    }

    const quantity = parseFloat(formQuantity)
    if (!formQuantity.trim() || !isFinite(quantity) || quantity <= 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Quantity must be a positive number' })
      return
    }

    const unitPrice = parseFloat(formUnitPrice)
    if (!formUnitPrice.trim() || !isFinite(unitPrice) || unitPrice <= 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Unit Price must be a positive number' })
      return
    }

    const fees = formFees.trim() === '' ? 0 : parseFloat(formFees)

    dispatch({ type: 'SAVE_START' })

    const base = {
      brokerName: selectedNode.brokerName,
      portfolioName: selectedNode.portfolioName,
      assetName: selectedNode.assetName,
      date: formDate,
      type: formType,
      quantity,
      unitPrice,
      fees,
    }

    const call = editingId
      ? apiClient.updateTransaction({ ...base, id: editingId })
      : apiClient.addTransaction(base)

    void call
      .then((result) => dispatch({ type: 'SAVE_SUCCESS', payload: result }))
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to save transaction',
        })
      })
  }, [selectedNode, state, apiClient])

  const deleteTransaction = useCallback(
    (id: string) => {
      if (!selectedNode?.portfolioName || !selectedNode.assetName) return
      if (!window.confirm('Delete this transaction?')) return

      void apiClient
        .deleteTransaction({
          brokerName: selectedNode.brokerName,
          portfolioName: selectedNode.portfolioName,
          assetName: selectedNode.assetName,
          id,
        })
        .then((result) => dispatch({ type: 'DELETE_SUCCESS', payload: result }))
        .catch((err: unknown) => {
          dispatch({
            type: 'DELETE_ERROR',
            payload: err instanceof Error ? err.message : 'Failed to delete transaction',
          })
        })
    },
    [selectedNode, apiClient],
  )

  return {
    asset: state.asset,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    transactions,
    isFormVisible: state.isFormVisible,
    editingId: state.editingId,
    formDate: state.formDate,
    formType: state.formType,
    formQuantity: state.formQuantity,
    formUnitPrice: state.formUnitPrice,
    formFees: state.formFees,
    isSaving: state.isSaving,
    saveError: state.saveError,
    deleteError: state.deleteError,
    nodeType: selectedNode?.nodeType,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteTransaction,
  }
}
