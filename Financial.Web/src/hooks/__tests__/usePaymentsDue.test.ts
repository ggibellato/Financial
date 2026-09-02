import { renderHook, waitFor } from '@testing-library/react'
import { act } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PaymentDueDto } from '../../api/types'
import { PAYMENT_DUE_BANNER_DISMISS_MS, usePaymentsDue } from '../usePaymentsDue'

const { getPaymentsDueMock } = vi.hoisted(() => ({
  getPaymentsDueMock: vi.fn<FinancialApiClient['getPaymentsDue']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getPaymentsDue: getPaymentsDueMock,
  } as Partial<FinancialApiClient>,
}))

const SAMPLE_PAYMENTS: PaymentDueDto[] = [
  { type: 'Mensais', name: 'Internet', dueDate: '2026-09-05', daysRemaining: 3 },
]

describe('usePaymentsDue', () => {
  beforeEach(() => {
    getPaymentsDueMock.mockReset()
    // shouldAdvanceTime keeps real timers ticking so RTL's waitFor polling still resolves
    // while the auto-dismiss timeout is driven by fake time.
    vi.useFakeTimers({ shouldAdvanceTime: true })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('calls_getPaymentsDue_on_mount', async () => {
    getPaymentsDueMock.mockResolvedValue([])

    renderHook(() => usePaymentsDue())

    await waitFor(() => expect(getPaymentsDueMock).toHaveBeenCalledTimes(1))
  })

  it('does_not_poll', async () => {
    getPaymentsDueMock.mockResolvedValue([])

    renderHook(() => usePaymentsDue())
    await waitFor(() => expect(getPaymentsDueMock).toHaveBeenCalledTimes(1))

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60000)
    })

    expect(getPaymentsDueMock).toHaveBeenCalledTimes(1)
  })

  it('payments_null_when_response_is_empty', async () => {
    getPaymentsDueMock.mockResolvedValue([])

    const { result } = renderHook(() => usePaymentsDue())

    await waitFor(() => expect(getPaymentsDueMock).toHaveBeenCalledTimes(1))
    expect(result.current.payments).toBeNull()
  })

  it('payments_set_when_response_is_non_empty', async () => {
    getPaymentsDueMock.mockResolvedValue(SAMPLE_PAYMENTS)

    const { result } = renderHook(() => usePaymentsDue())

    await waitFor(() => expect(result.current.payments).toEqual(SAMPLE_PAYMENTS))
  })

  it('payments_null_when_fetch_rejects', async () => {
    getPaymentsDueMock.mockRejectedValue(new Error('network error'))

    const { result } = renderHook(() => usePaymentsDue())

    await waitFor(() => expect(getPaymentsDueMock).toHaveBeenCalledTimes(1))
    expect(result.current.payments).toBeNull()
  })

  it('auto_dismisses_after_10_seconds', async () => {
    getPaymentsDueMock.mockResolvedValue(SAMPLE_PAYMENTS)

    const { result } = renderHook(() => usePaymentsDue())
    await waitFor(() => expect(result.current.payments).toEqual(SAMPLE_PAYMENTS))

    await act(async () => {
      await vi.advanceTimersByTimeAsync(PAYMENT_DUE_BANNER_DISMISS_MS)
    })

    expect(result.current.payments).toBeNull()
  })

  it('dismiss_clears_payments_immediately', async () => {
    getPaymentsDueMock.mockResolvedValue(SAMPLE_PAYMENTS)

    const { result } = renderHook(() => usePaymentsDue())
    await waitFor(() => expect(result.current.payments).toEqual(SAMPLE_PAYMENTS))

    act(() => result.current.dismiss())

    expect(result.current.payments).toBeNull()
  })

  it('dismiss_cancels_the_pending_auto_dismiss_timer', async () => {
    getPaymentsDueMock.mockResolvedValue(SAMPLE_PAYMENTS)

    const { result } = renderHook(() => usePaymentsDue())
    await waitFor(() => expect(result.current.payments).toEqual(SAMPLE_PAYMENTS))

    act(() => result.current.dismiss())

    await act(async () => {
      await vi.advanceTimersByTimeAsync(PAYMENT_DUE_BANNER_DISMISS_MS)
    })

    expect(result.current.payments).toBeNull()
  })
})
