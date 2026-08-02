import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import BanksGrid from '../BanksGrid'
import type { BankTotal } from '../../hooks/useMonthly'

const BANK_TOTALS: BankTotal[] = [
  { bank: 'Barclays', balance: 42.5, roundUpTotal: 0 },
  { bank: 'Trading212', balance: 8.8, roundUpTotal: 0.6 },
]

describe('BanksGrid', () => {
  it('renders a row per bank with balance and round-up columns, plus footer totals', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} bankTotalsSum={51.3} roundUpTotalsSum={0.6} />)

    const barclaysRow = screen.getByRole('row', { name: /Barclays/ })
    expect(within(barclaysRow).getByRole('cell', { name: '42.50' })).toBeInTheDocument()

    const trading212Row = screen.getByRole('row', { name: /Trading212/ })
    expect(within(trading212Row).getByRole('cell', { name: '8.80' })).toBeInTheDocument()
    expect(within(trading212Row).getByRole('cell', { name: '0.60' })).toBeInTheDocument()

    expect(screen.getByText('51.30')).toBeInTheDocument()
  })

  it('renders no expand control and no action buttons', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} bankTotalsSum={51.3} roundUpTotalsSum={0.6} />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders exactly three data columns in the header', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} bankTotalsSum={51.3} roundUpTotalsSum={0.6} />)

    expect(screen.getAllByRole('columnheader')).toHaveLength(3)
    expect(screen.getByRole('columnheader', { name: 'Bank' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Bank Balance' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Round-Up' })).toBeInTheDocument()
  })
})
