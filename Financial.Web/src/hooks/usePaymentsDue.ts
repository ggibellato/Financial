import { useEffect, useReducer } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { PaymentDueDto } from '../api/types'

/** How long the banner stays visible before auto-dismissing itself. */
export const PAYMENT_DUE_BANNER_DISMISS_MS = 10000

interface PaymentsDueState {
  payments: PaymentDueDto[] | null
}

type PaymentsDueAction = { type: 'FETCH_SUCCESS'; payload: PaymentDueDto[] } | { type: 'DISMISS' }

const INITIAL_STATE: PaymentsDueState = {
  payments: null,
}

function reducer(state: PaymentsDueState, action: PaymentsDueAction): PaymentsDueState {
  switch (action.type) {
    case 'FETCH_SUCCESS':
      return { ...state, payments: action.payload }
    case 'DISMISS':
      return { ...state, payments: null }
    default:
      return state
  }
}

export interface PaymentsDueData {
  payments: PaymentDueDto[] | null
  dismiss: () => void
}

export function usePaymentsDue(): PaymentsDueData {
  const [state, dispatch] = useReducer(reducer, INITIAL_STATE)

  useEffect(() => {
    let cancelled = false

    apiClient
      .getPaymentsDue()
      .then((result) => {
        if (!cancelled && result.length > 0) {
          dispatch({ type: 'FETCH_SUCCESS', payload: result })
        }
      })
      .catch(() => {
        // Fail-safe per F01/PRD: a network or server error simply means no banner is shown.
      })

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!state.payments) return
    const timeoutId = setTimeout(() => dispatch({ type: 'DISMISS' }), PAYMENT_DUE_BANNER_DISMISS_MS)
    return () => clearTimeout(timeoutId)
  }, [state.payments])

  return { payments: state.payments, dismiss: () => dispatch({ type: 'DISMISS' }) }
}
