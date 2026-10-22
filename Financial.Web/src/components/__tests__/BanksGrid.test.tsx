import { fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import BanksGrid from '../BanksGrid'
import type { CurrencyTotalDto } from '../../api/types'
import type { BankTotal } from '../../hooks/useMonthly'

const BANK_TOTALS: BankTotal[] = [
  { bankId: 'bank-barclays', bank: 'Barclays', currency: 'GBP', balance: 42.5, roundUpTotal: 0 },
  { bankId: 'bank-trading212', bank: 'Trading212', currency: 'GBP', balance: 8.8, roundUpTotal: 0.6 },
]

const GBP_TOTALS: CurrencyTotalDto[] = [{ currency: 'GBP', balance: 51.3, roundUpTotal: 0.6 }]

function footer() {
  return document.querySelector('.monthly-page__section-total')!.textContent
}

describe('BanksGrid', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders a row per bank with balance and round-up columns, plus footer totals', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    const barclaysRow = screen.getByRole('row', { name: /Barclays/ })
    expect(within(barclaysRow).getByText('42.50')).toBeInTheDocument()

    const trading212Row = screen.getByRole('row', { name: /Trading212/ })
    expect(within(trading212Row).getByText('8.80')).toBeInTheDocument()
    expect(within(trading212Row).getByText('0.60')).toBeInTheDocument()

    expect(screen.getByText('51.30')).toBeInTheDocument()
  })

  it('renders no expand control and no row-level action buttons, only the sort and filter buttons', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    // 3 header sort buttons (Bank, Bank Balance, Round-Up) + 1 filter button (Bank).
    expect(screen.getAllByRole('button')).toHaveLength(4)
  })

  it('filters rows by Bank via the header checklist', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Bank' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Trading212' }))

    // The header's own filter checklist keeps every bank name in the DOM (as checkbox labels)
    // regardless of which rows are filtered out, so assertions must be scoped to tbody.
    const tbody = within(document.querySelector('tbody')!)
    expect(tbody.queryByText('Trading212')).not.toBeInTheDocument()
    expect(tbody.getByText('Barclays')).toBeInTheDocument()
  })

  it('shows the "no rows match" message when every value is unchecked', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Bank' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Barclays' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Trading212' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
  })

  it('renders exactly three data columns in the header', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    expect(screen.getAllByRole('columnheader')).toHaveLength(3)
    expect(screen.getByRole('columnheader', { name: 'Bank' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Bank Balance' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'Round-Up' })).toBeInTheDocument()
  })

  it('sorts rows by Bank Balance ascending when its header is clicked', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    fireEvent.click(screen.getByRole('button', { name: 'Bank Balance' }))

    const dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Trading212')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Barclays')).toBeInTheDocument()
  })

  it('keeps the single-currency footer unlabelled by currency', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    expect(footer()).toBe('Bank Balance: 51.30 · Round-Up: 0.60')
  })

  it('labels each footer total with its currency when the members hold several currencies', () => {
    render(
      <BanksGrid
        bankTotals={BANK_TOTALS}
        currencyTotals={[...GBP_TOTALS, { currency: 'BRL', balance: 640.5, roundUpTotal: 0 }]}
      />,
    )

    expect(footer()).toBe('Bank Balance GBP: 51.30 · Round-Up GBP: 0.60 · Bank Balance BRL: 640.50 · Round-Up BRL: 0.00')
  })

  it('shows zero footer totals when there are no currency totals', () => {
    render(<BanksGrid bankTotals={[]} currencyTotals={[]} />)

    expect(footer()).toBe('Bank Balance: 0.00 · Round-Up: 0.00')
  })

  it('shows its empty message when no account is in the default group', () => {
    render(<BanksGrid bankTotals={[]} currencyTotals={[]} />)

    expect(screen.getByText('No accounts in the default group for this month')).toBeInTheDocument()
  })

  it('keys rows by account Id, so two accounts with the same name render without a key clash', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    render(
      <BanksGrid
        bankTotals={[
          { bankId: 'bank-a', bank: 'Chase', currency: 'GBP', balance: 1, roundUpTotal: 0 },
          { bankId: 'bank-b', bank: 'Chase', currency: 'GBP', balance: 2, roundUpTotal: 0 },
        ]}
        currencyTotals={GBP_TOTALS}
      />,
    )

    expect(screen.getAllByRole('row', { name: /Chase/ })).toHaveLength(2)
    expect(consoleError).not.toHaveBeenCalled()
  })

  it('adds a Currency column when the members hold several currencies', () => {
    render(
      <BanksGrid
        bankTotals={[...BANK_TOTALS, { bankId: 'bank-itau', bank: 'Itaú', currency: 'BRL', balance: 640.5, roundUpTotal: 0 }]}
        currencyTotals={[...GBP_TOTALS, { currency: 'BRL', balance: 640.5, roundUpTotal: 0 }]}
      />,
    )

    expect(screen.getByRole('columnheader', { name: 'Currency' })).toBeInTheDocument()
    expect(within(screen.getByRole('row', { name: /Itaú/ })).getByText('BRL')).toBeInTheDocument()
  })

  it('names the table Bank balances', () => {
    render(<BanksGrid bankTotals={BANK_TOTALS} currencyTotals={GBP_TOTALS} />)

    expect(screen.getByRole('table', { name: 'Bank balances' })).toBeInTheDocument()
  })
})
