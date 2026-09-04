import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import MonthlyPage from '../MonthlyPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type {
  BalanceAdjustmentDto,
  BankBalanceDto,
  BankDto,
  CardStatementDto,
  CategoryDto,
  CategoryTotalDto,
  CreditCardDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
  TitheSummaryDto,
  TransferDto,
} from '../../api/types'

const {
  getExpensesByMonthMock,
  getUnpaidCardChargesByMonthMock,
  getCategoryTotalsByMonthMock,
  getCardStatementsByMonthMock,
  getBanksMock,
  getIncomeSourcesMock,
  getCategoriesMock,
  getCreditCardsMock,
  updateCreditCardMock,
  createExpenseMock,
  updateExpenseMock,
  deleteExpenseMock,
  markCardStatementPaidMock,
  unmarkCardStatementPaidMock,
  getIncomesByMonthMock,
  createIncomeMock,
  updateIncomeMock,
  deleteIncomeMock,
  getBankBalancesByMonthMock,
  getTitheSummaryByMonthMock,
  updateTitheCarryForwardMock,
  getTransfersByMonthMock,
  createTransferMock,
  updateTransferMock,
  deleteTransferMock,
  getAdjustmentsByBankMock,
  createBalanceAdjustmentMock,
  updateBalanceAdjustmentMock,
  deleteBalanceAdjustmentMock,
} = vi.hoisted(() => ({
  getExpensesByMonthMock: vi.fn<FinancialApiClient['getExpensesByMonth']>(),
  getUnpaidCardChargesByMonthMock: vi.fn<FinancialApiClient['getUnpaidCardChargesByMonth']>(),
  getCategoryTotalsByMonthMock: vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>(),
  getCardStatementsByMonthMock: vi.fn<FinancialApiClient['getCardStatementsByMonth']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  getCreditCardsMock: vi.fn<FinancialApiClient['getCreditCards']>(),
  updateCreditCardMock: vi.fn<FinancialApiClient['updateCreditCard']>(),
  createExpenseMock: vi.fn<FinancialApiClient['createExpense']>(),
  updateExpenseMock: vi.fn<FinancialApiClient['updateExpense']>(),
  deleteExpenseMock: vi.fn<FinancialApiClient['deleteExpense']>(),
  markCardStatementPaidMock: vi.fn<FinancialApiClient['markCardStatementPaid']>(),
  unmarkCardStatementPaidMock: vi.fn<FinancialApiClient['unmarkCardStatementPaid']>(),
  getIncomesByMonthMock: vi.fn<FinancialApiClient['getIncomesByMonth']>(),
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
  updateIncomeMock: vi.fn<FinancialApiClient['updateIncome']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
  getBankBalancesByMonthMock: vi.fn<FinancialApiClient['getBankBalancesByMonth']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
  getTransfersByMonthMock: vi.fn<FinancialApiClient['getTransfersByMonth']>(),
  createTransferMock: vi.fn<FinancialApiClient['createTransfer']>(),
  updateTransferMock: vi.fn<FinancialApiClient['updateTransfer']>(),
  deleteTransferMock: vi.fn<FinancialApiClient['deleteTransfer']>(),
  getAdjustmentsByBankMock: vi.fn<FinancialApiClient['getAdjustmentsByBank']>(),
  createBalanceAdjustmentMock: vi.fn<FinancialApiClient['createBalanceAdjustment']>(),
  updateBalanceAdjustmentMock: vi.fn<FinancialApiClient['updateBalanceAdjustment']>(),
  deleteBalanceAdjustmentMock: vi.fn<FinancialApiClient['deleteBalanceAdjustment']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getExpensesByMonth: getExpensesByMonthMock,
    getUnpaidCardChargesByMonth: getUnpaidCardChargesByMonthMock,
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getCardStatementsByMonth: getCardStatementsByMonthMock,
    getBanks: getBanksMock,
    getIncomeSources: getIncomeSourcesMock,
    getCategories: getCategoriesMock,
    getCreditCards: getCreditCardsMock,
    updateCreditCard: updateCreditCardMock,
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
    updateTitheCarryForward: updateTitheCarryForwardMock,
    getTransfersByMonth: getTransfersByMonthMock,
    createTransfer: createTransferMock,
    updateTransfer: updateTransferMock,
    deleteTransfer: deleteTransferMock,
    getAdjustmentsByBank: getAdjustmentsByBankMock,
    createBalanceAdjustment: createBalanceAdjustmentMock,
    updateBalanceAdjustment: updateBalanceAdjustmentMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
  { id: 'bank-chase', name: 'Chase', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false, hasReferences: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true, hasReferences: false },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', autoSplitToReserve: false, hasReferences: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', autoSplitToReserve: false, hasReferences: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-extras', name: 'Extras', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-reserva', name: 'Reserva', active: false, isInvestment: false, isTithe: false, hasReferences: false },
]

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: '2026-07-05',
    description: 'Lidl UK',
    value: 42.5,
    categoryId: 'category-mercado',
    categoryName: 'Mercado',
    paymentSourceBankId: 'bank-barclays',
    paymentSourceBankName: 'Barclays',
    creditCardId: null,
    creditCardName: null,
    chargeDate: null,
    invoiceDate: null,
    paymentStatus: 'ImmediatePayment',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  },
]

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', creditCardId: 'card-baamex', creditCardName: 'BaAmex', year: 2026, month: 7, isPaid: false, outstandingTotal: 100, warning: null },
  { id: 'c2', creditCardId: 'card-chase', creditCardName: 'ChaseMaster4023', year: 2026, month: 7, isPaid: true, outstandingTotal: 0, warning: null },
]

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
  { id: 'card-chase', name: 'ChaseMaster4023', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
]

const UNPAID_CARD_CHARGES: ExpenseDto[] = [
  {
    id: 'e2',
    date: '2026-07-08',
    description: 'Uber',
    value: 18.4,
    categoryId: 'category-extras',
    categoryName: 'Extras',
    paymentSourceBankId: null,
    paymentSourceBankName: null,
    creditCardId: 'card-baamex',
    creditCardName: 'BaAmex',
    chargeDate: '2026-07-08',
    invoiceDate: '2026-07-01',
    paymentStatus: 'CreditCardCharge',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  },
]

const INCOMES: IncomeDto[] = [
  {
    id: 'i1',
    date: '2026-07-01',
    incomeSourceId: '1',
    incomeSourceName: 'Gleison',
    grossValue: 3200,
    netValue: 2450,
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    description: null,
    splitToReserve: false,
  },
]

const BANK_BALANCES: BankBalanceDto[] = [
  { bank: 'Barclays', balance: 42.5 },
  { bank: 'Trading212', balance: 0 },
  { bank: 'Chase', balance: 0 },
]

const TITHE_SUMMARY: TitheSummaryDto = { calculatedTithe: 245, titheBalance: 245, carryForward: null }

const TRANSFERS: TransferDto[] = [
  {
    id: 't1',
    date: '2026-07-10',
    sourceBankId: 'bank-barclays',
    sourceBankName: 'Barclays',
    destinationBankId: 'bank-trading212',
    destinationBankName: 'Trading212',
    amount: 100,
    note: 'Top-up',
  },
]

const ADJUSTMENTS: BalanceAdjustmentDto[] = [
  {
    id: 'a1',
    date: '2026-07-12',
    bankId: 'bank-barclays',
    bankName: 'Barclays',
    targetBalance: 42.5,
    delta: 5,
    note: 'Matched statement',
  },
]

describe('MonthlyPage', () => {
  beforeEach(() => {
    // Pin "now" to match the fixtures' July 2026 dates below, so useMonthly's default
    // year/month (computed fresh at mount from the real clock) doesn't drift out of scope
    // with adjustment/expense/income fixtures as real time passes.
    // shouldAdvanceTime keeps real timers ticking (so RTL's waitFor/findBy* polling still
    // resolves) while Date/new Date() stays pinned to the system time set below.
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.setSystemTime(new Date('2026-07-15T12:00:00Z'))
    getExpensesByMonthMock.mockReset()
    getUnpaidCardChargesByMonthMock.mockReset()
    getCategoryTotalsByMonthMock.mockReset()
    getCardStatementsByMonthMock.mockReset()
    getBanksMock.mockReset()
    getIncomeSourcesMock.mockReset()
    getCategoriesMock.mockReset()
    getCreditCardsMock.mockReset()
    updateCreditCardMock.mockReset()
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
    updateTitheCarryForwardMock.mockReset()
    getTransfersByMonthMock.mockReset()
    createTransferMock.mockReset()
    updateTransferMock.mockReset()
    deleteTransferMock.mockReset()
    getAdjustmentsByBankMock.mockReset()
    createBalanceAdjustmentMock.mockReset()
    updateBalanceAdjustmentMock.mockReset()
    deleteBalanceAdjustmentMock.mockReset()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getUnpaidCardChargesByMonthMock.mockResolvedValue(UNPAID_CARD_CHARGES)
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    getBanksMock.mockResolvedValue(BANKS)
    getIncomeSourcesMock.mockResolvedValue(INCOME_SOURCES)
    getCategoriesMock.mockResolvedValue(CATEGORIES)
    getCreditCardsMock.mockResolvedValue(CREDIT_CARDS)
    getIncomesByMonthMock.mockResolvedValue(INCOMES)
    getBankBalancesByMonthMock.mockResolvedValue(BANK_BALANCES)
    getTitheSummaryByMonthMock.mockResolvedValue(TITHE_SUMMARY)
    getTransfersByMonthMock.mockResolvedValue(TRANSFERS)
    getAdjustmentsByBankMock.mockImplementation((bankId: string) =>
      Promise.resolve(bankId === 'bank-barclays' ? ADJUSTMENTS : []),
    )
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    sessionStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
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
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

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
    expect(screen.getByRole('tab', { name: 'Summary' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tab', { name: 'Bank expenses' })).toHaveAttribute('aria-selected', 'false')
    expect(screen.getByRole('tab', { name: 'Income' })).toHaveAttribute('aria-selected', 'false')
  })

  it('lists Summary, Bank expenses, Credit Card expenses, Income, Bank balance adjustment in order in the tab strip', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const tabs = screen.getAllByRole('tab')
    const expectedOrder = ['Summary', 'Bank expenses', 'Credit Card expenses', 'Income', 'Bank balance adjustment']
    const actualOrder = expectedOrder.map((label) => tabs.indexOf(screen.getByRole('tab', { name: label })))
    expect(actualOrder).toEqual([0, 1, 2, 3, 4])
  })

  it('re-scopes all 4 Summary grids when the month/year value changes', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getAllByText('Mercado').length).toBeGreaterThan(0)

    getCategoryTotalsByMonthMock.mockResolvedValue([{ category: 'Viagem', totalValue: 300 }])
    getCardStatementsByMonthMock.mockResolvedValue([
      { id: 'c3', creditCardId: 'card-paypal', creditCardName: 'PaypalCredit', year: 2026, month: 8, isPaid: false, outstandingTotal: 55, warning: null },
    ])
    getBankBalancesByMonthMock.mockResolvedValue([{ bank: 'Barclays', balance: 300 }])
    getIncomesByMonthMock.mockResolvedValue([
      {
        id: 'i2',
        date: '2026-08-01',
        incomeSourceId: '3',
        incomeSourceName: 'Lottery',
        grossValue: null,
        netValue: 50,
        bankId: 'bank-barclays',
        bankName: 'Barclays',
        description: null,
        splitToReserve: false,
      },
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
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    expect(screen.queryByText(/^Total:/)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New Income' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument()
    expect(screen.getByText('Lidl UK')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Bank expenses' })).toHaveAttribute('aria-selected', 'true')
  })

  it('shows the same card statements on the Credit Card tab as on Summary', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    expect(screen.getAllByRole('cell', { name: 'BaAmex' }).length).toBeGreaterThan(0)
    expect(screen.getAllByRole('cell', { name: 'ChaseMaster4023' }).length).toBeGreaterThan(0)
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Credit Card expenses' })).toHaveAttribute('aria-selected', 'true')
  })

  it('shows the unpaid card charge list on the Credit Card tab below the totals grid', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    expect(screen.getByText('Uber')).toBeInTheDocument()
    expect(screen.getByText('18.40')).toBeInTheDocument()
    expect(screen.getByText('Extras')).toBeInTheDocument()
  })

  it("an expense's position in the Credit Card tab list is unchanged immediately before and after its invoice is marked paid", async () => {
    // The actual ordering fix lives server-side (ExpenseService sorts by InvoiceDate, not Date);
    // this test guards against the frontend introducing its own conflicting client-side sort.
    const chargedFirst = { ...UNPAID_CARD_CHARGES[0], id: 'e10', description: 'Charged First', creditCardId: 'card-baamex', creditCardName: 'BaAmex', chargeDate: '2026-07-05', date: '2026-07-05' }
    const chargedSecond = { ...UNPAID_CARD_CHARGES[0], id: 'e11', description: 'Charged Second', creditCardId: 'card-baamex', creditCardName: 'BaAmex', chargeDate: '2026-07-20', date: '2026-07-20' }
    getUnpaidCardChargesByMonthMock.mockResolvedValue([chargedSecond, chargedFirst])
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByText('Charged First')).toBeInTheDocument())
    const rowsBefore = screen.getAllByRole('row')
    const secondIndexBefore = rowsBefore.findIndex((r) => r.textContent?.includes('Charged Second'))
    const firstIndexBefore = rowsBefore.findIndex((r) => r.textContent?.includes('Charged First'))
    expect(secondIndexBefore).toBeLessThan(firstIndexBefore)

    // Mark the BaAmex statement paid: both charges settle and move to the general Expense
    // tab's list. The (now-fixed) server still orders them by InvoiceDate, so Second stays
    // ahead of First - exactly the relative position they held before settlement.
    getUnpaidCardChargesByMonthMock.mockResolvedValue([])
    getExpensesByMonthMock.mockResolvedValue([
      {
        ...chargedSecond,
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        date: '2026-08-03',
        paymentStatus: 'CreditCardSettled',
      },
      {
        ...chargedFirst,
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        date: '2026-08-03',
        paymentStatus: 'CreditCardSettled',
      },
    ])
    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'bank-trading212' } })
    fireEvent.click(screen.getByRole('button', { name: 'Mark Paid' }))
    await waitFor(() =>
      expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSourceBankId: 'bank-trading212' }),
    )

    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))
    await waitFor(() => expect(screen.getByText('Charged Second')).toBeInTheDocument())
    const rowsAfter = screen.getAllByRole('row')
    const secondIndexAfter = rowsAfter.findIndex((r) => r.textContent?.includes('Charged Second'))
    const firstIndexAfter = rowsAfter.findIndex((r) => r.textContent?.includes('Charged First'))
    expect(secondIndexAfter).toBeLessThan(firstIndexAfter)
  })

  it("editing a row from the Credit Card tab's list opens the shared edit form and saves", async () => {
    updateExpenseMock.mockResolvedValue({ ...UNPAID_CARD_CHARGES[0], value: 20 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByText('Uber')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Edit expense' }))
    expect(screen.getByText('Edit Expense')).toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
    fireEvent.change(screen.getByDisplayValue('18.4'), { target: { value: '20' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateExpenseMock).toHaveBeenCalledWith('e2', expect.objectContaining({ value: 20 })))
  })

  it("deleting a row from the Credit Card tab's list calls delete and refreshes", async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByText('Uber')).toBeInTheDocument())
    const callCountBefore = getUnpaidCardChargesByMonthMock.mock.calls.length
    fireEvent.click(screen.getByRole('button', { name: 'Delete expense' }))

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e2'))
    await waitFor(() =>
      expect(getUnpaidCardChargesByMonthMock.mock.calls.length).toBeGreaterThan(callCountBefore),
    )
  })

  it('switching away from the Credit Card tab cancels an open edit form', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByText('Uber')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Edit expense' }))
    expect(screen.getByText('Edit Expense')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))

    expect(screen.queryByText('Edit Expense')).not.toBeInTheDocument()
  })

  it('does not duplicate or omit rows already covered by the Expense tab', async () => {
    render(<MonthlyPage />)

    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))
    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    expect(screen.queryByText('Uber')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))
    await waitFor(() => expect(screen.getByText('Uber')).toBeInTheDocument())
    expect(screen.queryByText('Lidl UK')).not.toBeInTheDocument()
  })

  it('still renders the card statements on the Summary tab unchanged', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))

    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
    expect(screen.getByText(/^Total:/)).toBeInTheDocument()
  })

  it('does not refetch card statements when switching to the Credit Card tab', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const callCountBefore = getCardStatementsByMonthMock.mock.calls.length

    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    expect(getCardStatementsByMonthMock.mock.calls.length).toBe(callCountBefore)
  })

  it('marking a statement paid from the Credit Card tab updates the Summary tab too', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))
    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'bank-trading212' } })
    fireEvent.click(screen.getByRole('button', { name: 'Mark Paid' }))
    // The silent refresh that follows a successful mark-paid call fires immediately (no longer
    // deferred to a later render), so the mock must be updated synchronously right after the
    // click - before any of its pending promises get a chance to resolve.
    getCardStatementsByMonthMock.mockResolvedValue([{ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 }, CARD_STATEMENTS[1]])
    await waitFor(() =>
      expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSourceBankId: 'bank-trading212' }),
    )

    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))

    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Unmark Paid' })).toHaveLength(2))
  })

  it("marking a statement paid removes its expenses from the Credit Card tab's list", async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByText('Uber')).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'bank-trading212' } })

    getUnpaidCardChargesByMonthMock.mockResolvedValue([])
    fireEvent.click(screen.getByRole('button', { name: 'Mark Paid' }))
    await waitFor(() =>
      expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSourceBankId: 'bank-trading212' }),
    )

    await waitFor(() => expect(screen.queryByText('Uber')).not.toBeInTheDocument())
  })

  it('shows only the Incoming tabs content after clicking Incoming', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    expect(screen.queryByText(/^Total:/)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'New Expense' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument()
    const incomeSection = within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    expect(incomeSection.getByText('Gleison')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Income' })).toHaveAttribute('aria-selected', 'true')
  })

  it('toggles carry-forward inclusion from the Income tab checkbox', async () => {
    getTitheSummaryByMonthMock.mockResolvedValue({
      calculatedTithe: 245,
      titheBalance: 195,
      carryForward: { amount: 50, included: true, fromYear: 2026, fromMonth: 8 },
    })
    updateTitheCarryForwardMock.mockResolvedValue({
      calculatedTithe: 245,
      titheBalance: 245,
      carryForward: { amount: 50, included: false, fromYear: 2026, fromMonth: 8 },
    })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    const checkbox = await screen.findByRole('checkbox')
    fireEvent.click(checkbox)

    const now = new Date()
    await waitFor(() =>
      expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(now.getFullYear(), now.getMonth() + 1, {
        included: false,
      }),
    )
  })

  it('does not change the month/year picker value when switching tabs', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const monthInput = screen.getByLabelText('Month') as HTMLInputElement
    const valueBefore = monthInput.value

    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    expect(monthInput.value).toBe(valueBefore)
  })

  it('does not refetch data when switching tabs', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    const callCountBefore = getExpensesByMonthMock.mock.calls.length

    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))

    expect(getExpensesByMonthMock.mock.calls.length).toBe(callCountBefore)
  })

  it('keeps the active tab unchanged when the month/year value changes', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    expect(screen.getByRole('tab', { name: 'Bank expenses' })).toHaveAttribute('aria-selected', 'true')
  })

  it('closes an open create form when switching tabs away and back', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    expect(screen.getByRole('button', { name: 'Add Expense' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    expect(screen.queryByRole('button', { name: 'Add Expense' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument()
  })

  it('re-scopes the expense list when the month/year value changes while Expense is active', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

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

    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))
    expect(screen.getByText('Lidl UK')).toBeInTheDocument()
  })

  it('renders a Banks grid with a row per payment source and its own total, alongside the other grids, with no expand or row-action controls', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())

    const banksSection = within(screen.getByText(/Bank Balance:/).closest('section')!)
    expect(banksSection.getByRole('cell', { name: 'Barclays' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Trading212' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Chase' })).toBeInTheDocument()
    // The 3 column-sort header buttons (Bank, Bank Balance, Round-Up) plus the Bank column's
    // filter button — no expand/edit/delete controls.
    expect(banksSection.getAllByRole('button')).toHaveLength(4)

    // The single expense (42.50) is on Barclays with no round-up amount, so its balance
    // is unchanged and every round-up figure (per-bank and the footer) is zero.
    expect(banksSection.getAllByText('42.50').length).toBe(2)
    expect(banksSection.getAllByText('0.00').length).toBe(6)

    expect(screen.getByText(/^Total:/).closest('section')).toHaveClass('monthly-page__section--grid')
    expect(screen.getByText(/Combined adjustment figure/).closest('section')).toHaveClass('monthly-page__section--grid')
    expect(screen.getByText(/Bank Balance:/).closest('section')).toHaveClass('monthly-page__section--grid')
  })

  it('shows the Banks grid on the Expense tab too, above the expense form and list', async () => {
    render(<MonthlyPage />)
    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())

    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    const banksSection = within(screen.getByText(/Bank Balance:/).closest('section')!)
    expect(banksSection.getByRole('cell', { name: 'Barclays' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Trading212' })).toBeInTheDocument()
    expect(banksSection.getByRole('cell', { name: 'Chase' })).toBeInTheDocument()
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

    fireEvent.change(screen.getByLabelText('Paying bank for BaAmex'), { target: { value: 'bank-trading212' } })
    expect(markPaidButton).toBeEnabled()
    fireEvent.click(markPaidButton)

    await waitFor(() =>
      expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1', { paymentSourceBankId: 'bank-trading212' }),
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

  it('does not unmark a paid statement when the user cancels the confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'ChaseMaster4023' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Unmark Paid' }))

    expect(unmarkCardStatementPaidMock).not.toHaveBeenCalled()
  })

  it('opens the New Expense form on the Expense tab locked to bank mode, no toggle', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    expect(screen.getByLabelText(/^Payment Source/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Card/)).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('opens the New Expense form on the Credit Card tab locked to card mode, no toggle', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    expect(screen.getByLabelText(/^Card/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Payment Source/)).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('creates a card expense from the Credit Card tab with a null payment source', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Amazon' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '9.99' } })
    await waitFor(() => expect(screen.getByRole('option', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText(/^Card/), { target: { value: 'card-baamex' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSourceBankId: null, creditCardId: 'card-baamex' }),
      ),
    )
  })

  it('refetches credit cards after saving a card expense, so a later invoice month becomes the default', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    getCreditCardsMock.mockReset()
    getCreditCardsMock.mockResolvedValueOnce(CREDIT_CARDS)
    getCreditCardsMock.mockResolvedValueOnce([{ ...CREDIT_CARDS[0], latestInvoiceDate: '2026-10-01' }, CREDIT_CARDS[1]])
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Amazon' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '9.99' } })
    await waitFor(() => expect(screen.getByRole('option', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText(/^Card/), { target: { value: 'card-baamex' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() => expect(getCreditCardsMock).toHaveBeenCalledTimes(2))

    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    await waitFor(() => expect(screen.getByRole('option', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText(/^Card/), { target: { value: 'card-baamex' } })

    expect(screen.getByLabelText('Invoice Month')).toHaveValue('2026-10')
  })

  it('only lists active categories in the expense form dropdown', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    await waitFor(() => expect(screen.getByRole('option', { name: 'Mercado' })).toBeInTheDocument())
    expect(screen.getByRole('option', { name: 'Extras' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Reserva' })).not.toBeInTheDocument()
  })

  it('deactivating a card via the Credit Card tab removes it from the expense form dropdown', async () => {
    getCreditCardsMock.mockReset()
    getCreditCardsMock.mockResolvedValueOnce(CREDIT_CARDS)
    getCreditCardsMock.mockResolvedValueOnce([CREDIT_CARDS[0], { ...CREDIT_CARDS[1], isActive: false }])
    updateCreditCardMock.mockResolvedValue({ ...CREDIT_CARDS[1], isActive: false })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByLabelText('Active for ChaseMaster4023')).toBeInTheDocument())
    fireEvent.click(screen.getByLabelText('Active for ChaseMaster4023'))

    await waitFor(() =>
      expect(updateCreditCardMock).toHaveBeenCalledWith('card-chase', { name: 'ChaseMaster4023', nextInvoiceDueDate: null, isActive: false }),
    )
    await waitFor(() => expect(getCreditCardsMock).toHaveBeenCalledTimes(2))

    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    await waitFor(() => expect(screen.getByRole('option', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.queryByRole('option', { name: 'ChaseMaster4023' })).not.toBeInTheDocument()
  })

  it('creates a bank expense from the Expense tab with a null card tag', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Waitrose 2' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '12' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({ paymentSourceBankId: 'bank-barclays', creditCardId: null }),
      ),
    )
  })

  it('shows read-only payment fields with a settlement note when editing a settled expense', async () => {
    getExpensesByMonthMock.mockResolvedValue([
      {
        ...EXPENSES[0],
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        creditCardId: 'card-baamex',
        creditCardName: 'BaAmex',
        chargeDate: '2026-07-05',
        invoiceDate: '2026-07-01',
        paymentStatus: 'CreditCardSettled',
      },
    ])
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])

    expect(screen.getByText(/Settled via its card statement/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Payment Source/)).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Card/)).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows the add-expense form only after New Expense is clicked, and submits a new expense', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    expect(screen.queryByLabelText(/^Date/)).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Waitrose' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '15.5' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ description: 'Waitrose', value: 15.5 })),
    )
  })

  it('edits an expense value via the toggled panel and saves, updating the displayed row', async () => {
    updateExpenseMock.mockResolvedValue({ ...EXPENSES[0], value: 50 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('tab', { name: 'Bank expenses' })).toHaveAttribute('aria-selected', 'true'))
    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])
    expect(screen.getByText('Edit Expense')).toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('42.5')
    fireEvent.change(valueInput, { target: { value: '50' } })

    getExpensesByMonthMock.mockResolvedValue([{ ...EXPENSES[0], value: 50 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateExpenseMock).toHaveBeenCalledWith('e1', expect.objectContaining({ value: 50 })))
    const expensesSection = within(screen.getByRole('button', { name: 'New Expense' }).closest('section')!)
    await waitFor(() => expect(expensesSection.getByText('50.00')).toBeInTheDocument(), { timeout: 3000 })
  })

  it('deletes an expense after confirmation', async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete expense' })[0])

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e1'))
  })

  it('does not delete an expense when the user cancels the confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete expense' })[0])

    expect(deleteExpenseMock).not.toHaveBeenCalled()
  })

  it('renders the income list on the Incoming tab', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    const incomeSection = within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    expect(incomeSection.getByText('Gleison')).toBeInTheDocument()
    expect(incomeSection.getByText('2,450.00')).toBeInTheDocument()
  })

  it('re-scopes the income list when the month/year value changes while Incoming is active', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    const getIncomeSection = () => within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    await waitFor(() => expect(getIncomeSection().getByText('Gleison')).toBeInTheDocument())

    getIncomesByMonthMock.mockResolvedValue([
      {
        id: 'i3',
        date: '2026-08-01',
        incomeSourceId: '3',
        incomeSourceName: 'Lottery',
        grossValue: null,
        netValue: 75,
        bankId: 'bank-barclays',
        bankName: 'Barclays',
        description: null,
        splitToReserve: false,
      },
    ])

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(getIncomeSection().getByText('Lottery')).toBeInTheDocument())
    expect(getIncomeSection().queryByText('Gleison')).not.toBeInTheDocument()
  })

  it('shows the add-income form only after New Income is clicked, and submits a new income entry', async () => {
    createIncomeMock.mockResolvedValue({ ...INCOMES[0], id: 'i2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    expect(screen.queryByLabelText('Net Value')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Net Value/), { target: { value: '400' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() =>
      expect(createIncomeMock).toHaveBeenCalledWith(
        expect.objectContaining({ date: '2026-07-16', incomeSourceId: '1', netValue: 400, bankId: null }),
      ),
    )
  })

  it('submits a new income entry with the selected bank when one is chosen', async () => {
    createIncomeMock.mockResolvedValue({ ...INCOMES[0], id: 'i2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Net Value/), { target: { value: '400' } })
    fireEvent.change(screen.getByLabelText('Bank'), { target: { value: 'bank-barclays' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() =>
      expect(createIncomeMock).toHaveBeenCalledWith(
        expect.objectContaining({ bankId: 'bank-barclays' }),
      ),
    )
  })

  it('hides the gross value field for Lottery and DividendoJuros sources', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    expect(screen.getByLabelText('Gross Value')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/^Source/), { target: { value: '3' } })

    expect(screen.queryByLabelText('Gross Value')).not.toBeInTheDocument()
  })

  it('submits a new income entry with a null bank when no bank is available to select', async () => {
    getBanksMock.mockResolvedValue([])
    createIncomeMock.mockResolvedValue({ ...INCOMES[0], id: 'i2', bankId: null, bankName: null })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Net Value/), { target: { value: '400' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() => expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ bankId: null })))
  })

  it('edits an income entry via the toggled panel and saves, updating the displayed row', async () => {
    updateIncomeMock.mockResolvedValue({ ...INCOMES[0], netValue: 500 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('tab', { name: 'Income' })).toHaveAttribute('aria-selected', 'true'))
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Edit income' }).length).toBeGreaterThan(0))

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit income' })[0])
    expect(screen.getByText('Edit Income')).toBeInTheDocument()
    const netValueInput = screen.getByDisplayValue('2450')
    fireEvent.change(netValueInput, { target: { value: '500' } })

    getIncomesByMonthMock.mockResolvedValue([{ ...INCOMES[0], netValue: 500 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ netValue: 500 })))
    const incomeSection = within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    await waitFor(() => expect(incomeSection.getByText('500.00')).toBeInTheDocument(), { timeout: 3000 })
  })

  it('deletes an income entry after confirmation', async () => {
    deleteIncomeMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

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
    createIncomeMock.mockResolvedValue({
      id: 'i2',
      date: '2026-07-15',
      incomeSourceId: '3',
      incomeSourceName: 'Lottery',
      grossValue: null,
      netValue: 100,
      bankId: 'bank-chase',
      bankName: 'Chase',
      description: null,
      splitToReserve: false,
    })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Income' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-15' } })
    fireEvent.change(screen.getByLabelText(/^Source/), { target: { value: '3' } })
    fireEvent.change(screen.getByLabelText(/^Net Value/), { target: { value: '100' } })

    getIncomesByMonthMock.mockResolvedValue([...INCOMES, {
      id: 'i2',
      date: '2026-07-15',
      incomeSourceId: '3',
      incomeSourceName: 'Lottery',
      grossValue: null,
      netValue: 100,
      bankId: 'bank-chase',
      bankName: 'Chase',
      description: null,
      splitToReserve: false,
    }])
    fireEvent.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() => expect(createIncomeMock).toHaveBeenCalled())
    const incomeSection = within(screen.getByRole('button', { name: 'New Income' }).closest('section')!)
    await waitFor(() => expect(incomeSection.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))
    const incomingSection = within(screen.getByText(/Total Incoming:/).closest('section')!)
    await waitFor(() => expect(incomingSection.getByRole('cell', { name: 'Lottery' })).toBeInTheDocument())
  })

  it('bank picker lists banks fetched from the API in the expense form', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    const bankPicker = screen.getByLabelText(/^Payment Source/)
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
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '9.40' } })

    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/^Payment Source/), { target: { value: 'bank-trading212' } })

    expect(screen.getByLabelText('Round-Up')).toHaveValue(0.6)
  })

  it('hides the round-up field for a non-round-up bank', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '9.40' } })
    fireEvent.change(screen.getByLabelText(/^Payment Source/), { target: { value: 'bank-trading212' } })
    expect(screen.getByLabelText('Round-Up')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/^Payment Source/), { target: { value: 'bank-barclays' } })
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()
  })

  it('never shows the round-up field in card mode', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Credit Card expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '9.40' } })

    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()
  })

  it('submits a typed round-up amount with the new expense', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Expense' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))
    fireEvent.change(screen.getByLabelText(/^Date/), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'TfL' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '9.40' } })
    fireEvent.change(screen.getByLabelText(/^Payment Source/), { target: { value: 'bank-trading212' } })
    fireEvent.change(screen.getByLabelText('Round-Up'), { target: { value: '0.10' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ roundUpAmount: 0.1 })),
    )
  })

  it('shows a bank balance reduced by its round-up total, in a separate column', async () => {
    getExpensesByMonthMock.mockResolvedValue([
      {
        ...EXPENSES[0],
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        value: 9.4,
        roundUpAmount: 0.6,
      },
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
      {
        ...EXPENSES[0],
        paymentSourceBankId: 'bank-trading212',
        paymentSourceBankName: 'Trading212',
        roundUpAmount: 0.6,
        suggestedRoundUpAmount: null,
      },
    ])
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank expenses' }))

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])

    expect(screen.getByLabelText('Round-Up')).toHaveValue(0.6)
  })

  it('shows the Bank tab operations list combining transfers and adjustments for the month, newest-first', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Transfer')).toBeInTheDocument())
    expect(screen.getByText('Adjustment')).toBeInTheDocument()
    expect(screen.getByText('Barclays → Trading212')).toBeInTheDocument()
  })

  it('shows the same bank balances grid on the Bank tab as on the Summary tab', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText(/Bank Balance:/)).toBeInTheDocument())
    const banksSection = within(screen.getByText(/Bank Balance:/).closest('section')!)
    expect(banksSection.getByRole('columnheader', { name: 'Bank Balance' })).toBeInTheDocument()
    expect(banksSection.getByRole('columnheader', { name: 'Round-Up' })).toBeInTheDocument()
  })

  it('opens the New Transfer form with no bank pre-selected, creates a transfer, and refreshes balances and the operations list', async () => {
    createTransferMock.mockResolvedValue(TRANSFERS[0])
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Transfer' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Transfer' }))

    expect(screen.getByText('New Transfer', { selector: 'h2' })).toBeInTheDocument()

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    const transfersCallsBefore = getTransfersByMonthMock.mock.calls.length

    fireEvent.change(screen.getByLabelText(/^To/), { target: { value: 'bank-trading212' } })
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '50' } })
    const transferFormPanel = screen.getByTestId('transfer-form-panel')
    fireEvent.click(within(transferFormPanel).getByRole('button', { name: 'Add Transfer' }))

    await waitFor(() => expect(createTransferMock).toHaveBeenCalled())
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
    expect(getTransfersByMonthMock.mock.calls.length).toBeGreaterThan(transfersCallsBefore)
  })

  it('gates New Balance Correction on a bank being chosen, then shows the resulting delta', async () => {
    createBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENTS[0], id: 'a2', delta: 2.5 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Balance Correction' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Balance Correction' }))

    const correctBalanceFormPanel = screen.getByTestId('balance-adjustment-form-panel')
    expect(within(correctBalanceFormPanel).getByRole('button', { name: 'Add Balance Correction' })).toBeDisabled()
    expect(screen.queryByLabelText('Target Balance')).not.toBeInTheDocument()

    fireEvent.change(screen.getByLabelText(/^Bank/), { target: { value: 'bank-barclays' } })
    expect(correctBalanceFormPanel).toHaveTextContent('Current calculated balance for Barclays: £42.50')

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    const adjustmentsCallsBefore = getAdjustmentsByBankMock.mock.calls.length

    fireEvent.change(screen.getByRole('spinbutton', { name: /^Target Balance/ }), { target: { value: '45' } })
    fireEvent.click(within(correctBalanceFormPanel).getByRole('button', { name: 'Add Balance Correction' }))

    await waitFor(() =>
      expect(createBalanceAdjustmentMock).toHaveBeenCalledWith('bank-barclays', expect.objectContaining({ targetBalance: 45 })),
    )
    await waitFor(() =>
      expect(screen.getByTestId('balance-adjustment-form-panel')).toHaveTextContent('Adjustment of £2.50 recorded'),
    )
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
    expect(getAdjustmentsByBankMock.mock.calls.length).toBeGreaterThan(adjustmentsCallsBefore)
  })

  it('narrows the operations list via the bank filter with no additional network request', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Transfer')).toBeInTheDocument())
    const transfersCallsBefore = getTransfersByMonthMock.mock.calls.length
    const adjustmentsCallsBefore = getAdjustmentsByBankMock.mock.calls.length

    // The Bank tab renders both BanksGrid and BankOperationsSection, each with its own "Bank"
    // column filter — scope to the operations table (identified by its Amount/Delta header).
    const operationsSection = screen.getByRole('columnheader', { name: 'Amount/Delta' }).closest('section') as HTMLElement
    fireEvent.click(within(operationsSection).getByRole('button', { name: 'Filter by Bank' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Barclays' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Trading212' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
    expect(getTransfersByMonthMock.mock.calls.length).toBe(transfersCallsBefore)
    expect(getAdjustmentsByBankMock.mock.calls.length).toBe(adjustmentsCallsBefore)
  })

  it('edits a transfer from the operations list, opening TransferForm pre-filled with its values, and persists the change', async () => {
    updateTransferMock.mockResolvedValue(TRANSFERS[0])
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Transfer')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Edit transfer' }))

    expect(screen.getByText('Edit Transfer')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount/)).toHaveValue(100)

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    fireEvent.change(screen.getByLabelText(/^Amount/), { target: { value: '150' } })
    const transferFormPanel = screen.getByTestId('transfer-form-panel')
    fireEvent.click(within(transferFormPanel).getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateTransferMock).toHaveBeenCalledWith('t1', expect.objectContaining({ amount: 150 })))
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
  })

  it('edits an adjustment from the operations list with the bank fixed, and persists the change', async () => {
    updateBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENTS[0], delta: 7.5 })
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Adjustment')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Edit balance adjustment' }))

    expect(screen.getByText('Edit Balance Correction')).toBeInTheDocument()
    expect(screen.queryByLabelText('Bank')).not.toBeInTheDocument()
    expect(screen.getByRole('spinbutton', { name: /^Target Balance/ })).toHaveValue(42.5)

    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    fireEvent.change(screen.getByRole('spinbutton', { name: /^Target Balance/ }), { target: { value: '50' } })
    const adjustmentFormPanel = screen.getByTestId('balance-adjustment-form-panel')
    fireEvent.click(within(adjustmentFormPanel).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateBalanceAdjustmentMock).toHaveBeenCalledWith('bank-barclays', 'a1', expect.objectContaining({ targetBalance: 50 })),
    )
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
  })

  it('does not delete a transfer when the user cancels the confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Transfer')).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Delete transfer' }))

    expect(deleteTransferMock).not.toHaveBeenCalled()
  })

  it('deletes a transfer from the operations list after confirmation, and refreshes balances', async () => {
    deleteTransferMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Transfer')).toBeInTheDocument())
    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    fireEvent.click(screen.getByRole('button', { name: 'Delete transfer' }))

    await waitFor(() => expect(deleteTransferMock).toHaveBeenCalledWith('t1'))
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
  })

  it('deletes an adjustment from the operations list after confirmation, and refreshes balances', async () => {
    deleteBalanceAdjustmentMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByText('Adjustment')).toBeInTheDocument())
    const balancesCallsBefore = getBankBalancesByMonthMock.mock.calls.length
    fireEvent.click(screen.getByRole('button', { name: 'Delete balance adjustment' }))

    await waitFor(() => expect(deleteBalanceAdjustmentMock).toHaveBeenCalledWith('bank-barclays', 'a1'))
    await waitFor(() => expect(getBankBalancesByMonthMock.mock.calls.length).toBeGreaterThan(balancesCallsBefore))
  })

  it('shows an error state with retry when the Bank tab operations fetch fails', async () => {
    getTransfersByMonthMock.mockRejectedValue(new Error('Operations down'))
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Operations down')).toBeInTheDocument()

    getTransfersByMonthMock.mockResolvedValue(TRANSFERS)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(screen.getByText('Transfer')).toBeInTheDocument())
  })

  it('cancels an open create form when switching away from the Bank tab and back', async () => {
    render(<MonthlyPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Transfer' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Transfer' }))
    expect(screen.getByText('New Transfer', { selector: 'h2' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('tab', { name: 'Summary' }))
    fireEvent.click(screen.getByRole('tab', { name: 'Bank balance adjustment' }))

    expect(screen.queryByText('New Transfer', { selector: 'h2' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'New Transfer' })).toBeInTheDocument()
  })
})
