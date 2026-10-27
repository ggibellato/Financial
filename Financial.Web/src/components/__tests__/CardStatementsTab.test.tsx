import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AccountStatementDto } from '../../api/types'
import type { CardStatements } from '../../hooks/useCardStatements'
import { render, screen, within } from '../../test/renderWithFluent'
import CardStatementsTab from '../CardStatementsTab'

const NOVEMBER: AccountStatementDto = {
  id: 's-nov', year: 2026, month: 11, total: 0, isPaid: false, dueDate: null, paidOn: null, paidFromAccountName: null,
}
const OCTOBER: AccountStatementDto = {
  id: 's-oct', year: 2026, month: 10, total: 210.45, isPaid: false, dueDate: '2026-11-03', paidOn: null, paidFromAccountName: null,
}
const SEPTEMBER: AccountStatementDto = {
  id: 's-sep', year: 2026, month: 9, total: 512.3, isPaid: true, dueDate: '2026-10-03', paidOn: '2026-10-12', paidFromAccountName: 'Barclays',
}

function cardStatements(overrides: Partial<CardStatements> = {}): CardStatements {
  return {
    statements: [NOVEMBER, OCTOBER, SEPTEMBER],
    isLoading: false,
    error: null,
    retry: vi.fn(),
    payers: [{ id: 'barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true }],
    payersError: null,
    retryPayers: vi.fn(),
    payForm: null,
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

function renderTab(overrides: Partial<CardStatements> = {}, canAct = true) {
  const statements = cardStatements(overrides)
  render(<CardStatementsTab cardStatements={statements} viewedYear={2026} viewedMonth={10} canAct={canAct} />)
  return statements
}

function rowOf(month: string) {
  return screen.getByRole('cell', { name: new RegExp(month) }).closest('tr') as HTMLElement
}

describe('CardStatementsTab', () => {
  it('[AC P60-F08-statement-payment-and-card-screen-11] lists each month with its total, due date and status, highlighting the viewed month', () => {
    renderTab()

    expect(within(rowOf('October 2026')).getByText('210.45')).toBeInTheDocument()
    expect(within(rowOf('October 2026')).getByText('3 Nov 2026')).toBeInTheDocument()
    expect(within(rowOf('November 2026')).getByText('—')).toBeInTheDocument()
    expect(within(rowOf('September 2026')).getByText('Paid 12 Oct 2026 from Barclays')).toBeInTheDocument()
    expect(within(rowOf('October 2026')).getByText('Unpaid')).toBeInTheDocument()
    expect(screen.getByRole('row', { current: true })).toBe(rowOf('October 2026'))
    expect(within(rowOf('October 2026')).getByText('Viewed month')).toBeInTheDocument()
  })

  it('lists the newest month first and sorts by a header', async () => {
    renderTab()

    const months = () => screen.getAllByRole('row').slice(1).map((row) => row.cells[0].textContent)
    expect(months()).toEqual([expect.stringContaining('November'), expect.stringContaining('October'), expect.stringContaining('September')])

    await userEvent.click(screen.getByRole('button', { name: /^Total/ }))

    expect(months()).toEqual([expect.stringContaining('November'), expect.stringContaining('October'), expect.stringContaining('September')])
    expect(screen.getByRole('columnheader', { name: /^Total/ })).toHaveAttribute('aria-sort', 'ascending')
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

  it('shows no actions when the card cannot act', () => {
    renderTab({}, false)

    expect(screen.queryByRole('button', { name: /^(Pay|Unmark) / })).not.toBeInTheDocument()
  })

  it('shows the pay form with the amount read-only, its errors and its saving state', () => {
    renderTab({
      payForm: {
        statement: OCTOBER,
        bankId: '',
        date: '2026-10-16',
        isSaving: false,
        error: 'This statement is already paid.',
        fieldErrors: { bankId: 'Select a bank', date: 'Date must not be in the future.' },
      },
    })

    expect(screen.getByRole('heading', { name: 'Pay October 2026 statement' })).toBeInTheDocument()
    expect(screen.getByText('This statement is already paid.')).toBeInTheDocument()
    expect(screen.getByText('Select a bank', { selector: '[id]' })).toBeInTheDocument()
    expect(screen.getByText('Date must not be in the future.')).toBeInTheDocument()
    expect(screen.getAllByText('210.45')).toHaveLength(2)
    expect(screen.getByLabelText(/^Bank/)).toHaveFocus()
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
