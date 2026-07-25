import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import IncomingGrid from '../IncomingGrid'
import type { IncomeTotal } from '../../hooks/useMonthly'

const INCOME_TOTALS: IncomeTotal[] = [
  { source: 'Gleison', grossValue: 3200, netValue: 2450 },
  { source: 'Lottery', grossValue: null, netValue: 100 },
]

describe('IncomingGrid', () => {
  it('renders a row per income source, using an em dash when gross value is absent', () => {
    render(<IncomingGrid incomeTotals={INCOME_TOTALS} totalIncoming={2550} titheSummary={null} />)

    expect(screen.getByRole('cell', { name: 'Gleison' })).toBeInTheDocument()
    expect(screen.getByText('3,200.00')).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Lottery' })).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
    expect(screen.getByText(/Total Incoming:/)).toBeInTheDocument()
    expect(screen.queryByText(/Calculated Tithe:/)).not.toBeInTheDocument()
  })

  it('shows the calculated tithe and tithe balance when a tithe summary is provided', () => {
    render(
      <IncomingGrid
        incomeTotals={INCOME_TOTALS}
        totalIncoming={2550}
        titheSummary={{ calculatedTithe: 245, titheBalance: 245 }}
      />,
    )

    expect(screen.getByText(/Calculated Tithe:/)).toBeInTheDocument()
    expect(screen.getByText(/Tithe Balance:/)).toBeInTheDocument()
  })
})
