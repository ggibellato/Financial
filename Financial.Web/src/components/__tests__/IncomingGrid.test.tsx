import { fireEvent, render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import IncomingGrid from '../IncomingGrid'
import type { IncomeSourceTotalDto } from '../../api/types'

const INCOME_TOTALS: IncomeSourceTotalDto[] = [
  { incomeSourceId: 'source-gleison', name: 'Gleison', grossValue: 3200, netValue: 2450 },
  { incomeSourceId: 'source-lottery', name: 'Lottery', grossValue: null, netValue: 100 },
]

function renderGrid(overrides: Partial<Parameters<typeof IncomingGrid>[0]> = {}) {
  return render(
    <IncomingGrid
      incomeTotals={INCOME_TOTALS}
      totalIncoming={2550}
      {...overrides}
    />,
  )
}

describe('IncomingGrid', () => {
  it('renders a row per income source, using an em dash when gross value is absent', () => {
    renderGrid()

    expect(screen.getByText('Gleison')).toBeInTheDocument()
    expect(screen.getByText('3,200.00')).toBeInTheDocument()
    expect(screen.getByText('Lottery')).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
    expect(screen.getByText(/Total incoming:/)).toBeInTheDocument()
  })

  it('sorts rows by Net ascending when its header is clicked', () => {
    renderGrid()

    fireEvent.click(screen.getByRole('button', { name: 'Net' }))

    const dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Lottery')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Gleison')).toBeInTheDocument()
  })

  it('[AC P61-F03-tithe-tab-and-history-05] shows no tithe lines or carry-forward checkbox', () => {
    renderGrid()

    expect(screen.queryByText(/tithe|carry/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
  })

  it('shows its empty message when no income was recorded in the month', () => {
    renderGrid({ incomeTotals: [], totalIncoming: 0 })

    expect(screen.getByText('No income recorded for this month.')).toBeInTheDocument()
    expect(screen.getByText('0.00')).toBeInTheDocument()
  })
})
