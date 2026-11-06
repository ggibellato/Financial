import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import type { AccountTransactionDto, MonthTransactionDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import MonthTransactionsGrid from '../MonthTransactionsGrid'

interface AccountFields {
  accountId: string
  accountName: string
  accountCurrency: string
  accountIsActive: boolean
}

const BARCLAYS: AccountFields = { accountId: 'bank-1', accountName: 'Barclays', accountCurrency: 'GBP', accountIsActive: true }
const RESERVA: AccountFields = { accountId: 'pots-1', accountName: 'Reserva', accountCurrency: 'GBP', accountIsActive: true }
const AMEX: AccountFields = { accountId: 'card-1', accountName: 'BA Amex', accountCurrency: 'GBP', accountIsActive: true }
const ITAU: AccountFields = { accountId: 'bank-9', accountName: 'Itau', accountCurrency: 'BRL', accountIsActive: false }

const DEPOSIT = { operationId: 'o1', potsAccountId: 'pots-1', year: 2026, month: 10, incomeId: null }

function monthRow(account: AccountFields, transaction: AccountTransactionDto): MonthTransactionDto {
  return { ...account, transaction }
}

const ROWS: MonthTransactionDto[] = [
  monthRow(AMEX, accountTransactionRow({ id: 'c2', kind: 'CardCharge', date: '2026-09-28', description: 'Amazon', categoryName: 'Clothing', amount: -39.99 })),
  monthRow(BARCLAYS, accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-28', description: 'Tesco', categoryName: 'Groceries', amount: -45.2, roundUp: -0.8 })),
  monthRow(BARCLAYS, accountTransactionRow({ id: 'e9', kind: 'Expense', date: '2026-10-10', description: 'Deposit to Reserva', categoryName: 'Reserva', amount: -90, operation: DEPOSIT })),
  monthRow(RESERVA, accountTransactionRow({ id: 'p3', kind: 'PotTransaction', date: '2026-10-10', description: '', counterpart: 'Casa', amount: 90, operation: DEPOSIT })),
  monthRow(BARCLAYS, accountTransactionRow({ id: 'cp1', kind: 'CardPayment', date: '2026-10-05', description: 'BA Amex payment', amount: -100 })),
  monthRow(AMEX, accountTransactionRow({ id: 'cp1', kind: 'CardPayment', date: '2026-10-05', description: 'BA Amex payment', amount: 100 })),
  monthRow(ITAU, accountTransactionRow({ id: 'x1', kind: 'Expense', date: '2026-10-03', description: 'Padaria', categoryName: 'Groceries', amount: -12.5 })),
]

interface GridOptions {
  rows?: MonthTransactionDto[]
  isLoading?: boolean
  error?: string | null
  onRetry?: () => void
}

function renderGrid({ rows = ROWS, isLoading = false, error = null, onRetry = vi.fn() }: GridOptions = {}) {
  return render(<MonthTransactionsGrid year={2026} month={10} rows={rows} isLoading={isLoading} error={error} onRetry={onRetry} />, {
    wrapper: MemoryRouter,
  })
}

function bodyRows() {
  const [, ...rows] = within(screen.getByRole('table', { name: 'All transactions' })).getAllByRole('row')
  return rows
}

function descriptions() {
  return bodyRows().map((row) => within(row).getAllByRole('cell')[3].textContent)
}

const FOOTER = /rows?\b/

function footer() {
  return screen.getByText(FOOTER, { selector: 'p' })
}

function queryFooter() {
  return screen.queryByText(FOOTER, { selector: 'p' })
}

function filterToNone(label: string) {
  fireEvent.click(screen.getByRole('button', { name: `Filter by ${label}` }))
  fireEvent.click(screen.getByRole('checkbox', { name: '(All)' }))
}

function filterTo(label: string, value: string) {
  filterToNone(label)
  fireEvent.click(screen.getByRole('checkbox', { name: value }))
}

describe('MonthTransactionsGrid', () => {
  it("[AC P60-F16-month-transactions-view-01] lists every account's rows with account, type, category and signed amount", () => {
    renderGrid()

    const headers = screen.getAllByRole('columnheader').map((header) => within(header).getAllByRole('button')[0].textContent)
    expect(headers.slice(0, 6)).toEqual(['Date', 'Account', 'Type', 'Description', 'Category', 'Amount'])
    expect(bodyRows()).toHaveLength(7)
    const [tesco] = bodyRows()
    expect(within(tesco).getAllByRole('cell').map((cell) => cell.textContent)).toEqual([
      'Date:28/10/2026',
      'Account:Barclays',
      'Type:Expense',
      'Description:Tesco',
      'Category:Groceries',
      'Amount:-45.20 GBP',
    ])
    expect(within(tesco).getAllByRole('cell')[5]).toHaveClass('data-table__col--numeric')
  })

  it('orders newest first and sorts by any column', () => {
    renderGrid()

    expect(descriptions()[0]).toBe('Description:Tesco')
    expect(descriptions().at(-1)).toBe('Description:Amazon')

    fireEvent.click(screen.getByRole('button', { name: 'Amount' }))

    expect(descriptions()[0]).toBe('Description:BA Amex payment')
    expect(within(bodyRows()[0]).getAllByRole('cell')[5]).toHaveTextContent('-100.00 GBP')
  })

  it('shows the card purchase date on a charge listed in its statement month', () => {
    renderGrid()

    const amazon = bodyRows().find((row) => within(row).queryByText('Amazon'))!
    expect(within(amazon).getAllByRole('cell')[0]).toHaveTextContent('28/09/2026')
  })

  it('marks an inactive account with an Inactive badge', () => {
    renderGrid()

    const padaria = bodyRows().find((row) => within(row).queryByText('Padaria'))!
    expect(within(padaria).getByText('Inactive')).toBeInTheDocument()
    const tesco = bodyRows().find((row) => within(row).queryByText('Tesco'))!
    expect(within(tesco).queryByText('Inactive')).not.toBeInTheDocument()
  })

  it('renders both sides of a card payment', () => {
    renderGrid()

    const payments = bodyRows().filter((row) => within(row).queryByText('Card payment'))
    expect(payments.map((row) => within(row).getAllByRole('cell')[1].textContent)).toEqual(['Account:Barclays', 'Account:BA Amex'])
  })

  it('[AC P60-F16-month-transactions-view-02] filters by category, account and type, with count, total per currency and Spend', () => {
    renderGrid()

    expect(footer()).toHaveTextContent('7 rows · Total -12.50 BRL · Total -85.19 GBP')
    expect(footer()).not.toHaveTextContent('Spend')

    filterTo('Category', 'Groceries')

    expect(descriptions()).toEqual(['Description:Tesco', 'Description:Padaria'])
    expect(footer()).toHaveTextContent('2 of 7 rows · Total -12.50 BRL · Total -45.20 GBP · Spend 12.50 BRL · Spend 45.20 GBP')

    filterTo('Account', 'Barclays')

    expect(descriptions()).toEqual(['Description:Tesco'])
    expect(footer()).toHaveTextContent('1 of 7 rows · Total -45.20 GBP · Spend 45.20 GBP')
  })

  it('narrows by type without showing Spend', () => {
    renderGrid()

    filterTo('Type', 'Card payment')

    expect(bodyRows()).toHaveLength(2)
    expect(footer()).toHaveTextContent('2 of 7 rows · Total 0.00 GBP')
    expect(footer()).not.toHaveTextContent('Spend')
  })

  it('drops rows without a category when a category is filtered', () => {
    renderGrid()

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Reserva' }))

    expect(descriptions()).toEqual(['Description:Tesco', 'Description:Padaria', 'Description:Amazon'])
  })

  it('shows Clear filters only while filtered, clears every filter and focuses the Category filter', () => {
    renderGrid()
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()

    filterTo('Account', 'Itau')
    filterTo('Type', 'Expense')

    fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }))

    expect(bodyRows()).toHaveLength(7)
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Filter by Category' })).toHaveFocus()
  })

  it('[AC P60-F16-month-transactions-view-03] links rows to the account screen and operation rows to their operation', () => {
    renderGrid()

    expect(within(bodyRows()[0]).getByRole('link', { name: 'Barclays' })).toHaveAttribute('href', '/cashflow/accounts/bank-1?month=2026-10&tab=transactions')
    const amazon = bodyRows().find((row) => within(row).queryByText('Amazon'))!
    expect(within(amazon).getByRole('link', { name: 'BA Amex' })).toHaveAttribute('href', '/cashflow/accounts/card-1?month=2026-10&tab=transactions')
    expect(screen.getByRole('link', { name: 'Deposit to Reserva' })).toHaveAttribute(
      'href',
      '/cashflow/accounts/pots-1?month=2026-10&tab=transactions&operation=o1',
    )
    expect(screen.getByRole('link', { name: 'Deposit from an income' })).toHaveAttribute(
      'href',
      '/cashflow/accounts/pots-1?month=2026-10&tab=transactions&operation=o1',
    )
    const payments = bodyRows().filter((row) => within(row).queryByText('Card payment'))
    expect(payments.map((row) => within(row).getAllByRole('link'))).toEqual([[expect.anything()], [expect.anything()]])
  })

  it('[AC P60-F16-month-transactions-view-04] shows a skeleton under the headers while loading, with no footer', () => {
    renderGrid({ rows: [], isLoading: true })

    expect(screen.getByRole('button', { name: 'Date' })).toBeInTheDocument()
    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
    expect(queryFooter()).not.toBeInTheDocument()
  })

  it('[AC P60-F16-month-transactions-view-04] shows the empty month message with no footer and nothing to clear', () => {
    renderGrid({ rows: [] })

    expect(screen.getByText('No transactions in October 2026')).toBeInTheDocument()
    expect(queryFooter()).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
  })

  it('[AC P60-F16-month-transactions-view-04] shows the no-match message with Clear filters above the grid', () => {
    renderGrid()

    filterToNone('Account')

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
    expect(queryFooter()).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Clear filters' })).toBeInTheDocument()
  })

  it('[AC P60-F16-month-transactions-view-04] shows the load error with Try again calling retry', () => {
    const onRetry = vi.fn()
    renderGrid({ rows: [], error: 'Unable to load the transactions', onRetry })

    expect(screen.getByRole('alert')).toHaveTextContent('Unable to load the transactions')
    expect(screen.queryByRole('table')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(onRetry).toHaveBeenCalledTimes(1)
  })

  it('reads a single row in the singular', () => {
    renderGrid({ rows: [ROWS[1]] })

    expect(footer()).toHaveTextContent('1 row · Total -45.20 GBP')
  })
})
