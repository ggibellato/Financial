import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { SyncStatusData } from '../../hooks/useSyncStatus'
import type { SyncStatusResponseDto } from '../../api/types'
import SyncStatusBanner from '../SyncStatusBanner'

const mockHookValue: SyncStatusData = {
  status: null,
}

vi.mock('../../hooks/useSyncStatus', () => ({
  useSyncStatus: () => mockHookValue,
}))

const IDLE_STATUS: SyncStatusResponseDto = {
  cashFlow: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
  investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
}

function setStatus(status: SyncStatusResponseDto | null) {
  mockHookValue.status = status
}

describe('SyncStatusBanner', () => {
  beforeEach(() => {
    setStatus(null)
  })

  it('no_banner_when_status_is_null', () => {
    render(<SyncStatusBanner />)

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('no_banner_when_both_contexts_idle', () => {
    setStatus(IDLE_STATUS)

    render(<SyncStatusBanner />)

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('banner_visible_when_cashflow_failed', () => {
    setStatus({
      cashFlow: { state: 'Failed', lastError: 'Drive request failed.', lastSuccessfulSaveUtc: null },
      investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
    })

    render(<SyncStatusBanner />)

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText(/CashFlow changes could not be saved/)).toBeInTheDocument()
    expect(screen.queryByText(/Investment changes could not be saved/)).not.toBeInTheDocument()
  })

  it('banner_visible_when_investment_failed', () => {
    setStatus({
      cashFlow: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
      investment: { state: 'Failed', lastError: 'Drive request failed.', lastSuccessfulSaveUtc: null },
    })

    render(<SyncStatusBanner />)

    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText(/Investment changes could not be saved/)).toBeInTheDocument()
    expect(screen.queryByText(/CashFlow changes could not be saved/)).not.toBeInTheDocument()
  })

  it('banner_names_both_contexts_when_both_failed', () => {
    setStatus({
      cashFlow: { state: 'Failed', lastError: 'CashFlow drive error.', lastSuccessfulSaveUtc: null },
      investment: { state: 'Failed', lastError: 'Investment drive error.', lastSuccessfulSaveUtc: null },
    })

    render(<SyncStatusBanner />)

    expect(screen.getByText(/CashFlow changes could not be saved/)).toBeInTheDocument()
    expect(screen.getByText(/Investment changes could not be saved/)).toBeInTheDocument()
  })

  it('banner_shows_last_error_message', () => {
    setStatus({
      cashFlow: {
        state: 'Failed',
        lastError: 'Drive request failed with a transient status (503 ServiceUnavailable).',
        lastSuccessfulSaveUtc: null,
      },
      investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
    })

    render(<SyncStatusBanner />)

    expect(
      screen.getByText(/Drive request failed with a transient status \(503 ServiceUnavailable\)/),
    ).toBeInTheDocument()
  })

  it('banner_shows_formatted_last_successful_save_time', () => {
    const localInstant = new Date(2026, 7, 13, 9, 12)
    setStatus({
      cashFlow: {
        state: 'Failed',
        lastError: 'Drive request failed.',
        lastSuccessfulSaveUtc: localInstant.toISOString(),
      },
      investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
    })

    render(<SyncStatusBanner />)

    expect(screen.getByText(/Last successful save: 13\/08\/2026 09:12/)).toBeInTheDocument()
  })

  it('banner_shows_never_when_no_prior_successful_save', () => {
    setStatus({
      cashFlow: { state: 'Failed', lastError: 'Drive request failed.', lastSuccessfulSaveUtc: null },
      investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
    })

    render(<SyncStatusBanner />)

    expect(screen.getByText(/Last successful save: Never/)).toBeInTheDocument()
  })

  it('banner_disappears_after_rerender_once_context_recovers', () => {
    setStatus({
      cashFlow: { state: 'Failed', lastError: 'Drive request failed.', lastSuccessfulSaveUtc: null },
      investment: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
    })

    const { rerender } = render(<SyncStatusBanner />)
    expect(screen.getByRole('alert')).toBeInTheDocument()

    setStatus(IDLE_STATUS)
    rerender(<SyncStatusBanner />)

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
