import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import CalendarSyncStatusBadge from '../CalendarSyncStatusBadge'

describe('CalendarSyncStatusBadge', () => {
  it('shows the visible text "Synced" for a synced card', () => {
    render(<CalendarSyncStatusBadge state="Synced" />)

    expect(screen.getByText('Synced')).toBeInTheDocument()
  })

  it('shows a visible "Syncing…" label for a pending card', () => {
    render(<CalendarSyncStatusBadge state="Pending" />)

    expect(screen.getByText('Syncing…')).toBeInTheDocument()
  })

  it('shows a visible "Syncing…" label for a card that has never been synced (state undefined)', () => {
    render(<CalendarSyncStatusBadge state={undefined} />)

    expect(screen.getByText('Syncing…')).toBeInTheDocument()
  })

  it('shows the visible failure reason for an errored card, not just an icon', () => {
    render(<CalendarSyncStatusBadge state="Error" lastError="Rate limit exceeded" />)

    expect(screen.getByText('Sync failed: Rate limit exceeded')).toBeInTheDocument()
  })

  it('falls back to a generic reason when an errored card has no recorded error message', () => {
    render(<CalendarSyncStatusBadge state="Error" lastError={null} />)

    expect(screen.getByText('Sync failed: Unknown error')).toBeInTheDocument()
  })
})
