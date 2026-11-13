import { Toaster } from '@fluentui/react-components'
import { MemoryRouter, useLocation, useNavigationType } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountTransactionDto, ExpenseDto, MonthTransactionDto } from '../../api/types'
import { APP_TOASTER_ID } from '../../hooks/useSuccessToast'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { mockMatchMedia } from '../../test/mockMatchMedia'
import { requestDelete } from '../../test-utils/confirmDelete'
import { never } from '../../test-utils/deferred'
import MonthTransactionsGrid from '../MonthTransactionsGrid'

const { deleteExpenseMock, deletePotOperationMock } = vi.hoisted(() => ({
  deleteExpenseMock: vi.fn<FinancialApiClient['deleteExpense']>(),
  deletePotOperationMock: vi.fn<FinancialApiClient['deletePotOperation']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { deleteExpense: deleteExpenseMock, deletePotOperation: deletePotOperationMock } as Partial<FinancialApiClient>,
}))

type AccountFields = Omit<MonthTransactionDto, 'transaction'>

const BARCLAYS: AccountFields = { accountId: 'bank-1', accountName: 'Barclays', accountCurrency: 'GBP', accountIsActive: true }
const RESERVA: AccountFields = { accountId: 'pots-1', accountName: 'Reserva', accountCurrency: 'GBP', accountIsActive: true }
const AMEX: AccountFields = { accountId: 'card-1', accountName: 'BA Amex', accountCurrency: 'GBP', accountIsActive: true }
const ITAU: AccountFields = { accountId: 'bank-9', accountName: 'Itau', accountCurrency: 'BRL', accountIsActive: false }

const DEPOSIT = { operationId: 'o1', kind: 'Deposit', potsAccountId: 'pots-1', year: 2026, month: 10, incomeId: null }
const PAID_STATEMENT = { accountId: 'card-1', year: 2026, month: 9 }
const TESCO = { id: 'e1' } as ExpenseDto

function monthRow(account: AccountFields, transaction: AccountTransactionDto): MonthTransactionDto {
  return { ...account, transaction }
}

const ROWS: MonthTransactionDto[] = [
  monthRow(AMEX, accountTransactionRow({ id: 'c2', kind: 'CardCharge', date: '2026-09-28', description: 'Amazon', categoryName: 'Clothing', amount: -39.99 })),
  monthRow(BARCLAYS, accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-28', description: 'Tesco', categoryName: 'Groceries', amount: -45.2, roundUp: -0.8, expense: TESCO })),
  monthRow(BARCLAYS, accountTransactionRow({ id: 'e9', kind: 'Expense', date: '2026-10-10', description: 'Deposit to Reserva', categoryName: 'Reserva', amount: -90, canEdit: false, operation: DEPOSIT })),
  monthRow(RESERVA, accountTransactionRow({ id: 'p3', kind: 'PotTransaction', date: '2026-10-10', description: '', counterpart: 'Casa', amount: 90, canEdit: false, operation: DEPOSIT })),
  monthRow(BARCLAYS, accountTransactionRow({ id: 'cp1', kind: 'CardPayment', date: '2026-10-05', description: 'BA Amex payment', amount: -100, canEdit: false, statement: PAID_STATEMENT })),
  monthRow(AMEX, accountTransactionRow({ id: 'cp1', kind: 'CardPayment', date: '2026-10-05', description: 'BA Amex payment', amount: 100, canEdit: false, statement: PAID_STATEMENT })),
  monthRow(ITAU, accountTransactionRow({ id: 'x1', kind: 'Expense', date: '2026-10-03', description: 'Padaria', categoryName: 'Groceries', amount: -12.5, currency: 'BRL', canEdit: false })),
]

interface GridOptions {
  rows?: MonthTransactionDto[]
  isLoading?: boolean
  error?: string | null
  onRetry?: () => void
  onDeleted?: () => void
  address?: string
}

function LocationText() {
  const { pathname, search } = useLocation()
  const navigationType = useNavigationType()
  return (
    <>
      <p>{`at:${pathname}${search}`}</p>
      <p>{`navigation:${navigationType}`}</p>
    </>
  )
}

const GRID_ADDRESS = '/cashflow/accounts?month=2026-10&tab=transactions'

function renderGrid({ rows = ROWS, isLoading = false, error = null, onRetry = vi.fn(), onDeleted = vi.fn(), address = GRID_ADDRESS }: GridOptions = {}) {
  return render(
    <MemoryRouter initialEntries={[address]}>
      <MonthTransactionsGrid year={2026} month={10} rows={rows} isLoading={isLoading} error={error} onRetry={onRetry} onDeleted={onDeleted} />
      <Toaster toasterId={APP_TOASTER_ID} />
      <LocationText />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  deleteExpenseMock.mockReset()
  deletePotOperationMock.mockReset()
})

function bodyRows() {
  const [, ...rows] = within(screen.getByRole('table', { name: 'All transactions' })).getAllByRole('row')
  return rows
}

function descriptions() {
  return bodyRows().map((row) => within(row).getAllByRole('cell')[3].textContent)
}

const FOOTER = { name: 'Totals for All transactions' }

function footer() {
  return screen.getByRole('group', FOOTER)
}

function queryFooter() {
  return screen.queryByRole('group', FOOTER)
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

    const headers = screen.getAllByRole('columnheader').map((header) => within(header).queryAllByRole('button')[0]?.textContent ?? header.textContent)
    expect(headers).toEqual(['Date', 'Account', 'Type', 'Description', 'Category', 'Amount', ''])
    expect(bodyRows()).toHaveLength(7)
    const [tesco] = bodyRows()
    expect(within(tesco).getAllByRole('cell').map((cell) => cell.textContent)).toEqual([
      'Date:28/10/2026',
      'Account:Barclays',
      'Type:Expense',
      'Description:Tesco',
      'Category:Groceries',
      'Amount:-45.20 GBP',
      '',
    ])
    expect(within(tesco).getAllByRole('cell')[5]).toHaveClass('data-table__col--numeric')
  })

  describe('settlement entries', () => {
    const MAE: AccountFields = { accountId: 'mae-1', accountName: 'Mãe', accountCurrency: 'BRL', accountIsActive: true }
    const ENTRY_ROWS: MonthTransactionDto[] = [
      monthRow(MAE, accountTransactionRow({ id: 's1', kind: 'SettlementEntry', date: '2026-10-11', description: 'Taxa', amount: 80, currency: 'GBP' })),
      monthRow(MAE, accountTransactionRow({ id: 's2', kind: 'SettlementEntry', date: '2026-10-10', description: 'Farmácia', amount: -150, currency: 'BRL' })),
      monthRow(BARCLAYS, accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-09', description: 'Tesco', categoryName: 'Groceries', amount: -45.2 })),
      monthRow(BARCLAYS, accountTransactionRow({ id: 'e2', kind: 'Expense', date: '2026-10-08', description: 'Bus', categoryName: 'Transport', amount: -3 })),
    ]

    it('[AC P60-F12-settlement-screen-03] shows each entry in its own currency and totals the footer per row currency', () => {
      renderGrid({ rows: ENTRY_ROWS })

      const [taxa] = bodyRows()
      expect(within(taxa).getAllByRole('cell').map((cell) => cell.textContent)).toEqual([
        'Date:11/10/2026',
        'Account:Mãe',
        'Type:Settlement entry',
        'Description:Taxa',
        'Category:',
        'Amount:+80.00 GBP',
        '',
      ])
      expect(footer()).toHaveTextContent('4 rows · Total -150.00 BRL · Total +31.80 GBP')
    })

    it('[AC P60-F12-settlement-screen-03] filters to entries by the Settlement entry type', () => {
      renderGrid({ rows: ENTRY_ROWS })

      filterTo('Type', 'Settlement entry')

      expect(descriptions()).toEqual(['Description:Taxa', 'Description:Farmácia'])
    })

    it('leaves entries out of a category filter and its Spend', () => {
      renderGrid({ rows: ENTRY_ROWS })

      filterTo('Category', 'Groceries')

      expect(descriptions()).toEqual(['Description:Tesco'])
      expect(footer()).toHaveTextContent('Spend 45.20 GBP')
      expect(footer()).not.toHaveTextContent('BRL')
    })
  })

  describe('incomes with no bank', () => {
    const NO_BANK: AccountFields = { accountId: null, accountName: null, accountCurrency: null, accountIsActive: true }
    const BANKLESS_ROWS: MonthTransactionDto[] = [
      ...ROWS,
      monthRow(NO_BANK, accountTransactionRow({ id: 'i1', kind: 'Income', date: '2026-10-15', description: 'XP dividends', counterpart: 'DividendoJuros', amount: 85.2, currency: null })),
      monthRow(NO_BANK, accountTransactionRow({ id: 'i2', kind: 'Income', date: '2026-10-30', description: 'Interest', counterpart: 'DividendoJuros', amount: 42.1, currency: null })),
    ]

    it('[AC P61-F02-income-tab-and-bankless-income-07] shows a bankless income as "No bank" plain text, its description unlinked and no currency', () => {
      renderGrid({ rows: BANKLESS_ROWS })

      const dividends = bodyRows().find((row) => within(row).queryByText('XP dividends'))!
      expect(within(dividends).getAllByRole('cell').map((cell) => cell.textContent)).toEqual([
        'Date:15/10/2026',
        'Account:No bank',
        'Type:Income',
        'Description:XP dividends',
        'Category:',
        'Amount:+85.20',
        '',
      ])
      expect(within(dividends).queryByRole('link')).not.toBeInTheDocument()
      expect(within(bodyRows()[0]).queryByRole('link')).not.toBeInTheDocument()
    })

    it('[AC P61-F02-income-tab-and-bankless-income-07] filters by the "No bank" account with its own footer total', () => {
      renderGrid({ rows: BANKLESS_ROWS })

      filterTo('Account', 'No bank')

      expect(descriptions()).toEqual(['Description:Interest', 'Description:XP dividends'])
      expect(footer()).toHaveTextContent('2 of 9 rows · Total (no bank) +127.30')
    })

    it('[AC P61-F02-income-tab-and-bankless-income-07] lists the "No bank" total after the currency totals, leaving Spend unchanged', () => {
      renderGrid({ rows: BANKLESS_ROWS })

      expect(footer()).toHaveTextContent('9 rows · Total -12.50 BRL · Total -85.19 GBP · Total (no bank) +127.30')

      filterTo('Category', 'Groceries')

      expect(footer()).toHaveTextContent('2 of 9 rows · Total -12.50 BRL · Total -45.20 GBP · Spend 12.50 BRL · Spend 45.20 GBP')
      expect(footer()).not.toHaveTextContent('(no bank)')
    })
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

  it('keeps the column filters in the address, replacing the entry and leaving the month and tab alone', () => {
    renderGrid()

    filterTo('Account', 'Itau')
    filterTo('Type', 'Expense')

    expect(screen.getByText(`at:${GRID_ADDRESS}&account=Itau&kind=Expense`)).toBeInTheDocument()
    expect(screen.getByText('navigation:REPLACE')).toBeInTheDocument()
  })

  it('restores the column filters from the address', () => {
    renderGrid({ address: `${GRID_ADDRESS}&kind=Card+payment` })

    expect(bodyRows()).toHaveLength(2)
    expect(footer()).toHaveTextContent('2 of 7 rows')
    expect(screen.getByRole('button', { name: 'Clear filters' })).toBeInTheDocument()
  })

  it('restores a column with every value unchecked as no rows', () => {
    renderGrid({ address: `${GRID_ADDRESS}&category=` })

    expect(screen.getByText('No rows match the current filters.')).toBeInTheDocument()
  })

  it('takes the filters out of the address on Clear filters', () => {
    renderGrid({ address: `${GRID_ADDRESS}&account=Itau&kind=Expense` })

    fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }))

    expect(screen.getByText(`at:${GRID_ADDRESS}`)).toBeInTheDocument()
    expect(bodyRows()).toHaveLength(7)
  })

  it('[AC P60-F16-month-transactions-view-03] links the account and keeps every description plain text', () => {
    renderGrid()

    expect(within(bodyRows()[0]).getByRole('link', { name: 'Barclays' })).toHaveAttribute('href', '/cashflow/accounts/bank-1?month=2026-10&tab=transactions')
    const amazon = bodyRows().find((row) => within(row).queryByText('Amazon'))!
    expect(within(amazon).getByRole('link', { name: 'BA Amex' })).toHaveAttribute('href', '/cashflow/accounts/card-1?month=2026-10&tab=transactions')
    expect(screen.queryByRole('link', { name: 'Deposit to Reserva' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Deposit from an income' })).not.toBeInTheDocument()
    expect(screen.getByText('Deposit from an income')).toBeInTheDocument()
    const payments = bodyRows().filter((row) => within(row).queryByText('Card payment'))
    expect(payments.map((row) => within(row).getAllByRole('link'))).toEqual([[expect.anything()], [expect.anything()]])
  })

  describe('row actions', () => {
    it('names each Edit by kind, description, date, account and category, so same-day rows differ', () => {
      renderGrid()

      const names = screen.getAllByRole('button', { name: /^Edit / }).map((button) => button.getAttribute('aria-label'))
      expect(names).toContain('Edit card payment BA Amex payment, 05/10/2026, Barclays')
      expect(names).toContain('Edit card payment BA Amex payment, 05/10/2026, BA Amex')
      expect(names).toContain('Edit deposit Deposit from an income, Casa, 10/10/2026, Reserva')
      expect(new Set(names).size).toBe(ROWS.length)
    })

    it('tells the lines of a split expense apart by their category', () => {
      const line = (id: string, categoryName: string) =>
        monthRow(BARCLAYS, accountTransactionRow({ id, kind: 'Expense', date: '2026-10-24', description: 'Toby Carvery', categoryName, amount: -7 }))
      renderGrid({ rows: [line('s1', 'Extras'), line('s2', 'Reserva')] })

      expect(screen.getByRole('button', { name: 'Edit expense Toby Carvery, 24/10/2026, Barclays, Extras' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Edit expense Toby Carvery, 24/10/2026, Barclays, Reserva' })).toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: 'Actions' })).toBeInTheDocument()
    })

    it('opens an account row on its account screen with its edit form', async () => {
      renderGrid()

      fireEvent.click(screen.getByRole('button', { name: 'Edit expense Tesco, 28/10/2026, Barclays, Groceries' }))

      expect(await screen.findByText('at:/cashflow/accounts/bank-1?month=2026-10&tab=transactions&edit=Expense-e1')).toBeInTheDocument()
    })

    it('[AC P60-F16-month-transactions-view-03] opens a pot operation where it lives and a statement payment on its Statements tab', async () => {
      renderGrid()

      fireEvent.click(screen.getByRole('button', { name: 'Edit deposit Deposit to Reserva, 10/10/2026, Barclays, Reserva' }))
      expect(await screen.findByText('at:/cashflow/accounts/pots-1?month=2026-10&tab=transactions&operation=o1')).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: 'Edit card payment BA Amex payment, 05/10/2026, Barclays' }))
      expect(await screen.findByText('at:/cashflow/accounts/card-1?month=2026-09&tab=statements')).toBeInTheDocument()
    })

    it('opens a bankless income on the Income tab', async () => {
      const income = monthRow(
        { accountId: null, accountName: null, accountCurrency: null, accountIsActive: true },
        accountTransactionRow({ id: 'i1', kind: 'Income', date: '2026-10-15', description: 'XP dividends', amount: 85.2, currency: null }),
      )
      renderGrid({ rows: [income] })

      fireEvent.click(screen.getByRole('button', { name: 'Edit income XP dividends, 15/10/2026, No bank' }))

      expect(await screen.findByText('at:/cashflow/accounts?month=2026-10&tab=income&edit=i1')).toBeInTheDocument()
    })

    it('says where to change rows it cannot change here', () => {
      renderGrid()

      const bankDeposit = screen.getByRole('button', { name: 'Delete deposit Deposit to Reserva, 10/10/2026, Barclays, Reserva' })
      expect(bankDeposit).toHaveAttribute('aria-disabled', 'true')
      expect(bankDeposit).toHaveAccessibleDescription('Delete it from the pots account.')
      expect(screen.getByRole('button', { name: 'Delete card payment BA Amex payment, 05/10/2026, Barclays' })).toHaveAccessibleDescription(
        'Unmark the statement to remove this payment.',
      )
      const inactiveEdit = screen.getByRole('button', { name: 'Edit expense Padaria, 03/10/2026, Itau, Groceries' })
      expect(inactiveEdit).toHaveAttribute('aria-disabled', 'true')
      expect(inactiveEdit).toHaveAccessibleDescription('This account is inactive, so its rows cannot be changed.')
      fireEvent.click(inactiveEdit)
      expect(screen.getByText('at:/cashflow/accounts?month=2026-10&tab=transactions')).toBeInTheDocument()
    })

    it('deletes a record in place after confirmation, toasting and refreshing the month', async () => {
      deleteExpenseMock.mockResolvedValue()
      const onDeleted = vi.fn()
      renderGrid({ onDeleted })

      const dialog = await requestDelete('expense Tesco, 28/10/2026, Barclays, Groceries', 'expense')
      expect(dialog).toHaveTextContent('Delete this expense? This cannot be undone.')
      fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

      expect(await screen.findAllByText('Expense deleted')).not.toHaveLength(0)
      expect(deleteExpenseMock).toHaveBeenCalledExactlyOnceWith('e1')
      expect(onDeleted).toHaveBeenCalledTimes(1)
    })

    it('deletes a pot operation from its Pots account row with its pot lines', async () => {
      deletePotOperationMock.mockResolvedValue()
      const onDeleted = vi.fn()
      renderGrid({ onDeleted })

      const dialog = await requestDelete('deposit Deposit from an income, Casa, 10/10/2026, Reserva', 'deposit')
      expect(dialog).toHaveTextContent('Delete this deposit? Its pot lines and any bank expenses are deleted with it. This cannot be undone.')
      fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

      expect(await screen.findAllByText('Deposit deleted')).not.toHaveLength(0)
      expect(deletePotOperationMock).toHaveBeenCalledExactlyOnceWith('o1')
      expect(onDeleted).toHaveBeenCalledTimes(1)
    })

    it('shows the running delete on its row and blocks the others', async () => {
      deleteExpenseMock.mockReturnValue(never())
      renderGrid()

      const dialog = await requestDelete('expense Tesco, 28/10/2026, Barclays, Groceries', 'expense')
      fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

      expect(await screen.findByRole('button', { name: 'Deleting expense Tesco, 28/10/2026, Barclays, Groceries', hidden: true })).toHaveAttribute('aria-disabled', 'true')
      expect(screen.getByRole('button', { name: 'Delete deposit Deposit from an income, Casa, 10/10/2026, Reserva', hidden: true })).toHaveAccessibleDescription(
        'Another delete is in progress.',
      )
    })

    it('keeps the dialog open with the error when the delete fails, and does not refresh', async () => {
      deleteExpenseMock.mockRejectedValue(new Error('boom'))
      const onDeleted = vi.fn()
      renderGrid({ onDeleted })

      const dialog = await requestDelete('expense Tesco, 28/10/2026, Barclays, Groceries', 'expense')
      fireEvent.click(within(dialog).getByRole('button', { name: 'Delete', hidden: true }))

      expect(await within(dialog).findByText(/boom|could not be deleted/)).toBeInTheDocument()
      expect(onDeleted).not.toHaveBeenCalled()
    })
  })

  it('[AC P60-F16-month-transactions-view-04] shows a skeleton under the headers while loading, with no footer', () => {
    renderGrid({ rows: [], isLoading: true })

    expect(screen.getByRole('button', { name: 'Date' })).toBeInTheDocument()
    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
    expect(queryFooter()).not.toBeInTheDocument()
  })

  it('[AC P60-F16-month-transactions-view-04] shows the empty month message with no footer and nothing to clear', () => {
    renderGrid({ rows: [] })

    expect(screen.getByText('No transactions in October 2026.')).toBeInTheDocument()
    expect(queryFooter()).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
  })

  it('[AC P60-F16-month-transactions-view-04] shows the no-match message with Clear filters above the grid', () => {
    renderGrid()

    filterToNone('Account')

    expect(screen.getByText('No rows match the current filters.')).toBeInTheDocument()
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

  describe('in the stacked narrow layout, where the header row is clipped', () => {
    const originalMatchMedia = window.matchMedia

    afterEach(() => {
      window.matchMedia = originalMatchMedia
    })

    it('shows the filtered row count with a single Clear filters that focuses the grid', async () => {
      mockMatchMedia(true)
      renderGrid()

      filterTo('Account', 'Itau')

      expect(await screen.findByRole('status')).toHaveTextContent('Filtered: 1 of 7 rows')
      fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }))

      expect(bodyRows()).toHaveLength(7)
      expect(screen.getByRole('status')).toHaveTextContent('Filters cleared: showing all 7 rows')
      expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
      expect(screen.getByRole('table', { name: 'All transactions' })).toHaveFocus()
    })
  })
})
