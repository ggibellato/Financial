import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { DisposalRecordDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { getErrorMessage } from '../utils/formatters'

export const ALL_TAX_YEARS = 'All years'

export interface DisposalChain {
  active: DisposalRecordDto
  /** Superseded predecessors for this disposal, newest first. */
  history: DisposalRecordDto[]
}

interface DisposalsState {
  records: DisposalRecordDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  selectedTaxYear: string
  expandedIds: Set<string>
}

type DisposalsAction =
  | { type: 'RESET' }
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: DisposalRecordDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'SET_TAX_YEAR'; payload: string }
  | { type: 'TOGGLE_EXPANDED'; payload: string }

const INITIAL_STATE: DisposalsState = {
  records: [],
  isLoading: false,
  error: null,
  retryCount: 0,
  selectedTaxYear: ALL_TAX_YEARS,
  expandedIds: new Set(),
}

function reducer(state: DisposalsState, action: DisposalsAction): DisposalsState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_STATE
    case 'FETCH_START':
      return { ...INITIAL_STATE, isLoading: true, retryCount: state.retryCount }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, records: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1, error: null }
    case 'SET_TAX_YEAR':
      return { ...state, selectedTaxYear: action.payload }
    case 'TOGGLE_EXPANDED': {
      const next = new Set(state.expandedIds)
      if (next.has(action.payload)) {
        next.delete(action.payload)
      } else {
        next.add(action.payload)
      }
      return { ...state, expandedIds: next }
    }
    default:
      return state
  }
}

export interface DisposalsData {
  chains: DisposalChain[]
  filteredChains: DisposalChain[]
  taxYearOptions: string[]
  isLoading: boolean
  error: string | null
  retry: () => void
  selectedTaxYear: string
  setTaxYear: (year: string) => void
  expandedIds: Set<string>
  toggleExpanded: (id: string) => void
}

// Asset-only, like usePriceHistory - a broker/portfolio has no single disposal history to show.
export function useDisposals(): DisposalsData {
  const { selectedNode, scope } = useSelectedNode()
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

    dispatch({ type: 'FETCH_START' })

    void apiClient
      .getAssetDetails(selectedNode.brokerName, selectedNode.portfolioName, selectedNode.assetName, scope)
      .then((result) => dispatch({ type: 'FETCH_SUCCESS', payload: result.disposalRecords }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load disposals') })
      })
  }, [selectedNode, scope, state.retryCount])

  // DisposalRecord.TransactionId is stable across F03 regeneration (confirmed against
  // DisposalRecordRegenerator.ComputePlan's existingByTransactionId lookup), so every record
  // sharing one groups into a single disposal's full history. A group with no Active record
  // (the disposing transaction was deleted, per F03) has nothing current to attach history to
  // and is not shown.
  const chains = useMemo(() => {
    const byTransaction = new Map<string, DisposalRecordDto[]>()
    for (const record of state.records) {
      const group = byTransaction.get(record.transactionId)
      if (group) {
        group.push(record)
      } else {
        byTransaction.set(record.transactionId, [record])
      }
    }

    const result: DisposalChain[] = []
    for (const records of byTransaction.values()) {
      const active = records.find((r) => r.status === 'Active')
      if (!active) continue

      const history = records
        .filter((r) => r.id !== active.id)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())

      result.push({ active, history })
    }

    return result.sort((a, b) => new Date(b.active.date).getTime() - new Date(a.active.date).getTime())
  }, [state.records])

  const taxYearOptions = useMemo(() => {
    const years = new Set(chains.map((chain) => chain.active.taxYear))
    return [...years].sort((a, b) => b.localeCompare(a))
  }, [chains])

  const filteredChains = useMemo(() => {
    if (state.selectedTaxYear === ALL_TAX_YEARS) return chains
    return chains.filter((chain) => chain.active.taxYear === state.selectedTaxYear)
  }, [chains, state.selectedTaxYear])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const setTaxYear = useCallback((year: string) => dispatch({ type: 'SET_TAX_YEAR', payload: year }), [])

  const toggleExpanded = useCallback((id: string) => dispatch({ type: 'TOGGLE_EXPANDED', payload: id }), [])

  return {
    chains,
    filteredChains,
    taxYearOptions,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    selectedTaxYear: state.selectedTaxYear,
    setTaxYear,
    expandedIds: state.expandedIds,
    toggleExpanded,
  }
}
