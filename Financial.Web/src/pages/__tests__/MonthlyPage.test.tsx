import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MonthlyPage from '../MonthlyPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankBalanceDto, BankDto, CardStatementDto, CategoryTotalDto, ExpenseDto, IncomeDto } from '../../api/types'

const getExpensesByMonthMock = vi.fn<FinancialApiClient['getExpensesByMonth']>()
const getCategoryTotalsByMonthMock = vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>()
const getCardStatementsByMonthMock = vi.fn<FinancialApiClient['getCardStatementsByMonth']>()
const getBanksMock = vi.fn<FinancialApiClient['getBanks']>()
const createExpenseMock = vi.fn<FinancialApiClient['createExpense']>()
const updateExpenseMock = vi.fn<FinancialApiClient['updateExpense']>()
const deleteExpenseMock = vi.fn<FinancialApiClient['deleteExpense']>()
const markCardStatementPaidMock = vi.fn<FinancialApiClient['markCardStatementPaid']>()
const unmarkCardStatementPaidMock = vi.fn<FinancialApiClient['unmarkCardStatementPaid']>()
const getIncomesByMonthMock = vi.fn<FinancialApiClient['getIncomesByMonth']>()
const createIncomeMock = vi.fn<FinancialApiClient['createIncome']>()
const updateIncomeMock = vi.fn<FinancialApiClient['updateIncome']>()
const deleteIncomeMock = vi.fn<FinancialApiClient['deleteIncome']>()
const getBankBalancesByMonthMock = vi.fn<FinancialApiClient['getBankBalancesByMonth']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getExpensesByMonth: getExpensesByMonthMock,
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getCardStatementsByMonth: getCardStatementsByMonthMock,
    getBanks: getBanksMock,
    createExpense: createExpenseMock,
    updateExpense: updateExpenseMock,
    deleteExpense: deleteExpenseMock,
    markCardStatementPaid: markCardStatementPaidMock,
    unmarkCardStatementPaid: unmarkCardStatementPaidMock,
    getIncomesByMonth: getIncomesByMonthMock,
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
    deleteIncome: deleteIncomeMock,
    getBankBalancesByMonth: getBankBalancesByMonthMock,
  }),
}))

const BANKS: BankDto[] = [
  { name: 'Barclays', roundUpEnabled: false },
  { name: 'Trading212', roundUpEnabled: true },
  { name: 'Chase', roundUpEnabled: true },
]

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: '2026-07-05',
    description: 'Lidl UK',
    value: 42.5,
    category: 'Mercado',
    paymentSource: 'Barclays',
    cardTag: null,
    settledAt: null,
    paymentStatus: 'ImmediatePayment',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
  },
]

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', card: 'BaAmex', year: 2026, month: 7, isPaid: false, outstandingTotal: 100 },
  { id: 'c2', card: 'ChaseMaster4023', year: 2026, month: 7, isPaid: true, outstandingTotal: 0 },
]

const INCOMES: IncomeDto[] = [
  { id: 'i1', date: '2026-07-01', incomeSource: 'Gleison', grossValue: 3200, netValue: 2450, bank: 'Barclays' },
]

const BANK_BALANCES: BankBalanceDto[] = [
  { bank: 'Barclays', balance: 42.5 },
  { bank: 'Trading212', balance: 0 },
  { bank: 'Chase', balance: 0 },
]

describe('MonthlyPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    getBanksMock.mockResolvedValue(BANKS)
    getIncomesByMonthMock.mockResolvedValue(INCOMES)
    getBankBalancesByMonthMock.mockResolvedValue(BANK_BALANCES)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('shows a loading state before data arrives', () => {
    render(<MonthlyPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getExpensesByMonthMock.mockRejectedValue(new Error('Network down'))

    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('renders category totals, card statements with the combined adjustment figure, and the expense list together', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    expect(screen.getByText('Category Totals')).toBeInTheDocument()
    expect(screen.getAllByText('Mercado').length).toBeGreaterThan(0)
    expect(screen.getByText('Cards')).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
    expect(screen.getAllByText('100.00').length).toBeGreaterThan(0)
  })

  it('renders a Banks grid with a row per payment source and its own total, alongside the other grids', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    expect(screen.getByText('Banks')).toBeInTheDocument()

    const banksSection = within(screen.getByText('Banks').closest('section')!)
    expect(banksSection.getByRole('cell', { name: 'Barclays' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Trading212' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Chase' })).toBeInTheDocument()

    // The single expense (42.50) is on Barclays with no round-up amount, so its balance
    // is unchanged and every round-up figure (per-bank and the footer) is zero.
    expect(banksSection.getAllByText('42.50').length).toBe(2)
    expect(banksSection.getAllByText('0.00').length).toBe(6)

    expect(screen.getByText('Category Totals').closest('section')).toHaveClass('monthly-page__section--grid')
    expect(screen.getByText('Cards').closest('section')).toHaveClass('monthly-page__section--grid')
    expect(screen.getByText('Banks').closest('section')).toHaveClass('monthly-page__section--grid')
  })

  it('shows Mark Paid with a bank picker for unpaid cards and Unmark Paid for paid ones', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getAllByRole('button', { name: 'Mark Paid' })).toHaveLength(1)
    expect(screen.getByLabelText('Paying bank for BaAmex')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: 'Unmark Paid' })).toHaveLength(1)
  })

  it('disables Mark Paid until a bank is selected, then marks paid with that bank', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const markPaidButton = screen.getByRole('button', { name: 'Mark Paid' })
    expect(markPaidButton).toBeDisabled()

    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'Trading212' } })
    expect(markPaidButton).toBeEnabled()
    fireEvent.click(markPaidButton)

    await waitFor(() =>
      expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSource: 'Trading212' }),
    )
  })

  it('unmarks a paid statement after confirmation', async () => {
    unmarkCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[1], isPaid: false, outstandingTotal: 0 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseMaster4023' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Unmark Paid' }))

    await waitFor(() => expect(unmarkCardStatementPaidMock).toHaveBeenCalledWith('c2'))
  })

  it('bank mode shows only the bank picker and card mode only the card picker', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    expect(screen.getByLabelText('Payment Source')).toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('radio', { name: 'Charge to card' }))

    expect(screen.queryByLabelText('Payment Source')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Card')).toBeInTheDocument()
  })

  it('submits a card-mode expense with a null payment source', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Amazon' } })
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '9.99' } })
    fireEvent.click(screen.getByRole('radio', { name: 'Charge to card' }))
    fireEvent.change(screen.getByLabelText('Card'), { target: { value: 'BaAmex' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSource: null, cardTag: 'BaAmex' }),
      ),
    )
  })

  it('shows read-only payment fields with a settlement note when editing a settled expense', async () => {
    getExpensesByMonthMock.mockResolvedValue([
      {
        ...EXPENSES[0],
        paymentSource: 'Trading212',
        cardTag: 'BaAmex',
        settledAt: '2026-07-20',
        paymentStatus: 'CreditCardSettled',
      },
    ])
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])

    expect(screen.getByText(/Settled via its card statement/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Payment Source')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows the add-expense form only after New Expense is clicked, and submits a new expense', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    expect(screen.queryByLabelText('Date')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Waitrose' } })
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '15.5' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ description: 'Waitrose', value: 15.5 })),
    )
  })

  it('edits an expense value via the toggled panel and saves, updating the displayed row', async () => {
    updateExpenseMock.mockResolvedValue({ ...EXPENSES[0], value: 50 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])
    expect(screen.getByText('Edit Expense')).toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('42.5')
    fireEvent.change(valueInput, { target: { value: '50' } })

    getExpensesByMonthMock.mockResolvedValue([{ ...EXPENSES[0], value: 50 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateExpenseMock).toHaveBeenCalledWith('e1', expect.objectContaining({ value: 50 })))
    const expensesSection = within(screen.getByText('Expenses').closest('section')!)
    await waitFor(() => expect(expensesSection.getByText('50.00')).toBeInTheDocument())
  })

  it('deletes an expense after confirmation', async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete expense' })[0])

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e1'))
  })

  it('renders the income list alongside the expense list', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Income')).toBeInTheDocument())
    const incomeSection = within(screen.getByText('Income').closest('section')!)
    expect(incomeSection.getByText('Gleison')).toBeInTheDocument()
    expect(incomeSection.getByText('2,450.00')).toBeInTheDocument()
  })

  it('shows the add-income form only after New Income is clicked, and submits a new income entry', async () => {
    createIncomeMock.mockResolvedValue({ ...INCOMES[0], id: 'i2' })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    expect(screen.queryByLabelText('Net Value')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText('Net Value'), { target: { value: '400' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() =>
      expect(createIncomeMock).toHaveBeenCalledWith(
        expect.objectContaining({ date: '2026-07-16', incomeSource: 'Gleison', netValue: 400, bank: 'Barclays' }),
      ),
    )
  })

  it('hides the gross value field for Lottery and DividendoJuros sources', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'Lottery' } })

    expect(screen.queryByLabelText('Gross Value')).not.toBeInTheDocument()
  })

  it('opening New Income closes an open expense form, and vice versa', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    expect(screen.getByRole('button', { name: 'Add Expense' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    expect(screen.queryByRole('button', { name: 'Add Expense' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add Income' })).toBeInTheDocument()
  })

  it('shows a validation error and does not call the API when no bank is available to select', async () => {
    getBanksMock.mockResolvedValue([])
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText('Net Value'), { target: { value: '400' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    expect(await screen.findByText('Bank is required')).toBeInTheDocument()
    expect(createIncomeMock).not.toHaveBeenCalled()
  })

  it('edits an income entry via the toggled panel and saves, updating the displayed row', async () => {
    updateIncomeMock.mockResolvedValue({ ...INCOMES[0], netValue: 500 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit income' })[0])
    expect(screen.getByText('Edit Income')).toBeInTheDocument()
    const netValueInput = screen.getByDisplayValue('2450')
    fireEvent.change(netValueInput, { target: { value: '500' } })

    getIncomesByMonthMock.mockResolvedValue([{ ...INCOMES[0], netValue: 500 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ netValue: 500 })))
    const incomeSection = within(screen.getByText('Income').closest('section')!)
    await waitFor(() => expect(incomeSection.getByText('500.00')).toBeInTheDocument())
  })

  it('deletes an income entry after confirmation', async () => {
    deleteIncomeMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete income' })[0])

    await waitFor(() => expect(deleteIncomeMock).toHaveBeenCalledWith('i1'))
  })

  it('bank picker and mark-paid picker list banks fetched from the API', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    const bankPicker = screen.getByLabelText('Payment Source')
    expect(within(bankPicker).getByRole('option', { name: 'Barclays' })).toBeInTheDocument()
    expect(within(bankPicker).getByRole('option', { name: 'Trading212' })).toBeInTheDocument()
    expect(within(bankPicker).getByRole('option', { name: 'Chase' })).toBeInTheDocument()

    const markPaidPicker = screen.getByLabelText('Paying bank for BaAmex')
    expect(within(markPaidPicker).getByRole('option', { name: 'Trading212' })).toBeInTheDocument()
  })

  it('shows a pre-filled round-up field when a round-up-enabled bank is selected', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '9.40' } })

    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Payment Source'), { target: { value: 'Trading212' } })

    expect(screen.getByLabelText('Round-Up')).toHaveValue(0.6)
  })

  it('hides the round-up field for a non-round-up bank and for card mode', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '9.40' } })
    fireEvent.change(screen.getByLabelText('Payment Source'), { target: { value: 'Trading212' } })
    expect(screen.getByLabelText('Round-Up')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Payment Source'), { target: { value: 'Barclays' } })
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('radio', { name: 'Charge to card' }))
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()
  })

  it('submits a typed round-up amount with the new expense', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'TfL' } })
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '9.40' } })
    fireEvent.change(screen.getByLabelText('Payment Source'), { target: { value: 'Trading212' } })
    fireEvent.change(screen.getByLabelText('Round-Up'), { target: { value: '0.10' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ roundUpAmount: 0.1 })),
    )
  })

  it('shows a bank balance reduced by its round-up total, in a separate column', async () => {
    getExpensesByMonthMock.mockResolvedValue([
      { ...EXPENSES[0], paymentSource: 'Trading212', value: 9.4, roundUpAmount: 0.6 },
    ])
    getBankBalancesByMonthMock.mockResolvedValue([
      { bank: 'Barclays', balance: 0 },
      { bank: 'Trading212', balance: 8.8 },
      { bank: 'Chase', balance: 0 },
    ])
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    const banksSection = within(screen.getByText('Banks').closest('section')!)

    const trading212Row = banksSection.getByRole('row', { name: /Trading212/ })
    expect(within(trading212Row).getByRole('cell', { name: '8.80' })).toBeInTheDocument()
    expect(within(trading212Row).getByRole('cell', { name: '0.60' })).toBeInTheDocument()

    const barclaysRow = banksSection.getByRole('row', { name: /Barclays/ })
    expect(within(barclaysRow).getAllByRole('cell', { name: '0.00' })).toHaveLength(2)
  })

  it('pre-fills the edit round-up field with the saved amount', async () => {
    getExpensesByMonthMock.mockResolvedValue([
      { ...EXPENSES[0], paymentSource: 'Trading212', roundUpAmount: 0.6, suggestedRoundUpAmount: null },
    ])
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])

    expect(screen.getByLabelText('Round-Up')).toHaveValue(0.6)
  })
})
