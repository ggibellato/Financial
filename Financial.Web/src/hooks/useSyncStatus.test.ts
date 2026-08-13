import { renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { SyncStatusResponseDto } from '../api/types'
import { useSyncStatus } from './useSyncStatus'

const getSyncStatusMock = vi.fn<FinancialApiClient['getSyncStatus']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getSyncStatus: getSyncStatusMock,
  }),
}))

const IDLE_STATUS: SyncStatusResponseDto = {
  cashFlow: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
  investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
}

const FAILED_STATUS: SyncStatusResponseDto = {
  cashFlow: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
  investment: {
    state: 'Failed',
    lastError: 'Drive request failed with a transient status (503 ServiceUnavailable).',
    lastSuccessfulSaveUtc: '2026-08-13T09:12:04Z',
  },
}

describe('useSyncStatus', () => {
  beforeEach(() => {
    getSyncStatusMock.mockReset()
    // shouldAdvanceTime keeps real timers ticking so RTL's waitFor polling still resolves
    // while setInterval is driven by fake time.
    vi.useFakeTimers({ shouldAdvanceTime: true })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('calls_getSyncStatus_on_mount', async () => {
    getSyncStatusMock.mockResolvedValue(IDLE_STATUS)

    renderHook(() => useSyncStatus())

    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(1))
  })

  it('calls_getSyncStatus_every_15_seconds', async () => {
    getSyncStatusMock.mockResolvedValue(IDLE_STATUS)

    renderHook(() => useSyncStatus())
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(1))

    vi.advanceTimersByTime(15000)
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(2))

    vi.advanceTimersByTime(15000)
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(3))
  })

  it('failed_poll_does_not_throw_and_next_tick_still_polls', async () => {
    getSyncStatusMock.mockRejectedValueOnce(new Error('Network error'))
    getSyncStatusMock.mockResolvedValue(IDLE_STATUS)

    renderHook(() => useSyncStatus())
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(1))

    vi.advanceTimersByTime(15000)
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(2))
  })

  it('exposes_latest_successfully_polled_status', async () => {
    getSyncStatusMock.mockResolvedValue(FAILED_STATUS)

    const { result } = renderHook(() => useSyncStatus())

    await waitFor(() => expect(result.current.status).toEqual(FAILED_STATUS))
  })

  it('retains_previous_status_after_a_failed_poll', async () => {
    getSyncStatusMock.mockResolvedValueOnce(IDLE_STATUS)
    getSyncStatusMock.mockRejectedValueOnce(new Error('Network error'))

    const { result } = renderHook(() => useSyncStatus())
    await waitFor(() => expect(result.current.status).toEqual(IDLE_STATUS))

    vi.advanceTimersByTime(15000)
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(2))
    expect(result.current.status).toEqual(IDLE_STATUS)
  })

  it('clears_interval_on_unmount', async () => {
    getSyncStatusMock.mockResolvedValue(IDLE_STATUS)

    const { unmount } = renderHook(() => useSyncStatus())
    await waitFor(() => expect(getSyncStatusMock).toHaveBeenCalledTimes(1))

    unmount()
    vi.advanceTimersByTime(30000)
    expect(getSyncStatusMock).toHaveBeenCalledTimes(1)
  })
})
