import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MonthlyPage from '../MonthlyPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  BalanceAdjustmentDto,
  BankBalanceDto,
  BankDto,
  CardStatementDto,
  CategoryTotalDto,
  ExpenseDto,
  IncomeDto,
  TitheSummaryDto,
  TransferDto,
} from '../../api/types'

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
const getTitheSummaryByMonthMock = vi.fn<FinancialApiClient['getTitheSummaryByMonth']>()
const getTransfersByMonthMock = vi.fn<FinancialApiClient['getTransfersByMonth']>()
const createTransferMock = vi.fn<FinancialApiClient['createTransfer']>()
const updateTransferMock = vi.fn<FinancialApiClient['updateTransfer']>()
const deleteTransferMock = vi.fn<FinancialApiClient['deleteTransfer']>()
const getAdjustmentsByBankMock = vi.fn<FinancialApiClient['getAdjustmentsByBank']>()
const createBalanceAdjustmentMock = vi.fn<FinancialApiClient['createBalanceAdjustment']>()
const updateBalanceAdjustmentMock = vi.fn<FinancialApiClient['updateBalanceAdjustment']>()
const deleteBalanceAdjustmentMock = vi.fn<FinancialApiClient['deleteBalanceAdjustment']>()

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
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    getTransfersByMonth: getTransfersByMonthMock,
    createTransfer: createTransferMock,
    updateTransfer: updateTransferMock,
    deleteTransfer: deleteTransferMock,
    getAdjustmentsByBank: getAdjustmentsByBankMock,
    createBalanceAdjustment: createBalanceAdjustmentMock,
    updateBalanceAdjustment: updateBalanceAdjustmentMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
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

const TITHE_SUMMARY: TitheSummaryDto = { calculatedTithe: 245, titheBalance: 245 }

const TRANSFERS: TransferDto[] = [
  {
    id: 't1',
    date: '2026-07-10',
    sourceBank: 'Barclays',
    destinationBank: 'Trading212',
    amount: 100,
    note: 'Top-up',
  },
]

const ADJUSTMENTS: BalanceAdjustmentDto[] = [
  { id: 'a1', date: '2026-07-12', bank: 'Barclays', targetBalance: 42.5, delta: 5, note: 'Matched statement' },
]

describe('MonthlyPage', () => {
  beforeEach(() => {
    getExpensesByMonthMock.mockReset()
    getCategoryTotalsByMonthMock.mockReset()
    getCardStatementsByMonthMock.mockReset()
    getBanksMock.mockReset()
    createExpenseMock.mockReset()
    updateExpenseMock.mockReset()
    deleteExpenseMock.mockReset()
    markCardStatementPaidMock.mockReset()
    unmarkCardStatementPaidMock.mockReset()
    getIncomesByMonthMock.mockReset()
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    deleteIncomeMock.mockReset()
    getBankBalancesByMonthMock.mockReset()
    getTitheSummaryByMonthMock.mockReset()
    getTransfersByMonthMock.mockReset()
    createTransferMock.mockReset()
    updateTransferMock.mockReset()
    deleteTransferMock.mockReset()
    getAdjustmentsByBankMock.mockReset()
    createBalanceAdjustmentMock.mockReset()
    updateBalanceAdjustmentMock.mockReset()
    deleteBalanceAdjustmentMock.mockReset()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    getBanksMock.mockResolvedValue(BANKS)
    getIncomesByMonthMock.mockResolvedValue(INCOMES)
    getBankBalancesByMonthMock.mockResolvedValue(BANK_BALANCES)
    getTitheSummaryByMonthMock.mockResolvedValue(TITHE_SUMMARY)
    getTransfersByMonthMock.mockResolvedValue(TRANSFERS)
    getAdjustmentsByBankMock.mockImplementation((bankName: string) =>
      Promise.resolve(bankName === 'Barclays' ? ADJUSTMENTS : []),
    )
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

  it('shows the error/retry state regardless of the active tab', async () => {
    getExpensesByMonthMock.mockRejectedValue(new Error('Network down'))

    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('defaults to the Summary tab on load, showing only the total grids', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getByText(/^Total:/)).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
    expect(screen.getByText(/Bank Balance:/)).toBeInTheDocument()
    expect(screen.getByText(/Total Incoming:/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New Expense' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New Income' })).not.toBeInTheDocument()
  })

  it('marks Summary as the active tab button by default', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Summary' })).toHaveClass('monthly-page__tab--active')
    expect(screen.getByRole('button', { name: 'Expense' })).not.toHaveClass('monthly-page__tab--active')
    expect(screen.getByRole('button', { name: 'Incoming' })).not.toHaveClass('monthly-page__tab--active')
  })

  it('re-scopes all 4 Summary grids when the month/year value changes', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getAllByText('Mercado').length).toBeGreaterThan(0)

    getCategoryTotalsByMonthMock.mockResolvedValue([{ category: 'Viagem', totalValue: 300 }])
    getCardStatementsByMonthMock.mockResolvedValue([
      { id: 'c3', card: 'PaypalCredit', year: 2026, month: 8, isPaid: false, outstandingTotal: 55 },
    ])
    getBankBalancesByMonthMock.mockResolvedValue([{ bank: 'Barclays', balance: 300 }])
    getIncomesByMonthMock.mockResolvedValue([
      { id: 'i2', date: '2026-08-01', incomeSource: 'Lottery', grossValue: null, netValue: 50, bank: 'Barclays' },
    ])

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(screen.getByText('Viagem')).toBeInTheDocument())
    expect(screen.queryByText('Mercado')).not.toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'PaypalCredit' })).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'Lottery' })).toBeInTheDocument()
  })

  it('shows only the Expense tabs content after clicking Expense', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    expect(screen.queryByText(/^Total:/)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New Income' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument()
    expect(screen.getByText('Lidl UK')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Expense' })).toHaveClass('monthly-page__tab--active')
  })

  it('shows only the Incoming tabs content after clicking Incoming', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    expect(screen.queryByText(/^Total:/)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New Expense' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument()
    expect(screen.getByText('Gleison')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Incoming' })).toHaveClass('monthly-page__tab--active')
  })

  it('does not change the month/year picker value when switching tabs', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const monthInput = screen.getByLabelText('Month') as HTMLInputElement
    const valueBefore = monthInput.value

    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    expect(monthInput.value).toBe(valueBefore)
  })

  it('does not refetch data when switching tabs', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const callCountBefore = getExpensesByMonthMock.mock.calls.length

    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))
    fireEvent.click(screen.getByRole('button', { name: 'Summary' }))

    expect(getExpensesByMonthMock.mock.calls.length).toBe(callCountBefore)
  })

  it('keeps the active tab unchanged when the month/year value changes', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Expense' })).toHaveClass('monthly-page__tab--active')
  })

  it('closes an open create form when switching tabs away and back', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    expect(screen.getByRole('button', { name: 'Add Expense' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Summary' }))
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    expect(screen.queryByRole('button', { name: 'Add Expense' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument()
  })

  it('re-scopes the expense list when the month/year value changes while Expense is active', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())

    getExpensesByMonthMock.mockResolvedValue([{ ...EXPENSES[0], id: 'e3', description: 'TfL Top-Up' }])

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(screen.getByText('TfL Top-Up')).toBeInTheDocument())
    expect(screen.queryByText('Lidl UK')).not.toBeInTheDocument()
  })

  it('renders category totals and card statements with the combined adjustment on Summary, and the expense list on the Expense tab', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getByText(/^Total:/)).toBeInTheDocument()
    expect(screen.getAllByText('Mercado').length).toBeGreaterThan(0)
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
    expect(screen.getAllByText('100.00').length).toBeGreaterThan(0)

    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))
    expect(screen.getByText('Lidl UK')).toBeInTheDocument()
  })

  it('renders a Banks grid with a row per payment source and its own total, alongside the other grids', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())

    const banksSection = within(screen.getByText(/Bank Balance:/).closest('section')!)
    expect(banksSection.getByRole('cell', { name: 'Barclays' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Trading212' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Chase' })).toBeInTheDocument()

    // The single expense (42.50) is on Barclays with no round-up amount, so its balance
    // is unchanged and every round-up figure (per-bank and the footer) is zero.
    expect(banksSection.getAllByText('42.50').length).toBe(2)
    expect(banksSection.getAllByText('0.00').length).toBe(6)

    expect(screen.getByText(/^Total:/).closest('section')).toHaveClass('monthly-page__section--grid')
    expect(screen.getByText(/Combined adjustment figure/).closest('section')).toHaveClass('monthly-page__section--grid')
    expect(screen.getByText(/Bank Balance:/).closest('section')).toHaveClass('monthly-page__section--grid')
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

  it('renders Category Totals and Cards in the first Summary row, Banks and Incoming in the second', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())

    const groups = document.querySelector('.monthly-page__summary-groups')
    expect(groups).not.toBeNull()
    const rows = groups!.querySelectorAll(':scope > .monthly-page__grids-row')
    expect(rows).toHaveLength(2)

    const firstRow = within(rows[0] as HTMLElement)
    expect(firstRow.getByText(/^Total:/)).toBeInTheDocument()
    expect(firstRow.getByText(/Combined adjustment figure/)).toBeInTheDocument()

    const secondRow = within(rows[1] as HTMLElement)
    expect(secondRow.getByText(/Bank Balance:/)).toBeInTheDocument()
    expect(secondRow.getByText(/Total Incoming:/)).toBeInTheDocument()

    expect(groups!.querySelectorAll(':scope > :not(.monthly-page__grids-row)')).toHaveLength(0)
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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])
    expect(screen.getByText('Edit Expense')).toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('42.5')
    fireEvent.change(valueInput, { target: { value: '50' } })

    getExpensesByMonthMock.mockResolvedValue([{ ...EXPENSES[0], value: 50 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateExpenseMock).toHaveBeenCalledWith('e1', expect.objectContaining({ value: 50 })))
    const expensesSection = within(screen.getByRole('button', { name: 'New Expense' }).closest('section')!)
    await waitFor(() => expect(expensesSection.getByText('50.00')).toBeInTheDocument())
  })

  it('deletes an expense after confirmation', async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete expense' })[0])

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e1'))
  })

  it('renders the income list on the Incoming tab', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    const incomeSection = within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    expect(incomeSection.getByText('Gleison')).toBeInTheDocument()
    expect(incomeSection.getByText('2,450.00')).toBeInTheDocument()
  })

  it('re-scopes the income list when the month/year value changes while Incoming is active', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    getIncomesByMonthMock.mockResolvedValue([
      { id: 'i3', date: '2026-08-01', incomeSource: 'Lottery', grossValue: null, netValue: 75, bank: 'Barclays' },
    ])

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(screen.getByText('Lottery')).toBeInTheDocument())
    expect(screen.queryByText('Gleison')).not.toBeInTheDocument()
  })

  it('shows the add-income form only after New Income is clicked, and submits a new income entry', async () => {
    createIncomeMock.mockResolvedValue({ ...INCOMES[0], id: 'i2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'Lottery' } })

    expect(screen.queryByLabelText('Gross Value')).not.toBeInTheDocument()
  })

  it('shows a validation error and does not call the API when no bank is available to select', async () => {
    getBanksMock.mockResolvedValue([])
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Edit income' }).length).toBeGreaterThan(0))

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit income' })[0])
    expect(screen.getByText('Edit Income')).toBeInTheDocument()
    const netValueInput = screen.getByDisplayValue('2450')
    fireEvent.change(netValueInput, { target: { value: '500' } })

    getIncomesByMonthMock.mockResolvedValue([{ ...INCOMES[0], netValue: 500 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ netValue: 500 })))
    const incomeSection = within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    await waitFor(() => expect(incomeSection.getByText('500.00')).toBeInTheDocument())
  })

  it('deletes an income entry after confirmation', async () => {
    deleteIncomeMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Delete income' }).length).toBeGreaterThan(0))
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete income' })[0])

    await waitFor(() => expect(deleteIncomeMock).toHaveBeenCalledWith('i1'))
  })

  it('renders the Incoming card with one row per source and the calculated tithe and tithe balance', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText(/Total Incoming:/)).toBeInTheDocument())
    const incomingSection = within(screen.getByText(/Total Incoming:/).closest('section')!)
    expect(incomingSection.getByRole('cell', { name: 'Gleison' })).toBeInTheDocument()
    expect(incomingSection.getByText('3,200.00')).toBeInTheDocument()
    expect(incomingSection.getAllByText('2,450.00').length).toBeGreaterThanOrEqual(1)
    expect(incomingSection.getByText(/Total Incoming:/)).toBeInTheDocument()
    expect(incomingSection.getByText(/Calculated Tithe:/)).toBeInTheDocument()
    expect(incomingSection.getByText(/Tithe Balance:/)).toBeInTheDocument()
  })

  it('updates the Incoming card after a new income entry is added', async () => {
    createIncomeMock.mockResolvedValue({ id: 'i2', date: '2026-07-15', incomeSource: 'Lottery', grossValue: null, netValue: 100, bank: 'Chase' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Incoming' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-15' } })
    fireEvent.change(screen.getByLabelText('Source'), { target: { value: 'Lottery' } })
    fireEvent.change(screen.getByLabelText('Net Value'), { target: { value: '100' } })

    getIncomesByMonthMock.mockResolvedValue([...INCOMES, {
      id: 'i2',
      date: '2026-07-15',
      incomeSource: 'Lottery',
      grossValue: null,
      netValue: 100,
      bank: 'Chase',
    }])
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() => expect(createIncomeMock).toHaveBeenCalled())
    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Summary' }))
    const incomingSection = within(screen.getByText(/Total Incoming:/).closest('section')!)
    await waitFor(() => expect(incomingSection.getByRole('cell', { name: 'Lottery' })).toBeInTheDocument())
  })

  it('bank picker lists banks fetched from the API in the expense form', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    const bankPicker = screen.getByLabelText('Payment Source')
    expect(within(bankPicker).getByRole('option', { name: 'Barclays' })).toBeInTheDocument()
    expect(within(bankPicker).getByRole('option', { name: 'Trading212' })).toBeInTheDocument()
    expect(within(bankPicker).getByRole('option', { name: 'Chase' })).toBeInTheDocument()
  })

  it('mark-paid picker on the Cards grid lists banks fetched from the API', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const markPaidPicker = screen.getByLabelText('Paying bank for BaAmex')
    expect(within(markPaidPicker).getByRole('option', { name: 'Trading212' })).toBeInTheDocument()
  })

  it('shows a pre-filled round-up field when a round-up-enabled bank is selected', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '9.40' } })

    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Payment Source'), { target: { value: 'Trading212' } })

    expect(screen.getByLabelText('Round-Up')).toHaveValue(0.6)
  })

  it('hides the round-up field for a non-round-up bank and for card mode', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

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

    await waitFor(() => expect(screen.getByText(/Bank Balance:/)).toBeInTheDocument())
    const banksSection = within(screen.getByText(/Bank Balance:/).closest('section')!)

    const trading212Row = await banksSection.findByRole('row', { name: /Trading212/ })
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
    fireEvent.click(screen.getByRole('button', { name: 'Expense' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])

    expect(screen.getByLabelText('Round-Up')).toHaveValue(0.6)
  })

  it('opens Move Money from a bank row pre-filled with that bank as the source, and refreshes balances and history on save', async () => {
    createTransferMock.mockResolvedValue(TRANSFERS[0])
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const trading212Row = screen.getByRole('row', { name: /Trading212/ })
    fireEvent.click(within(trading212Row).getByRole('button', { name: 'Move Money' }))

    expect(screen.getByText('Move Money', { selector: 'p' })).toBeInTheDocument()
    expect(screen.getByLabelText('From')).toHaveValue('Trading212')

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    const transfersCallsBefore = getTransfersByMonthMock.mock.calls.length

    fireEvent.change(screen.getByLabelText('To'), { target: { value: 'Barclays' } })
    fireEvent.change(screen.getByLabelText('Amount'), { target: { value: '50' } })
    const transferFormPanel = screen.getByLabelText('From').closest('.monthly-page__form-panel') as HTMLElement
    fireEvent.click(within(transferFormPanel).getByRole('button', { name: 'Move Money' }))

    await waitFor(() => expect(createTransferMock).toHaveBeenCalled())
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
    expect(getTransfersByMonthMock.mock.calls.length).toBeGreaterThan(transfersCallsBefore)
  })

  it('opens Correct Balance from a bank row with the current balance, and refreshes balances and history on save', async () => {
    createBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENTS[0], id: 'a2', delta: 2.5 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const barclaysRow = screen.getByRole('row', { name: /Barclays/ })
    fireEvent.click(within(barclaysRow).getByRole('button', { name: 'Correct Balance' }))

    expect(screen.getByText(/Current calculated balance for Barclays: £42.50/)).toBeInTheDocument()

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    const adjustmentsCallsBefore = getAdjustmentsByBankMock.mock.calls.length

    fireEvent.change(screen.getByLabelText('Target Balance'), { target: { value: '45' } })
    const adjustmentFormPanel = screen.getByLabelText('Target Balance').closest('.monthly-page__form-panel') as HTMLElement
    fireEvent.click(within(adjustmentFormPanel).getByRole('button', { name: 'Correct Balance' }))

    await waitFor(() => expect(createBalanceAdjustmentMock).toHaveBeenCalledWith('Barclays', expect.objectContaining({ targetBalance: 45 })))
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
    expect(getAdjustmentsByBankMock.mock.calls.length).toBeGreaterThan(adjustmentsCallsBefore)
  })

  it('expands a bank row to show its transfer and adjustment history', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expand history for Barclays' }))

    expect(await screen.findByText('Transfer Out')).toBeInTheDocument()
    expect(screen.getByText('Adjustment')).toBeInTheDocument()
  })

  it('edits a transfer from the history list, opening TransferForm pre-filled with its values', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expand history for Barclays' }))
    await screen.findByText('Transfer Out')

    fireEvent.click(screen.getByRole('button', { name: 'Edit transfer' }))

    expect(screen.getByText('Edit Transfer')).toBeInTheDocument()
    expect(screen.getByLabelText('Amount')).toHaveValue(100)
  })

  it('edits an adjustment from the history list, opening BalanceAdjustmentForm pre-filled with its values', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expand history for Barclays' }))
    await screen.findByText('Adjustment')

    fireEvent.click(screen.getByRole('button', { name: 'Edit balance adjustment' }))

    expect(screen.getByText('Edit Balance Adjustment')).toBeInTheDocument()
    expect(screen.getByLabelText('Target Balance')).toHaveValue(42.5)
  })

  it('deletes a transfer from the history list after confirmation, and refreshes balances', async () => {
    deleteTransferMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expand history for Barclays' }))
    await screen.findByText('Transfer Out')

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    fireEvent.click(screen.getByRole('button', { name: 'Delete transfer' }))

    await waitFor(() => expect(deleteTransferMock).toHaveBeenCalledWith('t1'))
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
  })

  it('deletes an adjustment from the history list after confirmation, and refreshes balances', async () => {
    deleteBalanceAdjustmentMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Expand history for Barclays' }))
    await screen.findByText('Adjustment')

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    fireEvent.click(screen.getByRole('button', { name: 'Delete balance adjustment' }))

    await waitFor(() => expect(deleteBalanceAdjustmentMock).toHaveBeenCalledWith('Barclays', 'a1'))
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
  })
})
