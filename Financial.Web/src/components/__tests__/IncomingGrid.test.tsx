import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import IncomingGrid from '../IncomingGrid'
import type { IncomeTotal } from '../../hooks/useMonthly'

const INCOME_TOTALS: IncomeTotal[] = [
  { source: 'Gleison', grossValue: 3200, netValue: 2450 },
  { source: 'Lottery', grossValue: null, netValue: 100 },
]

function renderGrid(overrides: Partial<Parameters<typeof IncomingGrid>[0]> = {}) {
  return render(
    <IncomingGrid
      incomeTotals={INCOME_TOTALS}
      totalIncoming={2550}
      titheSummary={null}
      carryForwardUpdating={false}
      onToggleCarryForward={vi.fn()}
      {...overrides}
    />,
  )
}

describe('IncomingGrid', () => {
  it('renders a row per income source, using an em dash when gross value is absent', () => {
    renderGrid()

    expect(screen.getByRole('cell', { name: 'Gleison' })).toBeInTheDocument()
    expect(screen.getByText('3,200.00')).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Lottery' })).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
    expect(screen.getByText(/Total Incoming:/)).toBeInTheDocument()
    expect(screen.queryByText(/Calculated Tithe:/)).not.toBeInTheDocument()
  })

  it('shows the calculated tithe and tithe balance when a tithe summary is provided', () => {
    renderGrid({ titheSummary: { calculatedTithe: 245, titheBalance: 245, carryForward: null } })

    expect(screen.getByText(/Calculated Tithe:/)).toBeInTheDocument()
    expect(screen.getByText(/Tithe Balance:/)).toBeInTheDocument()
  })

  it('sorts rows by Net ascending when its header is clicked', () => {
    renderGrid()

    fireEvent.click(screen.getByRole('button', { name: 'Net' }))

    const dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Lottery')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Gleison')).toBeInTheDocument()
  })

  it('does not render a carry-forward control when nothing is available to carry', () => {
    renderGrid({ titheSummary: { calculatedTithe: 245, titheBalance: 245, carryForward: null } })

    expect(screen.queryByText(/Carry forward from/)).not.toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
  })

  it('renders the carry-forward checkbox, pre-checked, with the amount and source month, when available', () => {
    renderGrid({
      titheSummary: {
        calculatedTithe: 200,
        titheBalance: 150,
        carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 8 },
      },
    })

    expect(screen.getByText(/Carry forward from Aug 2026/)).toBeInTheDocument()
    const checkbox = screen.getByRole('checkbox')
    expect(checkbox).toBeChecked()
    expect(screen.getByText('50.00')).toBeInTheDocument()
  })

  it('reflects an excluded carry-forward as unchecked', () => {
    renderGrid({
      titheSummary: {
        calculatedTithe: 200,
        titheBalance: 100,
        carryForward: { amount: 50, included: false, fromYear: 2026, fromMonth: 8 },
      },
    })

    expect(screen.getByRole('checkbox')).not.toBeChecked()
  })

  it('calls onToggleCarryForward with the new checked state when clicked', () => {
    const onToggle = vi.fn()
    renderGrid({
      titheSummary: {
        calculatedTithe: 200,
        titheBalance: 150,
        carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 8 },
      },
      onToggleCarryForward: onToggle,
    })

    fireEvent.click(screen.getByRole('checkbox'))

    expect(onToggle).toHaveBeenCalledWith(false)
  })

  it('disables the checkbox while the toggle is in flight', () => {
    renderGrid({
      titheSummary: {
        calculatedTithe: 200,
        titheBalance: 150,
        carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 8 },
      },
      carryForwardUpdating: true,
    })

    expect(screen.getByRole('checkbox')).toBeDisabled()
  })

  it('shows the action error message when a toggle failed', () => {
    renderGrid({ carryForwardActionError: 'Failed to update carry-forward' })

    expect(screen.getByRole('alert')).toHaveTextContent('Failed to update carry-forward')
  })
})
