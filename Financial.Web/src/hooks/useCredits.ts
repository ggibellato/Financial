import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { createFinancialApiClient } from '../api/financialApiClient'
import type { CreditDto, SelectedNode } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import type { PeriodFilterOption } from '../utils/periodFilter'
import { getPeriodFilterStartDate } from '../utils/periodFilter'

export type ViewMode = 'Stacked' | 'Grouped'
export type ChartType = 'Bar' | 'Line'
export type CreditType = 'Dividend' | 'Rent'
export type CreditFormField = 'formDate' | 'formType' | 'formValue'

export interface MonthBucket {
  month: string
  total: number
  byType: Record<string, number>
}

interface PersistedPrefs {
  filter: PeriodFilterOption
  mode: ViewMode
  chartType: ChartType
}

const DEFAULT_FILTER: PeriodFilterOption = 'last-12-months'
const DEFAULT_MODE: ViewMode = 'Stacked'
const DEFAULT_CHART_TYPE: ChartType = 'Bar'

interface CreditsState {
  credits: CreditDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  selectedFilter: PeriodFilterOption
  selectedMode: ViewMode
  selectedChartType: ChartType
  filterPersistence: Map<string, PersistedPrefs>
  isFormVisible: boolean
  editingId: string | null
  formDate: string
  formType: string
  formValue: string
  isSaving: boolean
  saveError: string | null
  deleteError: string | null
}

type CreditsAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START'; payload: { key: string } }
  | { type: 'FETCH_SUCCESS'; payload: CreditDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SET_FILTER'; payload: { filter: PeriodFilterOption; key: string } }
  | { type: 'SET_MODE'; payload: { mode: ViewMode; key: string } }
  | { type: 'SET_CHART_TYPE'; payload: { chartType: ChartType; key: string } }
  | { type: 'SHOW_NEW_FORM' }
  | { type: 'SHOW_EDIT_FORM'; payload: CreditDto }
  | { type: 'CANCEL_FORM' }
  | { type: 'SET_FORM_FIELD'; payload: { field: CreditFormField; value: string } }
  | { type: 'SAVE_START' }
  | { type: 'SAVE_SUCCESS'; payload: CreditDto[] }
  | { type: 'SAVE_ERROR'; payload: string }
  | { type: 'DELETE_SUCCESS'; payload: CreditDto[] }
  | { type: 'DELETE_ERROR'; payload: string }

const BLANK_FORM = {
  isFormVisible: false,
  editingId: null,
  formDate: '',
  formType: 'Dividend',
  formValue: '',
  isSaving: false,
  saveError: null,
} as const

const INITIAL_STATE: CreditsState = {
  credits: [],
  isLoading: false,
  error: null,
  retryCount: 0,
  selectedFilter: DEFAULT_FILTER,
  selectedMode: DEFAULT_MODE,
  selectedChartType: DEFAULT_CHART_TYPE,
  filterPersistence: new Map(),
  ...BLANK_FORM,
  deleteError: null,
}

function toInputDate(isoString: string): string {
  return isoString.split('T')[0]
}

function reducer(state: CreditsState, action: CreditsAction): CreditsState {
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
        selectedMode: prefs?.mode ?? DEFAULT_MODE,
        selectedChartType: prefs?.chartType ?? DEFAULT_CHART_TYPE,
      }
    }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, credits: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1, error: null }
    case 'SET_FILTER': {
      const newMap = new Map(state.filterPersistence)
      newMap.set(action.payload.key, {
        filter: action.payload.filter,
        mode: state.selectedMode,
        chartType: state.selectedChartType,
      })
      return { ...state, selectedFilter: action.payload.filter, filterPersistence: newMap }
    }
    case 'SET_MODE': {
      const newMap = new Map(state.filterPersistence)
      newMap.set(action.payload.key, {
        filter: state.selectedFilter,
        mode: action.payload.mode,
        chartType: state.selectedChartType,
      })
      return { ...state, selectedMode: action.payload.mode, filterPersistence: newMap }
    }
    case 'SET_CHART_TYPE': {
      const newMap = new Map(state.filterPersistence)
      newMap.set(action.payload.key, {
        filter: state.selectedFilter,
        mode: state.selectedMode,
        chartType: action.payload.chartType,
      })
      return { ...state, selectedChartType: action.payload.chartType, filterPersistence: newMap }
    }
    case 'SHOW_NEW_FORM':
      return {
        ...state,
        isFormVisible: true,
        editingId: null,
        formDate: '',
        formType: 'Dividend',
        formValue: '',
        saveError: null,
        isSaving: false,
      }
    case 'SHOW_EDIT_FORM': {
      const c = action.payload
      return {
        ...state,
        isFormVisible: true,
        editingId: c.id,
        formDate: toInputDate(c.date),
        formType: c.type,
        formValue: String(c.value),
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
      return { ...state, ...BLANK_FORM, credits: action.payload, deleteError: state.deleteError }
    case 'SAVE_ERROR':
      return { ...state, isSaving: false, saveError: action.payload }
    case 'DELETE_SUCCESS':
      return { ...state, credits: action.payload, deleteError: null }
    case 'DELETE_ERROR':
      return { ...state, deleteError: action.payload }
    default:
      return state
  }
}

export function buildSelectionKey(node: SelectedNode): string {
  if (node.nodeType === 'Asset') {
    return `Asset|${node.brokerName}|${node.portfolioName ?? ''}|${node.assetName ?? ''}`
  }
  if (node.nodeType === 'Portfolio') {
    return `Portfolio|${node.brokerName}|${node.portfolioName ?? ''}`
  }
  return `Broker|${node.brokerName}`
}

function buildMonthKey(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(date.getMonth() + 1)}/${date.getFullYear()}`
}

function aggregateByMonth(credits: CreditDto[]): MonthBucket[] {
  const map = new Map<string, MonthBucket>()
  for (const c of credits) {
    const key = buildMonthKey(new Date(c.date))
    const existing = map.get(key) ?? { month: key, total: 0, byType: {} }
    const byType = { ...existing.byType }
    byType[c.type] = (byType[c.type] ?? 0) + c.value
    map.set(key, { month: key, total: existing.total + c.value, byType })
  }
  return [...map.values()].sort((a, b) => {
    const [am, ay] = a.month.split('/').map(Number)
    const [bm, by] = b.month.split('/').map(Number)
    if (ay !== by) return ay - by
    return am - bm
  })
}

function computeCreditTypes(buckets: MonthBucket[]): string[] {
  const types = new Set<string>()
  for (const bucket of buckets) {
    for (const type of Object.keys(bucket.byType)) types.add(type)
  }
  return [...types].sort()
}

export interface CreditsData {
  credits: CreditDto[]
  filteredCredits: CreditDto[]
  chartData: MonthBucket[]
  creditTypes: string[]
  isLoading: boolean
  error: string | null
  retry: () => void
  selectedFilter: PeriodFilterOption
  selectedMode: ViewMode
  selectedChartType: ChartType
  setFilter: (filter: PeriodFilterOption) => void
  setMode: (mode: ViewMode) => void
  setChartType: (chartType: ChartType) => void
  isFormVisible: boolean
  editingId: string | null
  formDate: string
  formType: string
  formValue: string
  isSaving: boolean
  saveError: string | null
  deleteError: string | null
  nodeType: string | undefined
  showNewForm: () => void
  showEditForm: (credit: CreditDto) => void
  cancelForm: () => void
  setFormField: (field: CreditFormField, value: string) => void
  saveForm: () => void
  deleteCredit: (id: string) => void
}

export function useCredits(): CreditsData {
  const { selectedNode } = useSelectedNode()
  const apiClient = useMemo(() => createFinancialApiClient(), [])
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    if (!selectedNode) {
      dispatch({ type: 'RESET' })
      return
    }

    const key = buildSelectionKey(selectedNode)
    dispatch({ type: 'FETCH_START', payload: { key } })

    const { nodeType, brokerName, portfolioName, assetName } = selectedNode

    if (nodeType === 'Asset' && portfolioName && assetName) {
      void apiClient
        .getAssetDetails(brokerName, portfolioName, assetName)
        .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result.credits }))
        .catch((err: unknown) => {
          dispatch({
            type: 'FETCH_ERROR',
            payload: err instanceof Error ? err.message : 'Unable to load credits',
          })
        })
    } else if (nodeType === 'Broker') {
      void apiClient
        .getCreditsByBroker(brokerName)
        .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
        .catch((err: unknown) => {
          dispatch({
            type: 'FETCH_ERROR',
            payload: err instanceof Error ? err.message : 'Unable to load credits',
          })
        })
    } else if (nodeType === 'Portfolio' && portfolioName) {
      void apiClient
        .getCreditsByPortfolio(brokerName, portfolioName)
        .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result }))
        .catch((err: unknown) => {
          dispatch({
            type: 'FETCH_ERROR',
            payload: err instanceof Error ? err.message : 'Unable to load credits',
          })
        })
    }
  }, [selectedNode, apiClient, state.retryCount])

  const credits = useMemo(
    () =>
      [...state.credits].sort(
        (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
      ),
    [state.credits],
  )

  const filteredCredits = useMemo(() => {
    const start = getPeriodFilterStartDate(state.selectedFilter, new Date())
    if (!start) return credits
    return credits.filter((c) => new Date(c.date) >= start)
  }, [credits, state.selectedFilter])

  const chartData = useMemo(() => aggregateByMonth(filteredCredits), [filteredCredits])
  const creditTypes = useMemo(() => computeCreditTypes(chartData), [chartData])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setFilter = useCallback(
    (filter: PeriodFilterOption) => {
      if (!selectedNode) return
      dispatch({ type: 'SET_FILTER', payload: { filter, key: buildSelectionKey(selectedNode) } })
    },
    [selectedNode],
  )

  const setMode = useCallback(
    (mode: ViewMode) => {
      if (!selectedNode) return
      dispatch({ type: 'SET_MODE', payload: { mode, key: buildSelectionKey(selectedNode) } })
    },
    [selectedNode],
  )

  const setChartType = useCallback(
    (chartType: ChartType) => {
      if (!selectedNode) return
      dispatch({ type: 'SET_CHART_TYPE', payload: { chartType, key: buildSelectionKey(selectedNode) } })
    },
    [selectedNode],
  )

  const showNewForm = useCallback(() => dispatch({ type: 'SHOW_NEW_FORM' }), [])

  const showEditForm = useCallback(
    (credit: CreditDto) => dispatch({ type: 'SHOW_EDIT_FORM', payload: credit }),
    [],
  )

  const cancelForm = useCallback(() => dispatch({ type: 'CANCEL_FORM' }), [])

  const setFormField = useCallback((field: CreditFormField, value: string) => {
    dispatch({ type: 'SET_FORM_FIELD', payload: { field, value } })
  }, [])

  const saveForm = useCallback(() => {
    if (!selectedNode?.portfolioName || !selectedNode.assetName) return

    const { formDate, formType, formValue, editingId } = state

    if (!formDate.trim()) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Date is required' })
      return
    }

    const value = parseFloat(formValue)
    if (!formValue.trim() || !isFinite(value) || value <= 0) {
      dispatch({ type: 'SAVE_ERROR', payload: 'Value must be a positive number' })
      return
    }

    dispatch({ type: 'SAVE_START' })

    const base = {
      brokerName: selectedNode.brokerName,
      portfolioName: selectedNode.portfolioName,
      assetName: selectedNode.assetName,
      date: formDate,
      type: formType,
      value,
    }

    const call = editingId
      ? apiClient.updateCredit({ ...base, id: editingId })
      : apiClient.addCredit(base)

    void call
      .then((result) => dispatch({ type: 'SAVE_SUCCESS', payload: result.credits }))
      .catch((err: unknown) => {
        dispatch({
          type: 'SAVE_ERROR',
          payload: err instanceof Error ? err.message : 'Failed to save credit',
        })
      })
  }, [selectedNode, state, apiClient])

  const deleteCredit = useCallback(
    (id: string) => {
      if (!selectedNode?.portfolioName || !selectedNode.assetName) return
      if (!window.confirm('Delete this credit?')) return

      void apiClient
        .deleteCredit({
          brokerName: selectedNode.brokerName,
          portfolioName: selectedNode.portfolioName,
          assetName: selectedNode.assetName,
          id,
        })
        .then((result) => dispatch({ type: 'DELETE_SUCCESS', payload: result.credits }))
        .catch((err: unknown) => {
          dispatch({
            type: 'DELETE_ERROR',
            payload: err instanceof Error ? err.message : 'Failed to delete credit',
          })
        })
    },
    [selectedNode, apiClient],
  )

  return {
    credits,
    filteredCredits,
    chartData,
    creditTypes,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    selectedFilter: state.selectedFilter,
    selectedMode: state.selectedMode,
    selectedChartType: state.selectedChartType,
    setFilter,
    setMode,
    setChartType,
    isFormVisible: state.isFormVisible,
    editingId: state.editingId,
    formDate: state.formDate,
    formType: state.formType,
    formValue: state.formValue,
    isSaving: state.isSaving,
    saveError: state.saveError,
    deleteError: state.deleteError,
    nodeType: selectedNode?.nodeType,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteCredit,
  }
}
