import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PortfolioCreateDto, PortfolioDto, PortfolioUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface PortfoliosState {
  portfolios: PortfolioDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  deletingKey: string | null
  deleteError: string | null
}

type PortfoliosAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: PortfolioDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: PortfoliosState = {
  portfolios: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  deletingKey: null,
  deleteError: null,
}

function reducer(state: PortfoliosState, action: PortfoliosAction): PortfoliosState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, portfolios: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'DELETE_START':
      return { ...state, deletingKey: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingKey: null }
    case 'DELETE_ERROR':
      return { ...state, deletingKey: null, deleteError: action.payload }
    default:
      return state
  }
}

/** Uniquely identifies a portfolio row: names are only unique within their own broker. */
export function portfolioKey(brokerName: string, portfolioName: string): string {
  return `${brokerName}/${portfolioName}`
}

export interface PortfoliosData {
  portfolios: PortfolioDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createPortfolio: (request: PortfolioCreateDto) => Promise<PortfolioDto>
  updatePortfolio: (brokerName: string, currentName: string, request: PortfolioUpdateDto) => Promise<PortfolioDto>
  deletingKey: string | null
  deleteError: string | null
  deletePortfolio: (brokerName: string, portfolioName: string) => void
}

export function usePortfolios(): PortfoliosData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getAdminPortfolios()
      .then((portfolios) => dispatch({ type: 'FETCH_SUCCESS', payload: portfolios }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load portfolios') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createPortfolio = useCallback(async (request: PortfolioCreateDto) => {
    const created = await apiClient.createPortfolio(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updatePortfolio = useCallback(async (brokerName: string, currentName: string, request: PortfolioUpdateDto) => {
    const updated = await apiClient.updatePortfolio(brokerName, currentName, request)
    dispatch({ type: 'RETRY' })
    return updated
  }, [])

  const deletePortfolio = useCallback((brokerName: string, portfolioName: string) => {
    dispatch({ type: 'DELETE_START', payload: portfolioKey(brokerName, portfolioName) })

    void apiClient
      .deleteEmptyPortfolio(brokerName, portfolioName)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete portfolio') })
      })
  }, [])

  return {
    portfolios: state.portfolios,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createPortfolio,
    updatePortfolio,
    deletingKey: state.deletingKey,
    deleteError: state.deleteError,
    deletePortfolio,
  }
}
