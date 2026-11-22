import { useCallback, useEffect, useMemo, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { RealisedGainDto } from '../api/types'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { isAssetSelection } from '../utils/assetSelection'
import { useAsyncResource } from './useAsyncResource'

export const ALL_TAX_YEARS = 'All years'

export interface RealisedGainChain {
  active: RealisedGainDto
  /** Superseded predecessors for this realisedGain, newest first. */
  history: RealisedGainDto[]
}

interface RealisedGainsViewState {
  selectedTaxYear: string
  expandedIds: Set<string>
}

type RealisedGainsViewAction =
  | { type: 'RESET' }
  | { type: 'SET_TAX_YEAR'; payload: string }
  | { type: 'TOGGLE_EXPANDED'; payload: string }

const INITIAL_VIEW_STATE: RealisedGainsViewState = {
  selectedTaxYear: ALL_TAX_YEARS,
  expandedIds: new Set(),
}

const NO_RECORDS: RealisedGainDto[] = []

function reducer(state: RealisedGainsViewState, action: RealisedGainsViewAction): RealisedGainsViewState {
  switch (action.type) {
    case 'RESET':
      return INITIAL_VIEW_STATE
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

export interface RealisedGainsData {
  chains: RealisedGainChain[]
  filteredChains: RealisedGainChain[]
  taxYearOptions: string[]
  isLoading: boolean
  error: string | null
  retry: () => void
  selectedTaxYear: string
  setTaxYear: (year: string) => void
  expandedIds: Set<string>
  toggleExpanded: (id: string) => void
}

// Asset-only, like usePriceHistory - a broker/portfolio has no single realised gain history to show.
export function useRealisedGains(): RealisedGainsData {
  const { selectedNode, scope } = useSelectedNode()
  const [state, dispatch] = useReducer(reducer, INITIAL_VIEW_STATE)

  const { data, isLoading, error, retry: retryFetch } = useAsyncResource<RealisedGainDto[]>(
    () => {
      if (!isAssetSelection(selectedNode)) {
        return null
      }
      return apiClient
        .getAssetDetails(selectedNode.brokerName, selectedNode.portfolioName, selectedNode.assetName, scope)
        .then((result) => result.realisedGains)
    },
    [selectedNode, scope],
    'Unable to load realised gains.',
  )

  useEffect(() => dispatch({ type: 'RESET' }), [selectedNode, scope])

  const realisedGains = data ?? NO_RECORDS

  // RealisedGain.TransactionId is stable across F03 regeneration (confirmed against
  // RealisedGainRegenerator.ComputePlan's existingByTransactionId lookup), so every record
  // sharing one groups into a single realised gain's full history. A group with no Active record
  // (the selling transaction was deleted, per F03) has nothing current to attach history to
  // and is not shown.
  const chains = useMemo(() => {
    const byTransaction = new Map<string, RealisedGainDto[]>()
    for (const record of realisedGains) {
      const group = byTransaction.get(record.transactionId)
      if (group) {
        group.push(record)
      } else {
        byTransaction.set(record.transactionId, [record])
      }
    }

    const result: RealisedGainChain[] = []
    for (const records of byTransaction.values()) {
      const active = records.find((r) => r.status === 'Active')
      if (!active) continue

      const history = records
        .filter((r) => r.id !== active.id)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())

      result.push({ active, history })
    }

    return result
  }, [realisedGains])

  const taxYearOptions = useMemo(() => {
    const years = new Set(chains.map((chain) => chain.active.taxYear))
    return [...years].sort((a, b) => b.localeCompare(a))
  }, [chains])

  const filteredChains = useMemo(() => {
    if (state.selectedTaxYear === ALL_TAX_YEARS) return chains
    return chains.filter((chain) => chain.active.taxYear === state.selectedTaxYear)
  }, [chains, state.selectedTaxYear])

  const retry = useCallback(() => {
    retryFetch()
    dispatch({ type: 'RESET' })
  }, [retryFetch])

  const setTaxYear = useCallback((year: string) => dispatch({ type: 'SET_TAX_YEAR', payload: year }), [])

  const toggleExpanded = useCallback((id: string) => dispatch({ type: 'TOGGLE_EXPANDED', payload: id }), [])

  return {
    chains,
    filteredChains,
    taxYearOptions,
    isLoading,
    error,
    retry,
    selectedTaxYear: state.selectedTaxYear,
    setTaxYear,
    expandedIds: state.expandedIds,
    toggleExpanded,
  }
}
