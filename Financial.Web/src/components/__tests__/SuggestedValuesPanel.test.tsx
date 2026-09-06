import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import SuggestedValuesPanel from '../SuggestedValuesPanel'
import type { SuggestionRow } from '../../hooks/useSuggestedValues'

const ROW_ZERO_CURRENT: SuggestionRow = {
  snapshotId: 's1',
  accountId: 'a1',
  accountName: 'PlatinumVisa8003',
  currentValue: 0,
  suggestedValue: '142.17',
  sourceDescription: 'BarclaysPlatinumVisa8003 — Aug 2026 statement',
  included: true,
  status: 'pending',
}

const ROW_NONZERO_CURRENT: SuggestionRow = {
  snapshotId: 's2',
  accountId: 'a2',
  accountName: 'ReservasPessoais',
  currentValue: 5400,
  suggestedValue: '5612.30',
  sourceDescription: 'Sum of reserve buckets — as of Jul 2026',
  included: true,
  status: 'pending',
}

function baseProps(overrides: Partial<React.ComponentProps<typeof SuggestedValuesPanel>> = {}) {
  return {
    phase: 'ready' as const,
    fetchError: null,
    rows: [],
    notUpdated: [],
    checkedCount: 0,
    applyProgress: null,
    succeededCount: 0,
    failedRows: [],
    onToggleIncluded: vi.fn(),
    onSetValue: vi.fn(),
    onApply: vi.fn(),
    onRetryFailed: vi.fn(),
    onRetryFetch: vi.fn(),
    onClose: vi.fn(),
    ...overrides,
  }
}

describe('SuggestedValuesPanel', () => {
  it('shows no suggestions available message when both lists are empty', () => {
    render(<SuggestedValuesPanel {...baseProps({ rows: [], notUpdated: [] })} />)

    expect(screen.getByText('No suggestions available for this month.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Apply \d+ Suggestions/ })).not.toBeInTheDocument()
  })

  it('shows muted current value next to suggested for a checked overwrite row', () => {
    render(<SuggestedValuesPanel {...baseProps({ rows: [ROW_NONZERO_CURRENT], checkedCount: 1 })} />)

    expect(screen.getByText('5,400.00')).toBeInTheDocument()
  })

  it('apply button disabled at zero checked rows', () => {
    render(<SuggestedValuesPanel {...baseProps({ rows: [ROW_ZERO_CURRENT], checkedCount: 0 })} />)

    expect(screen.getByRole('button', { name: 'Apply 0 Suggestions' })).toBeDisabled()
  })

  it('shows progress text while applying', () => {
    render(
      <SuggestedValuesPanel
        {...baseProps({
          phase: 'applying',
          rows: [ROW_ZERO_CURRENT],
          applyProgress: { current: 2, total: 5, accountName: 'PlatinumVisa8003' },
        })}
      />,
    )

    expect(screen.getByText('Applying 2 of 5: PlatinumVisa8003...')).toBeInTheDocument()
  })

  it('shows a fetch error state with a retry action', () => {
    const onRetryFetch = vi.fn()
    render(<SuggestedValuesPanel {...baseProps({ phase: 'error', fetchError: "Couldn't load suggestions", onRetryFetch })} />)

    expect(screen.getByRole('alert')).toHaveTextContent("Couldn't load suggestions")
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(onRetryFetch).toHaveBeenCalledTimes(1)
  })

  it('shows completion summary with retry failed when a row failed', () => {
    const failed: SuggestionRow = { ...ROW_ZERO_CURRENT, status: 'error' }
    render(
      <SuggestedValuesPanel
        {...baseProps({
          phase: 'completed',
          rows: [failed],
          succeededCount: 0,
          failedRows: [failed],
        })}
      />,
    )

    expect(screen.getByText(/Applied 0 of 1/)).toBeInTheDocument()
    expect(screen.getByText(/1 failed: PlatinumVisa8003/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Retry Failed' })).toBeInTheDocument()
  })
})
