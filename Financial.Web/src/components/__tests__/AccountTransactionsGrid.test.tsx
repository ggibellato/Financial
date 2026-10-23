import { describe, expect, it } from 'vitest'
import type { AccountTransactionDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import AccountTransactionsGrid from '../AccountTransactionsGrid'
import { accountTransactionRow } from '../../test/accountTransactionRow'

const ROWS: AccountTransactionDto[] = [
  accountTransactionRow({
    id: 'e1',
    kind: 'Expense',
    date: '2026-10-14',
    description: 'Tesco',
    categoryName: 'Groceries',
    amount: -42.1,
    roundUp: -0.9,
    canEdit: true,
  }),
  accountTransactionRow({ id: 'i1', kind: 'Income', date: '2026-10-12', description: 'October pay', counterpart: 'Salary', amount: 2500, canEdit: true }),
  accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-10-10', description: 'Amazon', categoryName: 'Home', counterpart: 'Amex', amount: -60, canEdit: false }),
  accountTransactionRow({ id: 't1', kind: 'TransferOut', date: '2026-10-05', counterpart: 'Chase', amount: -200, canEdit: true }),
  accountTransactionRow({ id: 'a1', kind: 'Adjustment', date: '2026-10-01', description: 'Bank fee', amount: -1.5, canEdit: true }),
]

const EMPTY_MESSAGE = 'No transactions in October 2026'

function bodyRows() {
  const [, ...rows] = screen.getAllByRole('row')
  return rows
}

describe('AccountTransactionsGrid', () => {
  it('[AC P60-F05-bank-account-screen-04] shows signed, right-aligned amounts and a separate round-up column', () => {
    render(<AccountTransactionsGrid transactions={ROWS} emptyMessage={EMPTY_MESSAGE} />)

    const expenseRow = bodyRows()[0]
    const amountCell = within(expenseRow).getByText('-42.10').closest('td')
    const roundUpCell = within(expenseRow).getByText('-0.90').closest('td')
    expect(amountCell).toHaveClass('data-table__col--numeric')
    expect(roundUpCell).toHaveClass('data-table__col--numeric')
    expect(within(bodyRows()[1]).getByText('+2,500.00').closest('td')).toHaveClass('data-table__col--numeric')
    expect(screen.getByRole('columnheader', { name: 'Amount' })).toHaveClass('data-table__col--numeric')
  })

  it('labels each kind and shows the date, category and counterpart', () => {
    render(<AccountTransactionsGrid transactions={ROWS} emptyMessage={EMPTY_MESSAGE} />)

    expect(bodyRows().map((row) => within(row).getAllByRole('cell')[2].textContent)).toEqual([
      'Type:Expense',
      'Type:Income',
      'Type:Card charge',
      'Type:Transfer out',
      'Type:Adjustment',
    ])
    const cardRow = bodyRows()[2]
    expect(within(cardRow).getByText('10/10/2026')).toBeInTheDocument()
    expect(within(cardRow).getByText('Home')).toBeInTheDocument()
    expect(within(cardRow).getByText('Amex')).toBeInTheDocument()
  })

  it('lists the newest rows first and sorts by a column on request', () => {
    render(<AccountTransactionsGrid transactions={[...ROWS].reverse()} emptyMessage={EMPTY_MESSAGE} />)

    expect(within(bodyRows()[0]).getByText('14/10/2026')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Amount' }))

    expect(within(bodyRows()[0]).getByText('-200.00')).toBeInTheDocument()
    expect(within(bodyRows()[4]).getByText('+2,500.00')).toBeInTheDocument()
  })

  it('shows the empty message when the month has no rows', () => {
    render(<AccountTransactionsGrid transactions={[]} emptyMessage={EMPTY_MESSAGE} />)

    expect(screen.getByText(EMPTY_MESSAGE)).toBeInTheDocument()
  })

  it('shows a skeleton instead of data while loading', () => {
    render(<AccountTransactionsGrid transactions={ROWS} isLoading emptyMessage={EMPTY_MESSAGE} />)

    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
    expect(screen.queryByText('Tesco')).not.toBeInTheDocument()
  })
})
