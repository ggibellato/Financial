import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountStatementDto } from '../../api/types'
import { useCardStatements, type CardStatements, type StoredStatement } from '../../hooks/useCardStatements'
import type { DueDateFormState, StatementDueDateFormHandle } from '../../hooks/useStatementDueDateForm'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import CardStatementsTab from '../CardStatementsTab'

const { getAccountStatementsMock, getAccountsMock, setStatementDueDateMock } = vi.hoisted(() => ({
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  setStatementDueDateMock: vi.fn<FinancialApiClient['setStatementDueDate']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccountStatements: getAccountStatementsMock,
    getAccounts: getAccountsMock,
    setStatementDueDate: setStatementDueDateMock,
  } as Partial<FinancialApiClient>,
}))

const NOVEMBER: AccountStatementDto = {
  id: null, year: 2026, month: 11, total: 0, creditIn: 0, outstandingTotal: 0, creditCarried: 0, status: 'NothingDue', isPaid: false, dueDate: null, paidOn: null, paidAmount: null, paidFromAccountName: null,
}
const OCTOBER: StoredStatement = {
  id: 's-oct', year: 2026, month: 10, total: 210.45, creditIn: 0, outstandingTotal: 210.45, creditCarried: 0, status: 'Unpaid', isPaid: false, dueDate: '2026-11-03', paidOn: null, paidAmount: null, paidFromAccountName: null,
}
const SEPTEMBER: StoredStatement = {
  id: 's-sep', year: 2026, month: 9, total: 512.3, creditIn: 0, outstandingTotal: 0, creditCarried: 0, status: 'Paid', isPaid: true, dueDate: '2026-10-03', paidOn: '2026-10-12', paidAmount: 512.3, paidFromAccountName: 'Barclays',
}
const CARRYING: AccountStatementDto = {
  id: 's-aug', year: 2026, month: 8, total: -35, creditIn: 12.5, outstandingTotal: 0, creditCarried: 47.5, status: 'NothingDue', isPaid: false, dueDate: null, paidOn: null, paidAmount: null, paidFromAccountName: null,
}

function dueDateHandle(overrides: Partial<StatementDueDateFormHandle> = {}): StatementDueDateFormHandle {
  return { form: null, failedSubmits: 0, isDirty: false, open: vi.fn(), close: vi.fn(), setDate: vi.fn(), save: vi.fn(), clear: vi.fn(), ...overrides }
}

function dueDateFormState(overrides: Partial<DueDateFormState> = {}): DueDateFormState {
  return { statement: { ...OCTOBER, dueDate: '2026-10-25' }, date: '2026-10-25', pendingAction: null, error: null, dateError: undefined, ...overrides }
}

function cardStatements(overrides: Partial<CardStatements> = {}): CardStatements {
  return {
    statements: [NOVEMBER, OCTOBER, SEPTEMBER],
    isLoading: false,
    error: null,
    retry: vi.fn(),
    payers: [
      { id: 'barclays', name: 'Barclays', roundUpEnabled: false, isActive: true },
      { id: 'closed', name: 'Closed', roundUpEnabled: false, isActive: false },
    ],
    isLoadingPayers: false,
    payersError: null,
    retryPayers: vi.fn(),
    payForm: null,
    savedCount: 0,
    failedSubmit: null,
    isFormDirty: false,
    dueDateForm: dueDateHandle(),
    openPayForm: vi.fn(),
    closeForms: vi.fn(),
    setPayField: vi.fn(),
    submitPayForm: vi.fn(),
    unmark: null,
    requestUnmark: vi.fn(),
    cancelUnmark: vi.fn(),
    confirmUnmark: vi.fn(),
    ...overrides,
  }
}

const runAtOnce = (change: () => void) => change()

function tabElement(statements: CardStatements, canAct: boolean, guardChange: (change: () => void) => void = runAtOnce) {
  return (
    <CardStatementsTab
      cardStatements={statements}
      viewedYear={2026}
      viewedMonth={10}
      currency="GBP"
      canAct={canAct}
      guardChange={guardChange}
    />
  )
}

function renderTab(overrides: Partial<CardStatements> = {}, canAct = true, guardChange?: (change: () => void) => void) {
  const statements = cardStatements(overrides)
  const view = render(tabElement(statements, canAct, guardChange))
  return { ...statements, rerenderWith: (next: Partial<CardStatements>) => view.rerender(tabElement(cardStatements({ ...overrides, ...next }), canAct, guardChange)) }
}

function rowOf(month: string) {
  return screen.getByRole('cell', { name: new RegExp(month) }).closest('tr') as HTMLElement
}

describe('CardStatementsTab', () => {
  it('[AC P60-F08-statement-payment-and-card-screen-11] lists each month with its total, due date and status, highlighting the viewed month', () => {
    renderTab()

    expect(within(rowOf('October 2026')).getAllByText('210.45')).toHaveLength(2)
    expect(within(rowOf('October 2026')).getByText('03/11/2026')).toBeInTheDocument()
    expect(within(rowOf('November 2026')).getAllByText('—')).toHaveLength(2)
    expect(within(rowOf('September 2026')).getByText('Paid 512.30 on 12 Oct 2026 from Barclays')).toBeInTheDocument()
    expect(within(rowOf('October 2026')).getByText('Unpaid')).toBeInTheDocument()
    expect(screen.getByRole('row', { current: true })).toBe(rowOf('October 2026'))
    expect(within(rowOf('October 2026')).getByText('Viewed month')).toBeInTheDocument()
  })

  it('lists the newest month first and sorts by a header', async () => {
    renderTab()

    const months = () => screen.getAllByRole('row').slice(1).map((row) => (row as HTMLTableRowElement).cells[0].textContent)
    expect(months()).toEqual([expect.stringContaining('November'), expect.stringContaining('October'), expect.stringContaining('September')])

    await userEvent.click(screen.getByRole('button', { name: /^Total/ }))

    expect(months()).toEqual([expect.stringContaining('November'), expect.stringContaining('October'), expect.stringContaining('September')])
    expect(screen.getByRole('columnheader', { name: /^Total/ })).toHaveAttribute('aria-sort', 'ascending')

    await userEvent.click(screen.getByRole('button', { name: /^Status/ }))

    expect(months()).toEqual([expect.stringContaining('November'), expect.stringContaining('September'), expect.stringContaining('October')])
  })

  it('[AC P60-F15-card-refunds-and-credit-03] shows Total, Credit in and To pay in that order, and an empty month as Nothing due', () => {
    renderTab({ statements: [NOVEMBER, CARRYING] })

    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual(['Month', 'Total', 'Credit in', 'To pay', 'Due date', 'Status', ''])
    const cells = within(rowOf('August 2026')).getAllByRole('cell').map((cell) => cell.textContent)
    expect(cells.slice(1, 4)).toEqual(['Total:-35.00', 'Credit in:12.50', 'To pay:0.00'])
    expect(within(rowOf('November 2026')).getByText('Nothing due')).toBeInTheDocument()
    expect(within(rowOf('November 2026')).getAllByRole('cell')[2]).toHaveTextContent('—')
  })

  it('[AC P60-F15-card-refunds-and-credit-02] shows no Pay on a Nothing due row, stored or not, and names the credit it carries', () => {
    renderTab({ statements: [NOVEMBER, CARRYING] })

    expect(within(rowOf('August 2026')).getByText('Nothing due · 47.50 credit carried')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Pay / })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Unmark / })).not.toBeInTheDocument()
  })

  it('shows the amount actually paid when carried credit made it differ from the total', () => {
    renderTab({ statements: [{ ...SEPTEMBER, creditIn: 100.3, paidAmount: 412 }] })

    expect(within(rowOf('September 2026')).getByText('Paid 412.00 on 12 Oct 2026 from Barclays')).toBeInTheDocument()
  })

  it('shows no empty Actions label on a row with no action', () => {
    renderTab({ statements: [NOVEMBER] }, false)

    expect(screen.queryByText(/^Actions/)).not.toBeInTheDocument()
  })

  it('moves focus to the Bank field when Pay opens on another statement', () => {
    const payFormFor = (statement: StoredStatement) => ({ statement, bankId: '', date: '2026-10-15', isSaving: false, error: null, fieldErrors: {} })
    const statements = renderTab({ payForm: payFormFor(OCTOBER) })
    screen.getByLabelText(/^Date/).focus()

    statements.rerenderWith({ payForm: payFormFor({ ...OCTOBER, id: 's-dec', month: 12 }) })

    expect(screen.getByLabelText(/^Bank/)).toHaveFocus()
  })

  it('offers no Pay on an unpaid row with no stored statement', () => {
    renderTab({ statements: [{ ...OCTOBER, id: null }] })

    expect(within(rowOf('October 2026')).getByText('Unpaid')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Pay / })).not.toBeInTheDocument()
  })

  it('shows the amount paid alone when the payment date is unknown', () => {
    renderTab({ statements: [{ ...SEPTEMBER, paidOn: null, paidFromAccountName: null }] })

    expect(within(rowOf('September 2026')).getByText('Paid 512.30')).toBeInTheDocument()
  })

  it('shows the amount and date when the bank is unknown', () => {
    renderTab({ statements: [{ ...SEPTEMBER, paidFromAccountName: null }] })

    expect(within(rowOf('September 2026')).getByText('Paid 512.30 on 12 Oct 2026')).toBeInTheDocument()
  })

  it('shows a skeleton while loading', () => {
    renderTab({ statements: [], isLoading: true })

    expect(screen.getByLabelText('Loading statements')).toBeInTheDocument()
  })

  it('says when the card has no statements yet', () => {
    renderTab({ statements: [] })

    expect(screen.getByText('No statements yet')).toBeInTheDocument()
  })

  it('shows the load error with Try again', async () => {
    const statements = renderTab({ statements: [], error: 'Unable to load the statements' })

    await userEvent.click(screen.getByRole('button', { name: /Try again/ }))

    expect(screen.getByText('Unable to load the statements')).toBeInTheDocument()
    expect(statements.retry).toHaveBeenCalled()
  })

  it('offers Pay on unpaid rows and Unmark on paid rows', async () => {
    const statements = renderTab()

    await userEvent.click(screen.getByRole('button', { name: 'Pay October 2026 statement' }))
    await userEvent.click(screen.getByRole('button', { name: 'Unmark September 2026 statement' }))

    expect(statements.openPayForm).toHaveBeenCalledWith(OCTOBER)
    expect(statements.requestUnmark).toHaveBeenCalledWith(SEPTEMBER)
    expect(within(rowOf('September 2026')).queryByRole('button', { name: /^Pay/ })).not.toBeInTheDocument()
  })

  it('says why an inactive card offers no Pay or Unmark', () => {
    renderTab({}, false)

    expect(screen.queryByRole('button', { name: /^(Pay|Unmark) / })).not.toBeInTheDocument()
    expect(screen.getByText("This card is inactive, so its statements can't be paid, unmarked or given a due date here.")).toBeInTheDocument()
  })

  it('opens Pay only through the discard guard', async () => {
    const guardChange = vi.fn()
    const statements = renderTab({}, true, guardChange)

    await userEvent.click(screen.getByRole('button', { name: 'Pay October 2026 statement' }))

    expect(guardChange).toHaveBeenCalledTimes(1)
    expect(statements.openPayForm).not.toHaveBeenCalled()
  })

  it('moves focus to the Statements heading after a payment or unmark is saved', () => {
    const statements = renderTab()

    statements.rerenderWith({ savedCount: 1 })

    expect(screen.getByRole('heading', { name: 'Statements' })).toHaveFocus()
  })

  it('shows the pay form with the amount read-only, its errors and its saving state', () => {
    renderTab({
      payForm: {
        statement: OCTOBER,
        bankId: '',
        date: '2026-10-16',
        isSaving: false,
        error: 'This statement is already paid.',
        fieldErrors: { bankId: 'Select a bank.', date: 'Date must not be in the future.' },
      },
    })

    expect(screen.getByRole('heading', { name: 'Pay October 2026 statement' })).toBeInTheDocument()
    expect(screen.getByText('This statement is already paid.')).toBeInTheDocument()
    expect(screen.getByText('Select a bank.')).toBeInTheDocument()
    expect(screen.getByText('Date must not be in the future.')).toBeInTheDocument()
    expect(screen.getByLabelText('Amount')).toHaveValue('£210.45')
    expect(screen.getByLabelText('Amount')).toHaveAttribute('readonly')
    expect(screen.getByLabelText(/^Bank/)).toHaveFocus()
  })

  it('moves focus to the first invalid field after a failed submit', () => {
    const payForm = { statement: OCTOBER, bankId: 'barclays', date: '', isSaving: false, error: null, fieldErrors: { date: 'Enter a date.' } }
    const statements = renderTab({ payForm })

    statements.rerenderWith({ payForm, failedSubmit: { firstInvalidField: 'date' } })

    expect(screen.getByLabelText(/^Date/)).toHaveFocus()
  })

  it('offers only active banks in the pay form', () => {
    renderTab({ payForm: { statement: OCTOBER, bankId: '', date: '2026-10-15', isSaving: false, error: null, fieldErrors: {} } })

    expect(screen.getByRole('option', { name: 'Barclays' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Closed' })).not.toBeInTheDocument()
  })

  it('uses the amount to pay in the form and its column, not the gross total', () => {
    const creditLowered = { ...OCTOBER, creditIn: 60.45, outstandingTotal: 150 }
    renderTab({
      statements: [creditLowered],
      payForm: { statement: creditLowered, bankId: '', date: '2026-10-15', isSaving: false, error: null, fieldErrors: {} },
    })

    expect(within(rowOf('October 2026')).getByText('150.00')).toBeInTheDocument()
    expect(screen.getByLabelText('Amount')).toHaveValue('£150.00')
  })

  it('disables the bank picker while the banks load', () => {
    renderTab({
      isLoadingPayers: true,
      payForm: { statement: OCTOBER, bankId: '', date: '2026-10-15', isSaving: false, error: null, fieldErrors: {} },
    })

    expect(screen.getByLabelText(/^Bank/)).toBeDisabled()
    expect(screen.getByRole('option', { name: 'Loading banks…' })).toBeInTheDocument()
  })

  it('disables the pay form while saving', () => {
    renderTab({
      payForm: { statement: OCTOBER, bankId: 'barclays', date: '2026-10-15', isSaving: true, error: null, fieldErrors: {} },
    })

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByLabelText(/^Bank/)).toBeDisabled()
  })

  it('submits and cancels the pay form', async () => {
    const statements = renderTab({
      payForm: { statement: OCTOBER, bankId: 'barclays', date: '2026-10-15', isSaving: false, error: null, fieldErrors: {} },
    })

    await userEvent.selectOptions(screen.getByLabelText(/^Bank/), 'barclays')
    await userEvent.click(screen.getByRole('button', { name: 'Pay statement' }))
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(statements.setPayField).toHaveBeenCalledWith('bankId', 'barclays')
    expect(statements.submitPayForm).toHaveBeenCalled()
    expect(statements.closeForms).toHaveBeenCalled()
  })

  it('asks before unmarking and shows the reason when unmark fails', async () => {
    renderTab({ unmark: { statement: SEPTEMBER, isBusy: false, error: 'The payment for this statement was changed elsewhere. Reload and try again.' } })

    expect(await screen.findByText('Unmark September 2026 as paid? The payment will be deleted.')).toBeInTheDocument()
    expect(screen.getByText('The payment for this statement was changed elsewhere. Reload and try again.')).toBeInTheDocument()
  })

  describe('due date', () => {
    const IN_MONTH: StoredStatement = { ...OCTOBER, dueDate: '2026-10-25' }

    beforeEach(() => {
      getAccountStatementsMock.mockReset().mockResolvedValue([IN_MONTH])
      getAccountsMock.mockReset().mockResolvedValue([])
      setStatementDueDateMock.mockReset().mockResolvedValue(undefined)
    })

    function RealTab({ onChanged }: { onChanged: (message: string) => void }) {
      return tabElement(useCardStatements('card-amex', onChanged), true)
    }

    it("[AC P60-F13-consolidated-navigation-and-retirement-03] sets and clears a statement's due date", async () => {
      const onChanged = vi.fn()
      render(<RealTab onChanged={onChanged} />)

      await userEvent.click(await screen.findByRole('button', { name: 'Set the October 2026 due date' }))
      expect(screen.getByRole('heading', { name: 'October 2026 due date' })).toBeInTheDocument()
      expect(screen.getByLabelText(/^Due date/)).toHaveFocus()
      fireEvent.change(screen.getByLabelText(/^Due date/), { target: { value: '2026-10-20' } })
      await userEvent.click(screen.getByRole('button', { name: 'Save due date' }))

      expect(setStatementDueDateMock).toHaveBeenCalledWith('card-amex', 2026, 10, { dueDate: '2026-10-20' })
      await waitFor(() => expect(onChanged).toHaveBeenCalledWith('Due date saved'))
      await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(2))
      expect(screen.queryByRole('button', { name: 'Save due date' })).not.toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Statements' })).toHaveFocus()

      await userEvent.click(screen.getByRole('button', { name: 'Set the October 2026 due date' }))
      await userEvent.click(screen.getByRole('button', { name: 'Clear due date' }))

      expect(setStatementDueDateMock).toHaveBeenLastCalledWith('card-amex', 2026, 10, { dueDate: null })
      await waitFor(() => expect(onChanged).toHaveBeenCalledWith('Due date cleared'))
    })

    it('offers Due date on Unpaid and Nothing due rows, stored or not, and never on a Paid row', () => {
      renderTab({ statements: [NOVEMBER, OCTOBER, SEPTEMBER, CARRYING, { ...OCTOBER, id: null, month: 7 }] })

      expect(screen.getByRole('button', { name: 'Set the November 2026 due date' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Set the October 2026 due date' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Set the August 2026 due date' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Set the July 2026 due date' })).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Set the September 2026 due date' })).not.toBeInTheDocument()
    })

    it('offers no Due date on an inactive card or a card with no rows', () => {
      const { rerenderWith } = renderTab({}, false)
      expect(screen.queryByRole('button', { name: /due date$/ })).not.toBeInTheDocument()

      rerenderWith({ statements: [] })
      expect(screen.getByText('No statements yet')).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /due date$/ })).not.toBeInTheDocument()
    })

    it('opens only through the discard guard', async () => {
      const guardChange = vi.fn()
      const dueDateForm = dueDateHandle()
      renderTab({ dueDateForm }, true, guardChange)

      await userEvent.click(screen.getByRole('button', { name: 'Set the October 2026 due date' }))

      expect(guardChange).toHaveBeenCalledTimes(1)
      expect(dueDateForm.open).not.toHaveBeenCalled()
    })

    it('limits the field to the month and offers Clear only when the row has a date', () => {
      const { rerenderWith } = renderTab({ dueDateForm: dueDateHandle({ form: dueDateFormState() }) })

      expect(screen.getByLabelText(/^Due date/)).toHaveAttribute('min', '2026-10-01')
      expect(screen.getByLabelText(/^Due date/)).toHaveAttribute('max', '2026-10-31')
      expect(screen.getByRole('button', { name: 'Clear due date' })).toBeInTheDocument()

      rerenderWith({ dueDateForm: dueDateHandle({ form: dueDateFormState({ statement: { ...OCTOBER, dueDate: null }, date: '' }) }) })
      expect(screen.queryByRole('button', { name: 'Clear due date' })).not.toBeInTheDocument()
    })

    async function openRealForm() {
      render(<RealTab onChanged={vi.fn()} />)
      await userEvent.click(await screen.findByRole('button', { name: 'Set the October 2026 due date' }))
    }

    it('shows the validation message and moves focus to the field after a failed submit', async () => {
      await openRealForm()
      fireEvent.change(screen.getByLabelText(/^Due date/), { target: { value: '2026-11-01' } })

      await userEvent.click(screen.getByRole('button', { name: 'Save due date' }))

      expect(screen.getByText('The due date must be in October 2026.')).toBeInTheDocument()
      expect(screen.getByLabelText(/^Due date/)).toHaveFocus()
      expect(setStatementDueDateMock).not.toHaveBeenCalled()
    })

    it('announces a server error in the form, keeps the value and returns focus to the field', async () => {
      setStatementDueDateMock.mockRejectedValue(new ApiError('Amex is inactive.', 400))
      await openRealForm()
      fireEvent.change(screen.getByLabelText(/^Due date/), { target: { value: '2026-10-20' } })

      await userEvent.click(screen.getByRole('button', { name: 'Save due date' }))

      expect(await screen.findByRole('alert')).toHaveTextContent('Amex is inactive.')
      expect(screen.getByLabelText(/^Due date/)).toHaveValue('2026-10-20')
      expect(screen.getByLabelText(/^Due date/)).toHaveFocus()
    })

    it.each([
      ['Save due date', 'Saving...'],
      ['Clear due date', 'Clearing...'],
    ])('disables the form and the row actions while %s runs', async (action, label) => {
      setStatementDueDateMock.mockReturnValue(new Promise(() => {}))
      await openRealForm()

      await userEvent.click(screen.getByRole('button', { name: action }))

      expect(screen.getByRole('button', { name: label })).toBeDisabled()
      expect(screen.getByLabelText(/^Due date/)).toBeDisabled()
      expect(screen.getByRole('button', { name: 'Set the October 2026 due date' })).toBeDisabled()
      expect(screen.getByRole('button', { name: 'Pay October 2026 statement' })).toBeDisabled()
    })

    it('places Clear due date after Cancel, apart from the save pair', () => {
      renderTab({ dueDateForm: dueDateHandle({ form: dueDateFormState() }) })

      const actions = within(screen.getByRole('form', { name: 'October 2026 due date' })).getAllByRole('button').map((button) => button.textContent)
      expect(actions).toEqual(['Save due date', 'Cancel', 'Clear due date'])
    })

    it('saves through the hook, and Cancel closes and returns focus to the row button', async () => {
      const dueDateForm = dueDateHandle({ form: dueDateFormState() })
      const statements = renderTab({ dueDateForm })

      await userEvent.click(screen.getByRole('button', { name: 'Save due date' }))
      expect(dueDateForm.save).toHaveBeenCalled()

      const opener = screen.getByRole('button', { name: 'Set the October 2026 due date' })
      await userEvent.click(opener)
      expect(dueDateForm.open).toHaveBeenCalledWith(OCTOBER)
      await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(statements.closeForms).toHaveBeenCalled()
      expect(opener).toHaveFocus()
    })
  })
})
