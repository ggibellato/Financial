import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CreditCardCreateDto, CreditCardDto, CreditCardUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface CreditCardsState {
  creditCards: CreditCardDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  updatingCardId: string | null
  updateError: string | null
  deletingId: string | null
  deleteError: string | null
}

type CreditCardsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: CreditCardDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'UPDATE_START'; payload: string }
  | { type: 'UPDATE_SUCCESS' }
  | { type: 'UPDATE_ERROR'; payload: string }
  | { type: 'DELETE_START'; payload: string }
  | { type: 'DELETE_SUCCESS' }
  | { type: 'DELETE_ERROR'; payload: string }

const INITIAL_STATE: CreditCardsState = {
  creditCards: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  updatingCardId: null,
  updateError: null,
  deletingId: null,
  deleteError: null,
}

function reducer(state: CreditCardsState, action: CreditCardsAction): CreditCardsState {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, isLoading: true, error: null }
    case 'FETCH_SUCCESS':
      return { ...state, isLoading: false, creditCards: action.payload }
    case 'FETCH_ERROR':
      return { ...state, isLoading: false, error: action.payload }
    case 'RETRY':
      return { ...state, retryCount: state.retryCount + 1 }
    case 'UPDATE_START':
      return { ...state, updatingCardId: action.payload, updateError: null }
    case 'UPDATE_SUCCESS':
      return { ...state, updatingCardId: null }
    case 'UPDATE_ERROR':
      return { ...state, updatingCardId: null, updateError: action.payload }
    case 'DELETE_START':
      return { ...state, deletingId: action.payload, deleteError: null }
    case 'DELETE_SUCCESS':
      return { ...state, deletingId: null }
    case 'DELETE_ERROR':
      return { ...state, deletingId: null, deleteError: action.payload }
    default:
      return state
  }
}

export interface CreditCardsData {
  creditCards: CreditCardDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createCreditCard: (request: CreditCardCreateDto) => Promise<CreditCardDto>
  updatingCardId: string | null
  updateError: string | null
  updateCreditCard: (id: string, request: CreditCardUpdateDto) => Promise<CreditCardDto>
  deletingId: string | null
  deleteError: string | null
  deleteCreditCard: (id: string) => void
}

export function useCreditCards(): CreditCardsData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    dispatch({ type: 'FETCH_START' })
    void apiClient
      .getCreditCards()
      .then((creditCards) => dispatch({ type: 'FETCH_SUCCESS', payload: creditCards }))
      .catch((err: unknown) => {
        dispatch({ type: 'FETCH_ERROR', payload: getErrorMessage(err, 'Unable to load credit cards') })
      })
  }, [state.retryCount])

  const retry = useCallback(() => dispatch({ type: 'RETRY' }), [])

  const createCreditCard = useCallback(async (request: CreditCardCreateDto) => {
    const created = await apiClient.createCreditCard(request)
    dispatch({ type: 'RETRY' })
    return created
  }, [])

  const updateCreditCard = useCallback(async (id: string, request: CreditCardUpdateDto) => {
    dispatch({ type: 'UPDATE_START', payload: id })

    try {
      const updated = await apiClient.updateCreditCard(id, request)
      dispatch({ type: 'UPDATE_SUCCESS' })
      dispatch({ type: 'RETRY' })
      return updated
    } catch (err: unknown) {
      dispatch({ type: 'UPDATE_ERROR', payload: getErrorMessage(err, 'Failed to update credit card') })
      throw err
    }
  }, [])

  const deleteCreditCard = useCallback((id: string) => {
    dispatch({ type: 'DELETE_START', payload: id })

    void apiClient
      .deleteCreditCard(id)
      .then(() => {
        dispatch({ type: 'DELETE_SUCCESS' })
        dispatch({ type: 'RETRY' })
      })
      .catch((err: unknown) => {
        dispatch({ type: 'DELETE_ERROR', payload: getErrorMessage(err, 'Failed to delete credit card') })
      })
  }, [])

  return {
    creditCards: state.creditCards,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    createCreditCard,
    updatingCardId: state.updatingCardId,
    updateError: state.updateError,
    updateCreditCard,
    deletingId: state.deletingId,
    deleteError: state.deleteError,
    deleteCreditCard,
  }
}
