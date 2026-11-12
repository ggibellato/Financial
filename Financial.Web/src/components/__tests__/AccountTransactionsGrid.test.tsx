import { describe, expect, it, vi } from 'vitest'
import type { AccountTransactionDto, SettlementEntryDto } from '../../api/types'
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

const EMPTY_MESSAGE = 'No transactions in October 2026.'

const NO_ROW_ACTIONS = { onEdit: vi.fn(), onDelete: vi.fn() }

function bodyRows() {
  const [, ...rows] = screen.getAllByRole('row')
  return rows
}

describe('AccountTransactionsGrid', () => {
  describe('a credit refund row', () => {
    const CREDIT_REFUND = accountTransactionRow({ id: 'cr1', kind: 'CardCreditRefund', date: '2026-10-15', description: 'BA Amex credit refund', amount: -35, canEdit: false })

    it('[AC P60-F15-card-refunds-and-credit-05] offers only Delete on an active screen account', () => {
      const onDelete = vi.fn()
      render(<AccountTransactionsGrid accountId="card-1" transactions={[CREDIT_REFUND]} emptyMessage={EMPTY_MESSAGE} onEdit={vi.fn()} onDelete={onDelete} onMove={vi.fn()} canDeleteCreditRefunds />)

      const [row] = bodyRows()
      expect(within(row).getAllByRole('button')).toHaveLength(1)
      fireEvent.click(within(row).getByRole('button', { name: /^Delete credit refund\b/ }))
      expect(onDelete).toHaveBeenCalledWith(CREDIT_REFUND)
    })

    it('offers nothing when the screen account is inactive', () => {
      render(<AccountTransactionsGrid accountId="card-1" transactions={[CREDIT_REFUND]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} canDeleteCreditRefunds={false} />)

      expect(within(bodyRows()[0]).queryByRole('button')).not.toBeInTheDocument()
    })

    it('gives other read-only rows no Delete', () => {
      const charge = accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-10-10', description: 'Amazon', amount: -60, canEdit: false })
      render(<AccountTransactionsGrid accountId="bank-1" transactions={[charge]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} canDeleteCreditRefunds />)

      expect(within(bodyRows()[0]).queryByRole('button')).not.toBeInTheDocument()
    })
  })

  describe('Settlement columns', () => {
    const entryRow = (id: string, amount: number, currency: string, settlement: Partial<SettlementEntryDto>) =>
      accountTransactionRow({
        id,
        kind: 'SettlementEntry',
        date: '2026-10-10',
        description: id,
        amount,
        currency,
        settlement: {
          id,
          accountId: 'mae',
          date: '2026-10-10',
          description: id,
          note: '',
          paidBy: 'You',
          currency,
          amount: Math.abs(amount),
          convertedCurrency: null,
          convertedValue: null,
          warning: null,
          ...settlement,
        },
      })
    const ENTRIES = [
      entryRow('Farmácia', 150, 'BRL', { convertedCurrency: 'GBP', convertedValue: 21.5, note: 'metade' }),
      entryRow('Taxa', -80, 'GBP', { paidBy: 'Other', convertedCurrency: 'BRL', convertedValue: null }),
    ]
    const renderSettlement = (transactions = ENTRIES, showsConverted = true) =>
      render(<AccountTransactionsGrid accountId="mae" columns="settlement" otherParticipant="Mãe" showsConverted={showsConverted} transactions={transactions} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    it('[AC P60-F12-settlement-screen-03] shows each entry with who paid, its amount in its own currency, the converted value or a dash, and the note', () => {
      renderSettlement()

      expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual(['Date', 'Description', 'Paid by', 'Amount', 'Converted', 'Note', ''])
      const paidByYou = bodyRows().find((row) => within(row).queryByText('Farmácia')) as HTMLElement
      const paidByOther = bodyRows().find((row) => within(row).queryByText('Taxa')) as HTMLElement
      expect(within(paidByYou).getByText('You')).toBeInTheDocument()
      expect(within(paidByYou).getByText('R$150.00').closest('td')).toHaveClass('data-table__col--numeric')
      expect(within(paidByYou).getByText('£21.50').closest('td')).toHaveClass('data-table__col--numeric')
      expect(within(paidByYou).getByText('metade')).toBeInTheDocument()
      expect(within(paidByOther).getByText('Mãe')).toBeInTheDocument()
      expect(within(paidByOther).getByText('£80.00')).toBeInTheDocument()
      expect(within(paidByOther).getByText('—')).toBeInTheDocument()
    })

    it('[AC P60-F12-settlement-screen-03] hides the Converted column on a one-currency account', () => {
      renderSettlement([entryRow('Taxa', 80, 'GBP', {})], false)

      expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual(['Date', 'Description', 'Paid by', 'Amount', 'Note', ''])
      expect(screen.queryByText('—')).not.toBeInTheDocument()
    })

    it('offers Edit and Delete on an entry row', () => {
      renderSettlement()

      expect(screen.getAllByRole('button', { name: /^Edit entry\b/ })).toHaveLength(2)
      expect(screen.getAllByRole('button', { name: /^Delete entry\b/ })).toHaveLength(2)
    })

    it.each([
      ['Paid by', 'Taxa'],
      ['Amount', 'Taxa'],
      ['Converted', 'Farmácia'],
      ['Note', 'Farmácia'],
    ])('sorts ascending by %s, putting %s first', (header, firstDescription) => {
      renderSettlement()

      fireEvent.click(screen.getByRole('button', { name: header }))

      expect(within(bodyRows()[0]).getByText(firstDescription)).toBeInTheDocument()
    })

    it('spans the message across the Settlement columns when the month has no entries', () => {
      renderSettlement([])

      expect(screen.getByText(EMPTY_MESSAGE)).toHaveAttribute('colspan', '7')
    })
  })

  it('shows pot movements as date, pot, description and amount, with edit and delete only on legacy rows', () => {
    render(
      <AccountTransactionsGrid accountId="bank-1"
        columns="pots"
        transactions={[
          accountTransactionRow({ id: 'm1', kind: 'PotTransaction', date: '2026-10-05', description: 'Boots', counterpart: 'Trips', amount: -20, canEdit: true }),
          accountTransactionRow({ id: 'm2', kind: 'PotTransaction', date: '2026-10-04', description: 'Bonus', counterpart: 'Home', amount: 30, canEdit: false }),
        ]}
        emptyMessage={EMPTY_MESSAGE}
        {...NO_ROW_ACTIONS}
      />,
    )

    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual(['Date', 'Pot', 'Description', 'Amount', ''])
    const [legacyRow, linkedRow] = bodyRows()
    expect(within(legacyRow).getByRole('button', { name: /^Edit pot transaction\b/ })).toBeInTheDocument()
    expect(within(legacyRow).getByRole('button', { name: /^Delete pot transaction\b/ })).toBeInTheDocument()
    expect(within(legacyRow).getByText('Trips')).toBeInTheDocument()
    expect(within(legacyRow).getByText('-20.00')).toBeInTheDocument()
    expect(within(linkedRow).queryByRole('button')).not.toBeInTheDocument()
  })

  it('spans the message across the pot columns when there are no movements', () => {
    render(<AccountTransactionsGrid accountId="bank-1" columns="pots" transactions={[]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(screen.getByText(EMPTY_MESSAGE)).toHaveAttribute('colspan', '5')
  })

  it('[AC P60-F05-bank-account-screen-04] shows signed, right-aligned amounts and a separate round-up column', () => {
    render(<AccountTransactionsGrid accountId="bank-1" transactions={ROWS} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    const expenseRow = bodyRows()[0]
    const amountCell = within(expenseRow).getByText('-42.10').closest('td')
    const roundUpCell = within(expenseRow).getByText('-0.90').closest('td')
    expect(amountCell).toHaveClass('data-table__col--numeric')
    expect(roundUpCell).toHaveClass('data-table__col--numeric')
    expect(within(bodyRows()[1]).getByText('+2,500.00').closest('td')).toHaveClass('data-table__col--numeric')
    expect(screen.getByRole('columnheader', { name: 'Amount' })).toHaveClass('data-table__col--numeric')
  })

  it('labels each kind and shows the date, category and counterpart', () => {
    render(<AccountTransactionsGrid accountId="bank-1" transactions={ROWS} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

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
    render(<AccountTransactionsGrid accountId="bank-1" transactions={[...ROWS].reverse()} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(within(bodyRows()[0]).getByText('14/10/2026')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Amount' }))

    expect(within(bodyRows()[0]).getByText('-200.00')).toBeInTheDocument()
    expect(within(bodyRows()[4]).getByText('+2,500.00')).toBeInTheDocument()
  })

  it('shows the empty message when the month has no rows', () => {
    render(<AccountTransactionsGrid accountId="bank-1" transactions={[]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(screen.getByText(EMPTY_MESSAGE)).toBeInTheDocument()
  })

  it('shows a skeleton instead of data while loading', () => {
    render(<AccountTransactionsGrid accountId="bank-1" transactions={ROWS} isLoading emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
    expect(screen.queryByText('Tesco')).not.toBeInTheDocument()
  })

  it('puts Edit and Delete in a trailing cell of editable rows only', () => {
    const onEdit = vi.fn()
    const onDelete = vi.fn()
    render(<AccountTransactionsGrid accountId="bank-1" transactions={ROWS} emptyMessage={EMPTY_MESSAGE} onEdit={onEdit} onDelete={onDelete} />)

    const expenseCells = within(bodyRows()[0]).getAllByRole('cell')
    const editExpense = within(expenseCells[expenseCells.length - 1]).getByRole('button', { name: /^Edit expense\b/ })
    fireEvent.click(editExpense)
    fireEvent.click(within(bodyRows()[3]).getByRole('button', { name: /^Delete transfer\b/ }))

    expect(onEdit).toHaveBeenCalledWith(ROWS[0], editExpense)
    expect(onDelete).toHaveBeenCalledWith(ROWS[3])
    expect(within(bodyRows()[1]).getByRole('button', { name: /^Edit income\b/ })).toBeInTheDocument()
    expect(within(bodyRows()[4]).getByRole('button', { name: /^Delete balance adjustment\b/ })).toBeInTheDocument()
    expect(within(bodyRows()[2]).queryByRole('button')).not.toBeInTheDocument()
  })

  it('disables Edit, but not Delete, while editing is unavailable', () => {
    render(<AccountTransactionsGrid accountId="bank-1" transactions={ROWS} emptyMessage={EMPTY_MESSAGE} isEditDisabled={() => true} {...NO_ROW_ACTIONS} />)

    expect(within(bodyRows()[0]).getByRole('button', { name: /^Edit expense\b/ })).toBeDisabled()
    expect(within(bodyRows()[0]).getByRole('button', { name: /^Delete expense\b/ })).toBeEnabled()
  })

  it('labels the date column as asked and card payments by their kind', () => {
    render(
      <AccountTransactionsGrid accountId="bank-1"
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

  it('[AC P60-F08-statement-payment-and-card-screen-02] opens the statement a card payment row paid from its Edit icon and says where to delete it', () => {
    const onOpenStatement = vi.fn()
    render(
      <AccountTransactionsGrid accountId="bank-1"
        transactions={[
          accountTransactionRow({
            id: 'p1',
            kind: 'CardPayment',
            date: '2026-11-12',
            description: 'Platinum Visa 6007 payment – Oct 2026 statement',
            counterpart: 'Platinum Visa 6007',
            amount: -512.3,
            canEdit: false,
            statement: { accountId: 'visa-id', year: 2026, month: 10 },
          }),
          accountTransactionRow({ id: 'p2', kind: 'CardPayment', date: '2026-11-02', counterpart: 'Amex', amount: -20, canEdit: false }),
        ]}
        emptyMessage={EMPTY_MESSAGE}
        {...NO_ROW_ACTIONS}
        onOpenStatement={onOpenStatement}
      />,
    )

    const [paid, unlinked] = bodyRows()
    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(within(unlinked).queryByRole('button')).not.toBeInTheDocument()
    const name = 'card payment Platinum Visa 6007 payment – Oct 2026 statement, 12/11/2026'
    fireEvent.click(within(paid).getByRole('button', { name: `Edit ${name}` }))
    expect(onOpenStatement).toHaveBeenCalledWith('/cashflow/accounts/visa-id?month=2026-10&tab=statements')
    const remove = within(paid).getByRole('button', { name: `Delete ${name}` })
    expect(remove).toHaveAttribute('aria-disabled', 'true')
    expect(remove).toHaveAccessibleDescription('Unmark the statement to remove this payment.')
  })

  it('offers Move beside Edit and Delete on an editable card charge only', () => {
    const onMove = vi.fn()
    render(
      <AccountTransactionsGrid accountId="bank-1"
        transactions={[
          accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-10-10', description: 'Taxi', amount: -30, canEdit: true }),
          accountTransactionRow({ id: 'c2', kind: 'CardCharge', date: '2026-10-09', description: 'Paid lunch', amount: -7, canEdit: false }),
          accountTransactionRow({ id: 'p1', kind: 'CardPayment', date: '2026-10-08', amount: 40, canEdit: false }),
        ]}
        emptyMessage={EMPTY_MESSAGE}
        {...NO_ROW_ACTIONS}
        onMove={onMove}
      />,
    )

    const [taxi, paidLunch, payment] = bodyRows()
    expect(within(taxi).getAllByRole('button').map((button) => button.getAttribute('aria-label'))).toEqual([
      'Move card charge Taxi, 10/10/2026 to a later statement',
      'Edit card charge Taxi, 10/10/2026',
      'Delete card charge Taxi, 10/10/2026',
    ])
    expect(within(paidLunch).queryByRole('button')).not.toBeInTheDocument()
    expect(within(payment).queryByRole('button')).not.toBeInTheDocument()

    fireEvent.click(within(taxi).getByRole('button', { name: /^Move card charge\b.* to a later statement$/ }))
    expect(onMove).toHaveBeenCalledWith(expect.objectContaining({ id: 'c1' }), expect.any(HTMLButtonElement))
  })

  describe('linked rows', () => {
    const OPERATION = { operationId: 'op1', potsAccountId: 'pots-1', year: 2026, month: 10, incomeId: null }
    const linkedExpense = (incomeId: string | null) =>
      accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-10', description: 'Deposit to Reserva', amount: -90, canEdit: false, operation: { ...OPERATION, incomeId } })
    const potLine = (amount: number, incomeId: string | null = null, description = 'Deposit to Reserva') =>
      accountTransactionRow({ id: 'm1', kind: 'PotTransaction', date: '2026-10-10', description, counterpart: 'Home', amount, canEdit: false, operation: { ...OPERATION, incomeId } })

    function renderLinked(accountId: string, row: AccountTransactionDto) {
      const handlers = { onOpenOperation: vi.fn(), onDeleteOperation: vi.fn() }
      render(<AccountTransactionsGrid accountId={accountId} transactions={[row]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} {...handlers} />)
      return handlers
    }

    it('opens a linked bank row from its Edit icon and says its operation is deleted from the pots account', () => {
      const { onOpenOperation, onDeleteOperation } = renderLinked('bank-1', linkedExpense(null))

      expect(screen.queryByRole('link')).not.toBeInTheDocument()
      const edit = screen.getByRole('button', { name: 'Edit expense Deposit to Reserva, 10/10/2026' })
      fireEvent.click(edit)
      expect(onOpenOperation).toHaveBeenCalledWith(expect.objectContaining({ operationId: 'op1' }), edit)
      const remove = screen.getByRole('button', { name: 'Delete expense Deposit to Reserva, 10/10/2026' })
      fireEvent.click(remove)
      expect(remove).toHaveAccessibleDescription('Delete it from the pots account.')
      expect(onDeleteOperation).not.toHaveBeenCalled()
    })

    it.each([
      [30, 'deposit'],
      [-30, 'withdrawal'],
    ])('deletes the whole operation of a Pots line of %d as a %s', (amount, noun) => {
      const { onDeleteOperation } = renderLinked('pots-1', potLine(amount))

      fireEvent.click(screen.getByRole('button', { name: 'Delete pot transaction Deposit to Reserva, 10/10/2026' }))

      expect(onDeleteOperation).toHaveBeenCalledWith(expect.objectContaining({ operationId: 'op1' }), noun)
    })

    it("names a Pots line of an income's deposit with no description and sends its delete to the income", () => {
      const { onDeleteOperation } = renderLinked('pots-1', potLine(30, 'i1', ''))

      expect(screen.getByText('Deposit from an income')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Edit pot transaction, 10/10/2026' })).toBeEnabled()
      const remove = screen.getByRole('button', { name: 'Delete pot transaction, 10/10/2026' })
      fireEvent.click(remove)
      expect(remove).toHaveAccessibleDescription('Delete the income to remove this deposit.')
      expect(onDeleteOperation).not.toHaveBeenCalled()
    })
  })

  describe('older split income rows', () => {
    const movement = { id: 'm1', bucketId: 'p1', bucketName: 'Home', amount: 30, date: '2026-10-04', description: 'Salary', incomeId: 'i1', splitLineCount: 1 }
    const olderLine = accountTransactionRow({ id: 'm1', kind: 'PotTransaction', date: '2026-10-04', description: 'Salary', counterpart: 'Home', amount: 30, canEdit: false, movement })

    it('tells where to edit an older split income line, in a column wide enough for the hint', () => {
      render(<AccountTransactionsGrid accountId="pots-1" columns="pots" transactions={[olderLine]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

      const hint = within(bodyRows()[0]).getByText('Edit from the income')
      expect(hint.closest('td')).toHaveClass('data-table__col--action-wide')
      expect(within(bodyRows()[0]).queryByRole('button')).not.toBeInTheDocument()
    })

    it('adds no hint to another read-only row', () => {
      const readOnly = accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-10-10', description: 'Paid lunch', amount: -7, canEdit: false })
      render(<AccountTransactionsGrid accountId="bank-1" transactions={[readOnly]} emptyMessage={EMPTY_MESSAGE} {...NO_ROW_ACTIONS} />)

      expect(screen.queryByText('Edit from the income')).not.toBeInTheDocument()
      expect(screen.getByRole('columnheader', { name: 'Actions' })).not.toHaveClass('data-table__col--action-wide')
    })
  })

  it('offers no Move on a bank expense', () => {
    render(
      <AccountTransactionsGrid accountId="bank-1"
        transactions={[accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-10', description: 'Tesco', amount: -30, canEdit: true })]}
        emptyMessage={EMPTY_MESSAGE}
        {...NO_ROW_ACTIONS}
        onMove={vi.fn()}
      />,
    )

    expect(within(bodyRows()[0]).queryByRole('button', { name: /^Move/ })).not.toBeInTheDocument()
  })
})
