import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AccountStatementDto } from '../../api/types'
import type { CardStatements } from '../../hooks/useCardStatements'
import { render, screen, within } from '../../test/renderWithFluent'
import CardStatementsTab from '../CardStatementsTab'

const NOVEMBER: AccountStatementDto = {
  id: 's-nov', year: 2026, month: 11, total: 0, outstandingTotal: 0, isPaid: false, dueDate: null, paidOn: null, paidFromAccountName: null,
}
const OCTOBER: AccountStatementDto = {
  id: 's-oct', year: 2026, month: 10, total: 210.45, outstandingTotal: 210.45, isPaid: false, dueDate: '2026-11-03', paidOn: null, paidFromAccountName: null,
}
const SEPTEMBER: AccountStatementDto = {
  id: 's-sep', year: 2026, month: 9, total: 512.3, outstandingTotal: 0, isPaid: true, dueDate: '2026-10-03', paidOn: '2026-10-12', paidFromAccountName: 'Barclays',
}

function cardStatements(overrides: Partial<CardStatements> = {}): CardStatements {
  return {
    statements: [NOVEMBER, OCTOBER, SEPTEMBER],
    isLoading: false,
    error: null,
    retry: vi.fn(),
    payers: [
      { id: 'barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
      { id: 'closed', name: 'Closed', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: false },
    ],
    isLoadingPayers: false,
    payersError: null,
    retryPayers: vi.fn(),
    payForm: null,
    savedCount: 0,
    failedSubmit: null,
    isPayFormDirty: false,
    openPayForm: vi.fn(),
    closePayForm: vi.fn(),
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

    expect(within(rowOf('October 2026')).getByText('210.45')).toBeInTheDocument()
    expect(within(rowOf('October 2026')).getByText('03/11/2026')).toBeInTheDocument()
    expect(within(rowOf('November 2026')).getByText('—')).toBeInTheDocument()
    expect(within(rowOf('September 2026')).getByText('Paid 12 Oct 2026 from Barclays')).toBeInTheDocument()
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

    expect(months()).toEqual([expect.stringContaining('September'), expect.stringContaining('November'), expect.stringContaining('October')])
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

  it('keeps Pay focusable but inert with Nothing to pay when the total is zero', async () => {
    const statements = renderTab()
    const pay = screen.getByRole('button', { name: 'Pay November 2026 statement' })

    await userEvent.click(pay)

    expect(pay).toHaveAttribute('aria-disabled', 'true')
    expect(pay).toHaveAccessibleDescription('Nothing to pay')
    expect(statements.openPayForm).not.toHaveBeenCalled()
  })

  it('says why an inactive card offers no Pay or Unmark', () => {
    renderTab({}, false)

    expect(screen.queryByRole('button', { name: /^(Pay|Unmark) / })).not.toBeInTheDocument()
    expect(screen.getByText("This card is inactive, so its statements can't be paid or unmarked here.")).toBeInTheDocument()
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

  it('uses the outstanding amount for Pay and the form, not the gross total', () => {
    renderTab({
      statements: [{ ...OCTOBER, outstandingTotal: 0 }],
      payForm: { statement: { ...OCTOBER, outstandingTotal: 150 }, bankId: '', date: '2026-10-15', isSaving: false, error: null, fieldErrors: {} },
    })

    expect(screen.getByRole('button', { name: 'Pay October 2026 statement' })).toHaveAttribute('aria-disabled', 'true')
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
    expect(statements.closePayForm).toHaveBeenCalled()
  })

  it('asks before unmarking and shows the reason when unmark fails', async () => {
    renderTab({ unmark: { statement: SEPTEMBER, isBusy: false, error: 'The payment for this statement was changed elsewhere. Reload and try again.' } })

    expect(await screen.findByText('Unmark September 2026 as paid? The payment will be deleted.')).toBeInTheDocument()
    expect(screen.getByText('The payment for this statement was changed elsewhere. Reload and try again.')).toBeInTheDocument()
  })
})
