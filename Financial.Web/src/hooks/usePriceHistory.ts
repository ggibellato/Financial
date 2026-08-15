import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { AssetPriceSnapshotDto, SelectedNode } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import type { PeriodFilterOption } from '../utils/periodFilter'
import { DEFAULT_FILTER, getPeriodFilterStartDate } from '../utils/periodFilter'
import { getErrorMessage, toInputDate } from '../utils/formatters'

export type PriceHistoryFormField = 'formDate' | 'formPrice'

interface PersistedPrefs {
  filter: PeriodFilterOption
}

interface PriceHistoryState {
  entries: AssetPriceSnapshotDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  selectedFilter: PeriodFilterOption
  filterPersistence: Map<string, PersistedPrefs>
  isFormVisible: boolean
  editingDate: string | null
  formDate: string
  formPrice: string
  isSaving: boolean
  saveError: string | null
  deleteError: string | null
}

type PriceHistoryAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START'; payload: { key: string } }
  | { type: 'FETCH_SUCCESS'; payload: AssetPriceSnapshotDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SET_FILTER'; payload: { filter: PeriodFilterOption; key: string } }
  | { type: 'SHOW_NEW_FORM' }
  | { type: 'SHOW_EDIT_FORM'; payload: AssetPriceSnapshotDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FORM_FIELD'; payload: { field: PriceHistoryFormField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: AssetPriceSnapshotDto[] }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'DELETE_SUCCESS'; payload: AssetPriceSnapshotDto[] }
  | { type: 'DELETE_ERROR'; payload: string }

const BLANK_FORM = {
  isFormVisible: false,
  editingDate: null,
  formDate: '',
  formPrice: '',
  isSaving: false,
  saveError: null,
} as const

const INITIAL_STATE: PriceHistoryState = {
  entries: [],
  isLoading: false,
  error: null,
  retryCount: 0,
  selectedFilter: DEFAULT_FILTER,
  filterPersistence: new Map(),
  ...BLANK_FORM,
  deleteError: null,
}

function reducer(state: PriceHistoryState, action: PriceHistoryAction): PriceHistoryState {
  switch (action.type) {
    case 'RESET':
      return { ...INITIAL_STATE, filterPersistence: state.filterPersistence }
    case 'FETCH_START': {
      const prefs = state.filterPersistence.get(action.payload.key)
      return {
        ...INITIAL_STATE,
        isLoading: true,
        retryCount: state.retryCount,
        filterPersistence: state.filterPersistence,
        selectedFilter: prefs?.filter ?? DEFAULT_FILTER,
      }
    }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, entries: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1, error: null }
    case 'SET_FILTER': {
      const newMap = new Map(state.filterPersistence)
      newMap.set(action.payload.key, { filter: action.payload.filter })
      return { ...state, selectedFilter: action.payload.filter, filterPersistence: newMap }
    }
    case 'SHOW_NEW_FORM':
      return {
        ...state,
        isFormVisible: true,
        editingDate: null,
        formDate: '',
        formPrice: '',
        saveError: null,
        isSaving: false,
      }
    case 'SHOW_EDIT_FORM': {
      const entry = action.payload
      return {
        ...state,
        isFormVisible: true,
        editingDate: entry.date,
        formDate: toInputDate(entry.date),
        formPrice: String(entry.price),
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
      return { ...state, ...BLANK_FORM, entries: action.payload, deleteError: state.deleteError }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    case 'DELETE_SUCCESS':
      return { ...state, entries: action.payload, deleteError: null }
    case 'DELETE_ERROR':
      return { ...state, deleteError: action.payload }
    default:
      return state
  }
}

function buildSelectionKey(node: SelectedNode): string {
  return `Asset|${node.brokerName}|${node.portfolioName ?? ''}|${node.assetName ?? ''}`
}

export interface PriceHistoryData {
  entries: AssetPriceSnapshotDto[]
  filteredEntries: AssetPriceSnapshotDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  selectedFilter: PeriodFilterOption
  setFilter: (filter: PeriodFilterOption) => void
  isFormVisible: boolean
  editingDate: string | null
  formDate: string
  formPrice: string
  isSaving: boolean
  saveError: string | null
  deleteError: string | null
  showNewForm: () => void
  showEditForm: (entry: AssetPriceSnapshotDto) => void
  cancelForm: () => void
  setFormField: (field: PriceHistoryFormField, value: string) => void
  saveForm: () => void
  deleteEntry: (date: string) => void
}

// Price History is asset-only (no Broker/Portfolio aggregate view - a portfolio's price history
// has no single meaningful series to chart), so unlike useCredits, this only ever fetches when
// the selected node is an Asset.
export function usePriceHistory(): PriceHistoryData {
  const { selectedNode, scope } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (
      !selectedNode ||
      selectedNode.nodeType !== 'Asset' ||
      !selectedNode.portfolioName ||
      !selectedNode.assetName
    ) {
      dispatch({ type: 'RESET' })
      return
    }

    const key = buildSelectionKey(selectedNode)
    dispatch({ type: 'FETCH_START', payload: { key } })

    void apiClient
      .getAssetDetails(selectedNode.brokerName, selectedNode.portfolioName, selectedNode.assetName, scope)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result.priceHistory }))
      .catch((err: unknown) => {
        dispatch({
          type: 'FETCH_ERROR',
          payload: getErrorMessage(err, 'Unable to load price history'),
        })
      })
  }, [selectedNode, apiClient, scope, state.retryCount])

  const entries = useMemo(
    () => [...state.entries].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    [state.entries],
  )

  const filteredEntries = useMemo(() => {
    const start = getPeriodFilterStartDate(state.selectedFilter, new Date())
    if (!start) return entries
    return entries.filter((entry) => new Date(entry.date) >= start)
  }, [entries, state.selectedFilter])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setFilter = useCallback(
    (filter: PeriodFilterOption) => {
      if (!selectedNode) return
      dispatch({ type: 'SET_FILTER', payload: { filter, key: buildSelectionKey(selectedNode) } })
    },
    [selectedNode],
  )

  const showNewForm = useCallback(() => dispatch({ type: 'SHOW_NEW_FORM' }), [])

  const showEditForm = useCallback(
    (entry: AssetPriceSnapshotDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: entry }),
    [],
  )

  const cancelForm = useCallback(() => dispatch({ type: 'CANCEL_FORM' }), [])

  const setFormField = useCallback((field: PriceHistoryFormField, value: string) => {
    dispatch({ type: 'SET_FORM_FIELD', payload: { field, value } })
  }, [])

  const saveForm = useCallback(() => {
    if (!selectedNode?.portfolioName || !selectedNode.assetName) return

    const { formDate, formPrice } = state

    if (!formDate.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Date is required' })
      return
    }

    const price = parseFloat(formPrice)
    if (!formPrice.trim() || !isFinite(price) || price <= 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Price must be a positive number' })
      return
    }

    dispatch({ type: 'SAVE_START' })

    void apiClient
      .setAssetPrice({
        brokerName: selectedNode.brokerName,
        portfolioName: selectedNode.portfolioName,
        assetName: selectedNode.assetName,
        date: formDate,
        price,
      })
      .then((result) => dispatch({ type: 'SAVE_SUCCESS', payload: result.priceHistory }))
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: getErrorMessage(err, 'Failed to save price'),
        })
      })
  }, [selectedNode, state, apiClient])

  const deleteEntry = useCallback(
    (date: string) => {
      if (!selectedNode?.portfolioName || !selectedNode.assetName) return
      if (!window.confirm('Delete this price entry?')) return

      void apiClient
        .deleteAssetPrice({
          brokerName: selectedNode.brokerName,
          portfolioName: selectedNode.portfolioName,
          assetName: selectedNode.assetName,
          date,
        })
        .then((result) => dispatch({ type: 'DELETE_SUCCESS', payload: result.priceHistory }))
        .catch((err: unknown) => {
          dispatch({
            type: 'DELETE_ERROR',
            payload: getErrorMessage(err, 'Failed to delete price'),
          })
        })
    },
    [selectedNode, apiClient],
  )

  return {
    entries,
    filteredEntries,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    selectedFilter: state.selectedFilter,
    setFilter,
    isFormVisible: state.isFormVisible,
    editingDate: state.editingDate,
    formDate: state.formDate,
    formPrice: state.formPrice,
    isSaving: state.isSaving,
    saveError: state.saveError,
    deleteError: state.deleteError,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteEntry,
  }
}
