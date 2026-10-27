import { describe, expect, it, vi } from 'vitest'
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

const NO_ROW_ACTIONS = { onEdit: vi.fn(), onDelete: vi.fn() }

function bodyRows() {
  const [, ...rows] = screen.getAllByRole('row')
  return rows
}

describe('AccountTransactionsGrid', () => {
  it('[AC P60-F05-bank-account-screen-04] shows signed, right-aligned amounts and a separate round-up column', () => {
    render(<AccountTransactionsGrid transactions={ROWS} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    const expenseRow = bodyRows()[0]
    const amountCell = within(expenseRow).getByText('-42.10').closest('td')
    const roundUpCell = within(expenseRow).getByText('-0.90').closest('td')
    expect(amountCell).toHaveClass('data-table__col--numeric')
    expect(roundUpCell).toHaveClass('data-table__col--numeric')
    expect(within(bodyRows()[1]).getByText('+2,500.00').closest('td')).toHaveClass('data-table__col--numeric')
    expect(screen.getByRole('columnheader', { name: 'Amount' })).toHaveClass('data-table__col--numeric')
  })

  it('labels each kind and shows the date, category and counterpart', () => {
    render(<AccountTransactionsGrid transactions={ROWS} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(bodyRows().map((row) => within(row).getAllByRole('cell')[2].textContent)).toEqual([
      'Type:Expense',
      'Type:Income',
      'Type:Card charge',
      'Type:Transfer out',
      'Type:Balance adjustment',
    ])
    const cardRow = bodyRows()[2]
    expect(within(cardRow).getByText('10/10/2026')).toBeInTheDocument()
    expect(within(cardRow).getByText('Home')).toBeInTheDocument()
    expect(within(cardRow).getByText('Amex')).toBeInTheDocument()
  })

  it('lists the newest rows first and sorts by a column on request', () => {
    render(<AccountTransactionsGrid transactions={[...ROWS].reverse()} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(within(bodyRows()[0]).getByText('14/10/2026')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Amount' }))

    expect(within(bodyRows()[0]).getByText('-200.00')).toBeInTheDocument()
    expect(within(bodyRows()[4]).getByText('+2,500.00')).toBeInTheDocument()
  })

  it('shows the empty message when the month has no rows', () => {
    render(<AccountTransactionsGrid transactions={[]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(screen.getByText(EMPTY_MESSAGE)).toBeInTheDocument()
  })

  it('shows a skeleton instead of data while loading', () => {
    render(<AccountTransactionsGrid transactions={ROWS} isLoading emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
    expect(screen.queryByText('Tesco')).not.toBeInTheDocument()
  })

  it('puts Edit and Delete in a trailing cell of editable rows only', () => {
    const onEdit = vi.fn()
    const onDelete = vi.fn()
    render(<AccountTransactionsGrid transactions={ROWS} emptyMessage={EMPTY_MESSAGE} onEdit={onEdit} onDelete={onDelete} />)

    const expenseCells = within(bodyRows()[0]).getAllByRole('cell')
    const editExpense = within(expenseCells[expenseCells.length - 1]).getByRole('button', { name: 'Edit expense' })
    fireEvent.click(editExpense)
    fireEvent.click(within(bodyRows()[3]).getByRole('button', { name: 'Delete transfer' }))

    expect(onEdit).toHaveBeenCalledWith(ROWS[0], editExpense)
    expect(onDelete).toHaveBeenCalledWith(ROWS[3])
    expect(within(bodyRows()[1]).getByRole('button', { name: 'Edit income' })).toBeInTheDocument()
    expect(within(bodyRows()[4]).getByRole('button', { name: 'Delete balance adjustment' })).toBeInTheDocument()
    expect(within(bodyRows()[2]).queryByRole('button')).not.toBeInTheDocument()
  })

  it('disables Edit, but not Delete, while editing is unavailable', () => {
    render(<AccountTransactionsGrid transactions={ROWS} emptyMessage={EMPTY_MESSAGE} isEditDisabled {...NO_ROW_ACTIONS} />)

    expect(within(bodyRows()[0]).getByRole('button', { name: 'Edit expense' })).toBeDisabled()
    expect(within(bodyRows()[0]).getByRole('button', { name: 'Delete expense' })).toBeEnabled()
  })

  it('labels the date column as asked and card payments by their kind', () => {
    render(
      <AccountTransactionsGrid
        transactions={[accountTransactionRow({ id: 'p1', kind: 'CardPayment', date: '2026-10-12', counterpart: 'Barclays', amount: 480, canEdit: false })]}
        emptyMessage={EMPTY_MESSAGE}
        dateHeader="Purchased"
        {...NO_ROW_ACTIONS}
      />,
    )

    expect(screen.getByRole('columnheader', { name: /^Purchased/ })).toBeInTheDocument()
    expect(screen.getByText('Card payment')).toBeInTheDocument()
    expect(within(bodyRows()[0]).queryByRole('button')).not.toBeInTheDocument()
  })

  it('offers no row actions on an editable card charge until card charge editing exists', () => {
    render(
      <AccountTransactionsGrid
        transactions={[accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-10-10', description: 'Taxi', amount: -30, canEdit: true })]}
        emptyMessage={EMPTY_MESSAGE}
        {...NO_ROW_ACTIONS}
      />,
    )

    expect(within(bodyRows()[0]).queryByRole('button')).not.toBeInTheDocument()
  })
})
