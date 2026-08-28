import { useCallback, useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { CreditCardDto, CreditCardUpdateDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

interface CreditCardsState {
  creditCards: CreditCardDto[]
  isLoading: boolean
  error: string | null
  retryCount: number
  updatingCardId: string | null
  updateError: string | null
}

type CreditCardsAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: CreditCardDto[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'RETRY' }
  | { type: 'UPDATE_START'; payload: string }
  | { type: 'UPDATE_SUCCESS' }
  | { type: 'UPDATE_ERROR'; payload: string }

const INITIAL_STATE: CreditCardsState = {
  creditCards: [],
  isLoading: true,
  error: null,
  retryCount: 0,
  updatingCardId: null,
  updateError: null,
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
    default:
      return state
  }
}

export interface CreditCardsData {
  creditCards: CreditCardDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  updatingCardId: string | null
  updateError: string | null
  updateCreditCard: (id: string, request: CreditCardUpdateDto) => void
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

  const updateCreditCard = useCallback(
    (id: string, request: CreditCardUpdateDto) => {
      dispatch({ type: 'UPDATE_START', payload: id })

      void apiClient
        .updateCreditCard(id, request)
        .then(() => {
          dispatch({ type: 'UPDATE_SUCCESS' })
          dispatch({ type: 'RETRY' })
        })
        .catch((err: unknown) => {
          dispatch({
            type: 'UPDATE_ERROR',
            payload: getErrorMessage(err, 'Failed to update credit card'),
          })
        })
    },
    [],
  )

  return {
    creditCards: state.creditCards,
    isLoading: state.isLoading,
    error: state.error,
    retry,
    updatingCardId: state.updatingCardId,
    updateError: state.updateError,
    updateCreditCard,
  }
}
