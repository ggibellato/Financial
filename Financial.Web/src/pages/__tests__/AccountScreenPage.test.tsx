import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { Toaster } from '@fluentui/react-components'
import type {
  AccountDetailDto,
  AccountDto,
  AccountStatementDto,
  AccountTransactionDto,
  AccountTransferDto,
  BalanceAdjustmentDto,
  BankDto,
  CategoryDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
  PayableStatementDto,
  RecurringBillDto,
} from '../../api/types'
import { APP_TOASTER_ID } from '../../hooks/useSuccessToast'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountScreenPage from '../AccountScreenPage'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { recurringBill } from '../../test/recurringBill'
import { account, cardAccount } from '../../test/account'

const {
  getAccountsMock,
  getAccountMock,
  getAccountTransactionsMock,
  getBanksMock,
  getCategoriesMock,
  getIncomeSourcesMock,
  createExpenseMock,
  createIncomeMock,
  updateExpenseMock,
  deleteExpenseMock,
  deleteIncomeMock,
  getTransferDestinationsMock,
  getTransferSourcesMock,
  createAccountTransferMock,
  updateAccountTransferMock,
  deleteAccountTransferMock,
  createBalanceAdjustmentMock,
  deleteBalanceAdjustmentMock,
  getMensaisBillsMock,
  markMensaisBillPaidMock,
  getAccountStatementsMock,
  markCardStatementPaidMock,
  unmarkCardStatementPaidMock,
  moveChargeToStatementMock,
  getPayableStatementsMock,
} = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getAccountMock: vi.fn<FinancialApiClient['getAccount']>(),
  getAccountTransactionsMock: vi.fn<FinancialApiClient['getAccountTransactions']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  createExpenseMock: vi.fn<FinancialApiClient['createExpense']>(),
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
  updateExpenseMock: vi.fn<FinancialApiClient['updateExpense']>(),
  deleteExpenseMock: vi.fn<FinancialApiClient['deleteExpense']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
  getTransferDestinationsMock: vi.fn<FinancialApiClient['getTransferDestinations']>(),
  getTransferSourcesMock: vi.fn<FinancialApiClient['getTransferSources']>(),
  createAccountTransferMock: vi.fn<FinancialApiClient['createAccountTransfer']>(),
  updateAccountTransferMock: vi.fn<FinancialApiClient['updateAccountTransfer']>(),
  deleteAccountTransferMock: vi.fn<FinancialApiClient['deleteAccountTransfer']>(),
  createBalanceAdjustmentMock: vi.fn<FinancialApiClient['createBalanceAdjustment']>(),
  deleteBalanceAdjustmentMock: vi.fn<FinancialApiClient['deleteBalanceAdjustment']>(),
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  markMensaisBillPaidMock: vi.fn<FinancialApiClient['markMensaisBillPaid']>(),
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
  markCardStatementPaidMock: vi.fn<FinancialApiClient['markCardStatementPaid']>(),
  unmarkCardStatementPaidMock: vi.fn<FinancialApiClient['unmarkCardStatementPaid']>(),
  moveChargeToStatementMock: vi.fn<FinancialApiClient['moveChargeToStatement']>(),
  getPayableStatementsMock: vi.fn<FinancialApiClient['getPayableStatements']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getAccount: getAccountMock,
    getAccountTransactions: getAccountTransactionsMock,
    getBanks: getBanksMock,
    getCategories: getCategoriesMock,
    getIncomeSources: getIncomeSourcesMock,
    createExpense: createExpenseMock,
    createIncome: createIncomeMock,
    updateExpense: updateExpenseMock,
    deleteExpense: deleteExpenseMock,
    deleteIncome: deleteIncomeMock,
    getTransferDestinations: getTransferDestinationsMock,
    getTransferSources: getTransferSourcesMock,
    createAccountTransfer: createAccountTransferMock,
    updateAccountTransfer: updateAccountTransferMock,
    deleteAccountTransfer: deleteAccountTransferMock,
    createBalanceAdjustment: createBalanceAdjustmentMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
    getMensaisBills: getMensaisBillsMock,
    markMensaisBillPaid: markMensaisBillPaidMock,
    getAccountStatements: getAccountStatementsMock,
    markCardStatementPaid: markCardStatementPaidMock,
    unmarkCardStatementPaid: unmarkCardStatementPaidMock,
    moveChargeToStatement: moveChargeToStatementMock,
    getPayableStatements: getPayableStatementsMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'
const AMEX_ID = '8f3b1c1a-2e3a-4b1a-9a7f-500000000001'
const CHASE_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000002'
const WISE_EUR_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000004'

const ACCOUNTS: AccountDto[] = [
  account({ id: BARCLAYS_ID, name: 'Barclays' }),
  account({ id: CHASE_ID, name: 'Chase' }),
  cardAccount({ id: AMEX_ID, name: 'BA Amex' }),
]

function detailFor(id: string, year: number, month: number): AccountDetailDto {
  const account = ACCOUNTS.find((candidate) => candidate.id === id)!
  const isCard = id === AMEX_ID
  return { ...account, year, month, balance: id === BARCLAYS_ID ? 2800.25 : 500, balanceIsOwed: isCard, hasStatements: isCard, allowedActions: isCard ? ['AddCharge'] : [] }
}

const TRANSACTIONS: AccountTransactionDto[] = [
  accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', description: 'Tesco', categoryName: 'Groceries', amount: -42.1, roundUp: -0.9, canEdit: true }),
  accountTransactionRow({ id: 't1', kind: 'TransferOut', date: '2026-10-05', counterpart: 'Chase', amount: -200, canEdit: true }),
]

const CARD_STATEMENTS: AccountStatementDto[] = [
  { id: 's-nov', year: 2026, month: 11, total: 0, outstandingTotal: 0, isPaid: false, dueDate: null, paidOn: null, paidFromAccountName: null },
  { id: 's-oct', year: 2026, month: 10, total: 210.45, outstandingTotal: 210.45, isPaid: false, dueDate: '2026-11-03', paidOn: null, paidFromAccountName: null },
  { id: 's-sep', year: 2026, month: 9, total: 512.3, outstandingTotal: 0, isPaid: true, dueDate: '2026-10-03', paidOn: '2026-10-12', paidFromAccountName: 'Barclays' },
]

const BANKS: BankDto[] = [
  { id: BARCLAYS_ID, name: 'Barclays', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
  { id: CHASE_ID, name: 'Chase', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-groceries', name: 'Groceries', active: true, isInvestment: false, isTithe: false, hasReferences: true },
  { id: 'category-home', name: 'Home', active: true, isInvestment: false, isTithe: false, hasReferences: true },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: 'source-salary', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: true },
]

const BANK_ACTIONS = ['AddExpense', 'AddIncome', 'TransferTo', 'TransferFrom', 'PayRecurringBill', 'PayCardStatement', 'Reconcile']

const VISA_ID = '8f3b1c1a-2e3a-4b1a-9a7f-500000000002'

const PAYABLE_STATEMENTS: PayableStatementDto[] = [
  { id: 's-oct', creditCardId: AMEX_ID, creditCardName: 'BA Amex', year: 2026, month: 10, total: 210.45, currency: 'GBP' },
  { id: 's-nov', creditCardId: AMEX_ID, creditCardName: 'BA Amex', year: 2026, month: 11, total: 12, currency: 'GBP' },
  { id: 'v-oct', creditCardId: VISA_ID, creditCardName: 'Platinum Visa 6007', year: 2026, month: 10, total: 80, currency: 'GBP' },
]

const CARD_PAYMENT_ROW = accountTransactionRow({
  id: 'p1',
  kind: 'CardPayment',
  date: '2026-10-12',
  description: 'BA Amex payment – Sep 2026 statement',
  counterpart: 'BA Amex',
  amount: -512.3,
  canEdit: false,
  statement: { accountId: AMEX_ID, year: 2026, month: 9 },
})

const COUNCIL_TAX = recurringBill({ id: 'council', dueDay: 5, description: 'Council Tax', value: 120 })

const BILLS: RecurringBillDto[] = [
  recurringBill({ id: 'water', dueDay: 20, description: 'Water', value: 35.5, status: 'Scheduled' }),
  recurringBill({ id: 'rent', dueDay: 1, description: 'Rent', value: 900, status: 'Paid' }),
  recurringBill({ id: 'iptu', dueDay: 2, description: 'IPTU', value: 80, area: 'BR' }),
  COUNCIL_TAX,
]

function withActions(allowedActions: string[]) {
  getAccountMock.mockImplementation((id, year, month) => Promise.resolve({ ...detailFor(id, year, month), allowedActions }))
}

function LocationText() {
  const location = useLocation()
  return <p>{`${location.pathname}${location.search}`}</p>
}

function BrowserBackButton() {
  const navigate = useNavigate()
  return (
    <button type="button" onClick={() => navigate(-1)}>
      Browser back
    </button>
  )
}

function renderScreen(entry: string, previousEntries: string[] = []) {
  const entries = [...previousEntries, entry]
  return render(
    <MemoryRouter initialEntries={entries} initialIndex={entries.length - 1}>
      <Routes>
        <Route
          path="/cashflow/accounts/:accountId"
          element={
            <>
              <Toaster toasterId={APP_TOASTER_ID} />
              <AccountScreenPage />
              <LocationText />
              <BrowserBackButton />
            </>
          }
        />
        <Route path="/cashflow/accounts" element={<p>Accounts page</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

// Under jsdom, Tabster can leave aria-hidden on a newly opened Fluent dialog, which empties its accessible name.
function findDialog(title: string): Promise<HTMLElement> {
  return waitFor(() => {
    const dialog = screen
      .getAllByRole('dialog', { hidden: true })
      .find((candidate) => candidate.querySelector('h2')?.textContent === title)
    expect(dialog).toBeDefined()
    return dialog as HTMLElement
  })
}

async function dialogButton(dialogTitle: string, buttonName: string) {
  return within(await findDialog(dialogTitle)).getByText(buttonName, { selector: 'button' })
}

beforeEach(() => {
  pinDate('2026-10-15T12:00:00+01:00')
  getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
  getAccountMock.mockReset().mockImplementation((id, year, month) => Promise.resolve(detailFor(id, year, month)))
  getAccountTransactionsMock.mockReset().mockResolvedValue(TRANSACTIONS)
  getBanksMock.mockReset().mockResolvedValue(BANKS)
  getCategoriesMock.mockReset().mockResolvedValue(CATEGORIES)
  getIncomeSourcesMock.mockReset().mockResolvedValue(INCOME_SOURCES)
  createExpenseMock.mockReset()
  createIncomeMock.mockReset()
  updateExpenseMock.mockReset()
  deleteExpenseMock.mockReset().mockResolvedValue(undefined)
  deleteIncomeMock.mockReset().mockResolvedValue(undefined)
  getTransferDestinationsMock.mockReset().mockResolvedValue([{ id: CHASE_ID, name: 'Chase', currency: 'GBP' }])
  getTransferSourcesMock.mockReset().mockResolvedValue([
    { id: CHASE_ID, name: 'Chase', currency: 'GBP' },
    { id: WISE_EUR_ID, name: 'Wise EUR', currency: 'EUR' },
  ])
  createAccountTransferMock.mockReset()
  updateAccountTransferMock.mockReset()
  deleteAccountTransferMock.mockReset().mockResolvedValue(undefined)
  createBalanceAdjustmentMock.mockReset()
  deleteBalanceAdjustmentMock.mockReset().mockResolvedValue(undefined)
  getMensaisBillsMock.mockReset().mockResolvedValue(BILLS)
  markMensaisBillPaidMock.mockReset()
  getAccountStatementsMock.mockReset().mockResolvedValue(CARD_STATEMENTS)
  markCardStatementPaidMock.mockReset()
  unmarkCardStatementPaidMock.mockReset()
  moveChargeToStatementMock.mockReset()
  getPayableStatementsMock.mockReset().mockResolvedValue(PAYABLE_STATEMENTS)
  sessionStorage.clear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('AccountScreenPage', () => {

  it('[AC P60-F05-bank-account-screen-02] switching account in the picker keeps the selected month', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-09`)

    await user.click(await screen.findByRole('combobox', { name: 'Account' }))
    await user.click(screen.getByRole('option', { name: 'Chase' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Chase' })).toBeInTheDocument()
    expect(screen.getByText(`/cashflow/accounts/${CHASE_ID}?month=2026-09`)).toBeInTheDocument()
    expect(getAccountMock).toHaveBeenLastCalledWith(CHASE_ID, 2026, 9)
  })

  it('[AC P60-F05-bank-account-screen-03] the header shows name, template, currency and the month-end balance from the detail', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
    expect(screen.getByText('Bank · GBP')).toBeInTheDocument()
    expect(screen.getByText('Balance at end of October 2026')).toBeInTheDocument()
    expect(screen.getByText('2,800.25 GBP')).toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-04] the grid lists the month rows with signed amounts', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    const table = await screen.findByRole('table', { name: 'Transactions' })
    await waitFor(() => expect(within(table).getByText('Tesco')).toBeInTheDocument())
    expect(within(table).getByText('-42.10')).toBeInTheDocument()
    expect(within(table).getByText('-0.90')).toBeInTheDocument()
    expect(within(table).getByText('-200.00')).toBeInTheDocument()
  })

  it('keeps keyboard focus on the picker once the chosen account has loaded', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    const picker = await screen.findByRole('combobox', { name: 'Account' })

    picker.focus()
    await user.keyboard('{Enter}{ArrowDown}{Enter}')

    expect(await screen.findByRole('heading', { level: 1, name: 'Chase' })).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Account' })).toHaveFocus()
  })

  it('keeps the header and shows the error in the transactions area when a later load fails', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })
    getAccountTransactionsMock.mockRejectedValueOnce(new ApiError('Server error', 500))

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    expect(await screen.findByText('Unable to load this account')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
    expect(screen.getByLabelText('Month')).toHaveValue('2026-08')
  })

  it('shows skeletons for the header and grid on first load', () => {
    getAccountMock.mockReturnValue(new Promise(() => {}))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(screen.getByLabelText('Loading account')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
  })

  it('shows the empty message naming the month when it has no transactions', async () => {
    getAccountTransactionsMock.mockResolvedValue([])

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('No transactions in October 2026')).toBeInTheDocument()
  })

  it('reloads for the month chosen in the month input', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    expect(await screen.findByText('Balance at end of August 2026')).toBeInTheDocument()
    expect(screen.getByText(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-08`)).toBeInTheDocument()
  })

  it('shows the not-found message with a link to the Accounts page for an unknown account', async () => {
    getAccountMock.mockRejectedValue(new ApiError('Account not found', 404))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('This account does not exist.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('link', { name: 'Go to Accounts' }))
    expect(screen.getByText('Accounts page')).toBeInTheDocument()
  })

  it('shows the not-found message for a malformed id without loading anything', () => {
    renderScreen('/cashflow/accounts/not-a-guid?month=2026-10')

    expect(screen.getByText('This account does not exist.')).toBeInTheDocument()
    expect(getAccountMock).not.toHaveBeenCalled()
  })

  it('shows an error with Try again when the load fails, and Try again reloads', async () => {
    getAccountTransactionsMock.mockRejectedValueOnce(new ApiError('Server error', 500))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('Unable to load this account')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
  })
})

describe('AccountScreenPage actions', () => {
  const SAVED_EXPENSE: ExpenseDto = {
    id: 'e9',
    date: '2026-10-15',
    description: 'Lidl',
    value: 12.4,
    categoryId: 'category-home',
    categoryName: 'Home',
    paymentSourceBankId: BARCLAYS_ID,
    paymentSourceBankName: 'Barclays',
    creditCardId: null,
    creditCardName: null,
    chargeDate: null,
    invoiceDate: null,
    paymentStatus: 'ImmediatePayment',
    roundUpAmount: 0.6,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  }

  const SAVED_INCOME: IncomeDto = {
    id: 'i9',
    date: '2026-10-15',
    incomeSourceId: 'source-salary',
    incomeSourceName: 'Gleison',
    grossValue: null,
    netValue: 2500,
    bankId: BARCLAYS_ID,
    bankName: 'Barclays',
    description: null,
    splitToReserve: false,
  }

  beforeEach(() => {
    createExpenseMock.mockResolvedValue(SAVED_EXPENSE)
    createIncomeMock.mockResolvedValue(SAVED_INCOME)
    withActions(BANK_ACTIONS)
  })

  async function openScreen() {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled())
  }

  it('[AC P60-F05-bank-account-screen-05] renders exactly the allowed actions: seven for a bank, a reduced list as sent, none when inactive', async () => {
    await openScreen()

    const actionNames = () =>
      within(screen.getByRole('group', { name: 'Account actions' }))
        .getAllByRole('button')
        .map((button) => button.textContent)
    expect(actionNames()).toEqual([
      'Add expense',
      'Add income',
      'Transfer to',
      'Transfer from',
      'Pay recurring bill',
      'Pay card statement',
      'Adjust balance',
    ])
  })

  it('renders only the actions a reduced list allows, in its order', async () => {
    withActions(['Reconcile', 'AddIncome'])

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    await waitFor(() => expect(screen.getByRole('button', { name: 'Add income' })).toBeEnabled())
    const actions = within(screen.getByRole('group', { name: 'Account actions' })).getAllByRole('button')
    expect(actions.map((button) => button.textContent)).toEqual(['Adjust balance', 'Add income'])
  })

  it('renders no actions and no row buttons for an inactive account', async () => {
    getAccountMock.mockImplementation((id, year, month) =>
      Promise.resolve({ ...detailFor(id, year, month), isActive: false, allowedActions: [] }),
    )
    getAccountTransactionsMock.mockResolvedValue(TRANSACTIONS.map((row) => ({ ...row, canEdit: false })))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    await screen.findByText('Tesco')
    expect(screen.queryByRole('group', { name: 'Account actions' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^(Edit|Delete) / })).not.toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-08] Pay recurring bill lists the unpaid UK bills and pays the chosen one with this bank', async () => {
    const user = userEvent.setup()
    markMensaisBillPaidMock.mockResolvedValue({ ...COUNCIL_TAX, status: 'Paid' })
    await openScreen()
    getAccountMock.mockClear()
    const payBill = screen.getByRole('button', { name: 'Pay recurring bill' })

    await user.click(payBill)
    const picker = await screen.findByRole('combobox', { name: /^Bill/ })
    await waitFor(() => expect(picker).toHaveFocus())
    expect(within(picker).getAllByRole('option').map((option) => option.textContent)).toEqual([
      'Select a bill',
      'Due 5 · Council Tax · £120.00',
      'Due 20 · Water · £35.50',
    ])
    await user.selectOptions(picker, 'council')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Council Tax')
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-15')
    await user.selectOptions(screen.getByLabelText(/^Category/), 'category-home')
    const form = screen.getByRole('heading', { name: 'Pay recurring bill' }).closest('form') as HTMLElement
    await user.click(within(form).getByRole('button', { name: 'Pay recurring bill' }))

    await waitFor(() =>
      expect(markMensaisBillPaidMock).toHaveBeenCalledWith('council', {
        expense: {
          date: '2026-10-15',
          description: 'Council Tax',
          value: 120,
          categoryId: 'category-home',
          paymentSourceBankId: BARCLAYS_ID,
        },
      }),
    )
    expect(await screen.findByText('Bill paid')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Pay recurring bill' })).not.toBeInTheDocument()
    await waitFor(() => expect(getAccountMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 10))
    await waitFor(() => expect(payBill).toHaveFocus())
  })

  it('says a bill is already paid and drops it from the list when the payment is refused for that', async () => {
    const user = userEvent.setup()
    markMensaisBillPaidMock.mockRejectedValue(
      new ApiError('The bill is already Paid; recording another payment expense would duplicate it.', 400),
    )
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Pay recurring bill' }))
    await user.selectOptions(await screen.findByRole('combobox', { name: /^Bill/ }), 'council')
    await user.selectOptions(screen.getByLabelText(/^Category/), 'category-home')
    getMensaisBillsMock.mockResolvedValue(BILLS.map((bill) => (bill.id === 'council' ? { ...bill, status: 'Paid' } : bill)))
    const form = screen.getByRole('heading', { name: 'Pay recurring bill' }).closest('form') as HTMLElement
    await user.click(within(form).getByRole('button', { name: 'Pay recurring bill' }))

    expect(await screen.findByText('This bill is already paid.')).toBeInTheDocument()
    expect(within(screen.getByRole('combobox', { name: /^Bill/ })).queryByRole('option', { name: /Council Tax/ })).not.toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-10] shows the loading, server error, saving and success states; validation and unsaved changes have their own tests', async () => {
    const user = userEvent.setup()
    let finishSave: (expense: ExpenseDto) => void = () => {}
    createExpenseMock
      .mockRejectedValueOnce(new ApiError('Failed to fetch', 0))
      .mockImplementationOnce(() => new Promise((resolve) => (finishSave = resolve)))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    expect(screen.getByLabelText('Loading account')).toBeInTheDocument()
    expect(screen.getByLabelText('Loading transactions')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled())

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.type(screen.getByLabelText(/^Value/), '12.40')
    await user.click(screen.getByRole('button', { name: 'Add Expense' }))
    expect(await screen.findByText('Could not save. Check the connection and try again.')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Lidl')

    await user.click(screen.getByRole('button', { name: 'Add Expense' }))
    expect(await screen.findByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByLabelText(/^Description/)).toBeDisabled()
    finishSave(SAVED_EXPENSE)
    expect(await screen.findByText('Expense added')).toBeInTheDocument()
  })

  it('keeps the other actions usable while the transfer counterparts are still loading', async () => {
    getTransferDestinationsMock.mockReturnValue(new Promise(() => {}))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    await waitFor(() => expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled())
    expect(screen.getByRole('button', { name: 'Adjust balance' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Transfer to' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Transfer from' })).toBeDisabled()
  })

  it('loads no transfer counterparts for an account that cannot transfer', async () => {
    withActions(['AddExpense'])

    await openScreen()

    expect(getTransferDestinationsMock).not.toHaveBeenCalled()
    expect(getTransferSourcesMock).not.toHaveBeenCalled()
  })

  it('renders no actions and loads no form options for an account that allows none', async () => {
    withActions([])

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })

    expect(screen.queryByRole('group', { name: 'Account actions' })).not.toBeInTheDocument()
    expect(getBanksMock).not.toHaveBeenCalled()
  })

  it('[AC P60-F05-bank-account-screen-06] opens one inline form above the grid, and switching with unsaved changes asks to discard', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    const expenseHeading = screen.getByRole('heading', { name: 'New Expense' })
    const transactionsHeading = screen.getByRole('heading', { name: 'Transactions' })
    expect(expenseHeading.compareDocumentPosition(transactionsHeading) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()

    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.click(screen.getByRole('button', { name: 'Add income' }))

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()

    await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Lidl')
    expect(screen.queryByRole('heading', { name: 'New Income' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add income' }))
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(screen.getByRole('heading', { name: 'New Income' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()
  })

  it('switches between clean forms without asking', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.click(screen.getByRole('button', { name: 'Add income' }))

    expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'New Income' })).toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-07] Add expense saves with this bank, refreshes header and grid, and stays open with the last date and category', async () => {
    const user = userEvent.setup()
    await openScreen()
    getAccountMock.mockClear()
    getAccountTransactionsMock.mockClear()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    expect(screen.getByText('Barclays', { selector: 'span' })).toBeInTheDocument()
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.type(screen.getByLabelText(/^Value/), '12.40')
    await user.selectOptions(screen.getByLabelText(/^Category/), 'category-home')
    await user.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({
          date: '2026-10-15',
          description: 'Lidl',
          value: 12.4,
          categoryId: 'category-home',
          paymentSourceBankId: BARCLAYS_ID,
          creditCardId: null,
          roundUpAmount: 0.6,
        }),
      ),
    )
    expect(await screen.findByText('Expense added')).toBeInTheDocument()
    await waitFor(() => expect(getAccountMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 10))
    expect(getAccountTransactionsMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 10)
    expect(screen.getByRole('heading', { name: 'New Expense' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-15')
    expect(screen.getByLabelText(/^Category/)).toHaveValue('category-home')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(null)
  })

  it('moves focus to Description after a save so the next entry can be typed at once', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.type(screen.getByLabelText(/^Value/), '12.40{Enter}')

    expect(await screen.findByText('Expense added')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText(/^Description/)).toHaveFocus())
  })

  it('says the income was split to reserve when it was', async () => {
    const user = userEvent.setup()
    createIncomeMock.mockResolvedValue({ ...SAVED_INCOME, splitToReserve: true })
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add income' }))
    await user.type(screen.getByLabelText(/^Net Value/), '2500')
    await user.click(screen.getByRole('button', { name: 'Add Income' }))

    expect(await screen.findByText('Income added and split to reserve')).toBeInTheDocument()
  })

  it('closes the open form when the open action is pressed again, returning focus to it', async () => {
    const user = userEvent.setup()
    await openScreen()
    const addExpense = screen.getByRole('button', { name: 'Add expense' })

    await user.click(addExpense)
    await user.click(addExpense)

    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()
    expect(addExpense).toHaveAttribute('aria-expanded', 'false')
    expect(addExpense).toHaveFocus()
  })

  it('closes the open form when the browser goes back to another account, so nothing saves to the wrong bank', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${CHASE_ID}?month=2026-10`, [`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`])
    await screen.findByRole('heading', { level: 1, name: 'Chase' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled())

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    expect(within(screen.getByRole('heading', { name: 'New Expense' }).closest('form') as HTMLElement).getByText('Chase')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Browser back' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.type(screen.getByLabelText(/^Value/), '12.40')
    await user.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ paymentSourceBankId: BARCLAYS_ID })),
    )
  })

  it('Add income saves with this bank, shows the toast and stays open', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add income' }))
    expect(screen.getByText('Barclays', { selector: 'span' })).toBeInTheDocument()
    await user.type(screen.getByLabelText(/^Net Value/), '2500')
    await user.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() =>
      expect(createIncomeMock).toHaveBeenCalledWith(
        expect.objectContaining({ bankId: BARCLAYS_ID, netValue: 2500, incomeSourceId: 'source-salary' }),
      ),
    )
    expect(await screen.findByText('Income added')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'New Income' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Net Value/)).toHaveValue(null)
  })

  it('keeps the values and shows the connection message when a save cannot reach the server', async () => {
    const user = userEvent.setup()
    createExpenseMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.type(screen.getByLabelText(/^Value/), '12.40')
    await user.click(screen.getByRole('button', { name: 'Add Expense' }))

    expect(await screen.findByText('Could not save. Check the connection and try again.')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Lidl')
    expect(screen.queryByText('Expense added')).not.toBeInTheDocument()
  })

  it('moves focus into the opened form and back to its action button on Cancel', async () => {
    const user = userEvent.setup()
    await openScreen()
    const addIncome = screen.getByRole('button', { name: 'Add income' })

    await user.click(addIncome)

    await waitFor(() => expect(screen.getByLabelText(/^Date/)).toHaveFocus())
    expect(addIncome).toHaveAttribute('aria-expanded', 'true')

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('heading', { name: 'New Income' })).not.toBeInTheDocument()
    expect(addIncome).toHaveFocus()
  })

  it('closes a clean form on a month change without asking', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })

    expect(await screen.findByText('Balance at end of September 2026')).toBeInTheDocument()
    expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()
  })

  it('opens a new entry on the last day of a past viewed month', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-09`)
    await screen.findByText('Balance at end of September 2026')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled())

    await user.click(screen.getByRole('button', { name: 'Add expense' }))

    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-09-30')
  })

  it('asks before a month change discards unsaved changes, and keeps the month on Keep editing', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
    await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))
    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())

    expect(screen.getByLabelText('Month')).toHaveValue('2026-10')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Lidl')

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(await screen.findByText('Balance at end of September 2026')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()
  })

  it('asks before an account change discards unsaved changes', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    await user.click(screen.getByRole('combobox', { name: 'Account' }))
    await user.click(screen.getByRole('option', { name: 'Chase' }))

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(await screen.findByRole('heading', { level: 1, name: 'Chase' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()
  })

  it('disables the actions and offers Try again when the form options fail to load', async () => {
    getBanksMock.mockRejectedValueOnce(new Error('boom'))

    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('Unable to load the form options')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add expense' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'Add expense' })).toBeEnabled())
  })
})

describe('AccountScreenPage transfers, reconcile and row actions', () => {
  const EXPENSE: ExpenseDto = {
    id: 'e1',
    date: '2026-10-14',
    description: 'Tesco',
    value: 42.1,
    categoryId: 'category-groceries',
    categoryName: 'Groceries',
    paymentSourceBankId: BARCLAYS_ID,
    paymentSourceBankName: 'Barclays',
    creditCardId: null,
    creditCardName: null,
    chargeDate: null,
    invoiceDate: null,
    paymentStatus: 'ImmediatePayment',
    roundUpAmount: 0.9,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  }

  const TRANSFER: AccountTransferDto = {
    id: 't1',
    date: '2026-10-05',
    sourceAccountId: BARCLAYS_ID,
    sourceAccountName: 'Barclays',
    sourceCurrency: 'GBP',
    destinationAccountId: CHASE_ID,
    destinationAccountName: 'Chase',
    destinationCurrency: 'GBP',
    amount: 200,
    destinationAmount: 200,
    note: null,
  }

  const ADJUSTMENT: BalanceAdjustmentDto = {
    id: 'a1',
    date: '2026-10-01',
    bankId: BARCLAYS_ID,
    bankName: 'Barclays',
    targetBalance: 2800.25,
    delta: -1.5,
    note: 'Bank fee',
  }

  const ROWS: AccountTransactionDto[] = [
    accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', description: 'Tesco', amount: -42.1, roundUp: -0.9, expense: EXPENSE }),
    accountTransactionRow({ id: 't1', kind: 'TransferOut', date: '2026-10-05', counterpart: 'Chase', amount: -200, transfer: TRANSFER }),
    accountTransactionRow({ id: 'a1', kind: 'Adjustment', date: '2026-10-01', description: 'Bank fee', amount: -1.5, adjustment: ADJUSTMENT }),
  ]

  beforeEach(() => {
    withActions(BANK_ACTIONS)
    getAccountTransactionsMock.mockResolvedValue(ROWS)
    createAccountTransferMock.mockResolvedValue(TRANSFER)
    updateAccountTransferMock.mockResolvedValue(TRANSFER)
    updateExpenseMock.mockResolvedValue(EXPENSE)
    createBalanceAdjustmentMock.mockResolvedValue({ ...ADJUSTMENT, id: 'a2', targetBalance: 2790, delta: -10.25 })
  })

  async function openScreen() {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Transfer to' })).toBeEnabled())
  }

  function rowOf(text: string): HTMLElement {
    return screen.getByText(text).closest('tr') as HTMLElement
  }

  it('Transfer to sends from this account to the chosen destination, then closes and returns focus', async () => {
    const user = userEvent.setup()
    await openScreen()
    const transferTo = screen.getByRole('button', { name: 'Transfer to' })

    await user.click(transferTo)
    const form = screen.getByRole('heading', { name: 'New Transfer' }).closest('form') as HTMLElement
    expect(within(form).getByText('Barclays', { selector: 'span' })).toBeInTheDocument()
    expect(within(form).queryByLabelText(/^Amount received/)).not.toBeInTheDocument()
    await user.selectOptions(within(form).getByRole('combobox', { name: /^To/ }), CHASE_ID)
    await user.type(within(form).getByLabelText(/^Amount/), '200')
    await user.click(within(form).getByRole('button', { name: 'Add Transfer' }))

    await waitFor(() =>
      expect(createAccountTransferMock).toHaveBeenCalledWith({
        date: '2026-10-15',
        sourceAccountId: BARCLAYS_ID,
        destinationAccountId: CHASE_ID,
        amount: 200,
        destinationAmount: null,
        note: null,
      }),
    )
    expect(await screen.findByText('Transfer saved')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Transfer' })).not.toBeInTheDocument()
    await waitFor(() => expect(transferTo).toHaveFocus())
  })

  it('Transfer from a different currency asks for the amount received in this account', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Transfer from' }))
    await user.selectOptions(screen.getByRole('combobox', { name: /^From/ }), WISE_EUR_ID)
    await user.type(screen.getByRole('spinbutton', { name: /^Amount(?! received)/ }), '100')
    await user.type(screen.getByLabelText(/^Amount received \(GBP\)/), '86.20')
    await user.click(screen.getByRole('button', { name: 'Add Transfer' }))

    await waitFor(() =>
      expect(createAccountTransferMock).toHaveBeenCalledWith(
        expect.objectContaining({
          sourceAccountId: WISE_EUR_ID,
          destinationAccountId: BARCLAYS_ID,
          amount: 100,
          destinationAmount: 86.2,
        }),
      ),
    )
  })

  it('Adjust balance starts from the header balance, records the adjustment and keeps its confirmation', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Adjust balance' }))
    const form = screen.getByTestId('balance-adjustment-form-panel')
    expect(form).toHaveTextContent('Balance at end of October 2026: £2,800.25')
    expect(within(form).queryByRole('combobox', { name: /Bank/ })).not.toBeInTheDocument()
    await user.type(within(form).getByRole('spinbutton', { name: /^Target Balance/ }), '2790')
    await user.click(within(form).getByRole('button', { name: 'Adjust balance' }))

    await waitFor(() =>
      expect(createBalanceAdjustmentMock).toHaveBeenCalledWith(BARCLAYS_ID, { date: '2026-10-15', targetBalance: 2790, note: null }),
    )
    await waitFor(() => expect(screen.getAllByText('Balance adjusted').length).toBeGreaterThan(1))
    expect(screen.getByRole('heading', { name: 'Balance adjusted' })).toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Close' })).toHaveFocus())
  })

  it('[AC P60-F05-bank-account-screen-09] Edit opens the matching form pre-filled and Delete asks before calling the endpoint', async () => {
    const user = userEvent.setup()
    await openScreen()
    const editExpense = within(rowOf('Tesco')).getByRole('button', { name: 'Edit expense' })

    await user.click(editExpense)
    expect(screen.getByRole('heading', { name: 'Edit Expense' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Tesco')
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-14')
    await user.clear(screen.getByLabelText(/^Value/))
    await user.type(screen.getByLabelText(/^Value/), '45')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateExpenseMock).toHaveBeenCalledWith('e1', expect.objectContaining({ value: 45, paymentSourceBankId: BARCLAYS_ID })),
    )
    expect(await screen.findByText('Expense updated')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Edit Expense' })).not.toBeInTheDocument()
    await waitFor(() => expect(editExpense).toHaveFocus())

    await user.click(within(rowOf('Chase')).getByRole('button', { name: 'Delete transfer' }))
    const dialog = await findDialog('Delete transfer')
    expect(dialog).toHaveTextContent('Delete this transfer? This cannot be undone.')
    await user.click(within(dialog).getByText('Delete', { selector: 'button' }))

    await waitFor(() => expect(deleteAccountTransferMock).toHaveBeenCalledWith('t1'))
    expect(await screen.findByText('Transfer deleted')).toBeInTheDocument()
  })

  it('deletes nothing when the delete is cancelled, and returns focus to the row Delete button', async () => {
    const user = userEvent.setup()
    await openScreen()
    const deleteExpense = within(rowOf('Tesco')).getByRole('button', { name: 'Delete expense' })

    await user.click(deleteExpense)
    await user.click(await dialogButton('Delete expense', 'Cancel'))

    await waitFor(() => expect(deleteExpense).toHaveFocus())
    expect(deleteExpenseMock).not.toHaveBeenCalled()
  })

  it('edits a transfer with this account locked on its side and its counterpart selected', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(within(rowOf('Chase')).getByRole('button', { name: 'Edit transfer' }))
    expect(screen.getByRole('heading', { name: 'Edit Transfer' })).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^To/ })).toHaveValue(CHASE_ID)
    await user.clear(screen.getByLabelText(/^Amount/))
    await user.type(screen.getByLabelText(/^Amount/), '250')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateAccountTransferMock).toHaveBeenCalledWith(
        't1',
        expect.objectContaining({ sourceAccountId: BARCLAYS_ID, destinationAccountId: CHASE_ID, amount: 250 }),
      ),
    )
    expect(await screen.findByText('Transfer updated')).toBeInTheDocument()
  })

  it('deletes an adjustment through its bank route and closes its open edit form', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(within(rowOf('Bank fee')).getByRole('button', { name: 'Edit balance adjustment' }))
    expect(screen.getByRole('heading', { name: 'Edit balance adjustment' })).toBeInTheDocument()
    await user.click(within(rowOf('Bank fee')).getByRole('button', { name: 'Delete balance adjustment' }))
    await user.click(await dialogButton('Delete balance adjustment', 'Delete'))

    await waitFor(() => expect(deleteBalanceAdjustmentMock).toHaveBeenCalledWith(BARCLAYS_ID, 'a1'))
    expect(await screen.findByText('Balance adjustment deleted')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Edit balance adjustment' })).not.toBeInTheDocument()
  })

  it('keeps the delete dialog open with the reason when the delete fails', async () => {
    const user = userEvent.setup()
    deleteExpenseMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    await openScreen()

    await user.click(within(rowOf('Tesco')).getByRole('button', { name: 'Delete expense' }))
    const dialog = await findDialog('Delete expense')
    await user.click(within(dialog).getByText('Delete', { selector: 'button' }))

    expect(await within(dialog).findByText('Could not delete. Check the connection and try again.')).toBeInTheDocument()
    expect(within(dialog).getByText('Delete', { selector: 'button' })).toBeEnabled()
    expect(screen.queryByText('Expense deleted')).not.toBeInTheDocument()

    await user.click(within(dialog).getByText('Cancel', { selector: 'button' }))
    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
  })

  it('asks to discard unsaved changes before opening a row edit', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Transfer to' }))
    await user.type(screen.getByLabelText(/^Amount/), '10')
    await user.click(within(rowOf('Tesco')).getByRole('button', { name: 'Edit expense' }))

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(screen.getByRole('heading', { name: 'Edit Expense' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Transfer' })).not.toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-11] focus moves into a row edit and back to the row Edit button on Cancel', async () => {
    const user = userEvent.setup()
    await openScreen()
    const editAdjustment = within(rowOf('Bank fee')).getByRole('button', { name: 'Edit balance adjustment' })

    await user.click(editAdjustment)
    await waitFor(() => expect(screen.getByLabelText(/^Date/)).toHaveFocus())
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('heading', { name: 'Edit balance adjustment' })).not.toBeInTheDocument()
    expect(editAdjustment).toHaveFocus()
  })
})

describe('AccountScreenPage card screen', () => {
  const TAXI: ExpenseDto = {
    id: 'c1',
    date: '2026-09-28',
    description: 'Taxi',
    value: 30,
    categoryId: 'category-groceries',
    categoryName: 'Groceries',
    paymentSourceBankId: null,
    paymentSourceBankName: null,
    creditCardId: AMEX_ID,
    creditCardName: 'BA Amex',
    chargeDate: '2026-09-28',
    invoiceDate: '2026-10-01',
    paymentStatus: 'CreditCardCharge',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  }

  const CARD_ROWS: AccountTransactionDto[] = [
    accountTransactionRow({ id: 'c1', kind: 'CardCharge', date: '2026-09-28', description: 'Taxi', amount: -30, canEdit: true, expense: TAXI }),
    accountTransactionRow({ id: 'p1', kind: 'CardPayment', date: '2026-10-12', counterpart: 'Barclays', amount: 480, canEdit: false }),
  ]

  beforeEach(() => {
    getAccountTransactionsMock.mockResolvedValue(CARD_ROWS)
  })

  it('shows no tabs on a bank', async () => {
    getAccountTransactionsMock.mockResolvedValue(TRANSACTIONS)
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    expect(await screen.findByText('Tesco')).toBeInTheDocument()
    expect(screen.queryByRole('tablist')).not.toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: /^Date/ })).toBeInTheDocument()
  })

  it('[AC P60-F08-statement-payment-and-card-screen-12] a card opens on Transactions with the Purchased column, card payment rows and charge actions', async () => {
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    expect(await screen.findByText('Taxi')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Transactions', selected: true })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: /^Purchased/ })).toBeInTheDocument()
    expect(screen.getByText('Card payment')).toBeInTheDocument()
    const taxiRow = screen.getByText('Taxi').closest('tr') as HTMLElement
    expect(within(taxiRow).getByRole('button', { name: 'Edit card charge' })).toBeInTheDocument()
    expect(within(taxiRow).getByRole('button', { name: 'Delete card charge' })).toBeInTheDocument()
    expect(within(taxiRow).getByRole('button', { name: 'Move card charge to a later statement' })).toBeInTheDocument()
    expect(within(screen.getByText('Card payment').closest('tr') as HTMLElement).queryByRole('button')).not.toBeInTheDocument()
    expect(getAccountStatementsMock).not.toHaveBeenCalled()
  })

  it('adds a charge with the card locked and the invoice month from the date', async () => {
    const user = userEvent.setup()
    withActions(['AddCharge'])
    createExpenseMock.mockResolvedValue({ ...TAXI, id: 'c2', description: 'Lunch' })
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('button', { name: 'Add charge' }))
    const form = screen.getByRole('heading', { name: 'New Charge' }).closest('form') as HTMLElement
    expect(within(form).getByText('BA Amex')).toBeInTheDocument()
    expect(within(form).queryByLabelText(/^Payment Source/)).not.toBeInTheDocument()
    expect(within(form).getByLabelText('Statement month')).toHaveValue('2026-10')
    await user.type(within(form).getByLabelText(/^Description/), 'Lunch')
    await user.type(within(form).getByLabelText(/^Value/), '12.40')
    await user.click(within(form).getByRole('button', { name: 'Add Charge' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(
        expect.objectContaining({
          date: '2026-10-15',
          description: 'Lunch',
          value: 12.4,
          creditCardId: AMEX_ID,
          paymentSourceBankId: null,
          invoiceDate: '2026-10-01',
          roundUpAmount: null,
        }),
      ),
    )
    expect(await screen.findByText('Charge added')).toBeInTheDocument()
  })

  it('edits an unpaid charge in card mode with the card locked', async () => {
    const user = userEvent.setup()
    updateExpenseMock.mockResolvedValue({ ...TAXI, value: 32 })
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('button', { name: 'Edit card charge' }))
    const form = screen.getByRole('heading', { name: 'Edit Charge' }).closest('form') as HTMLElement
    expect(within(form).getByText('BA Amex')).toBeInTheDocument()
    expect(within(form).getByLabelText('Statement month')).toHaveValue('2026-10')
    await user.clear(within(form).getByLabelText(/^Value/))
    await user.type(within(form).getByLabelText(/^Value/), '32')
    await user.click(within(form).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateExpenseMock).toHaveBeenCalledWith(
        'c1',
        expect.objectContaining({ value: 32, creditCardId: AMEX_ID, paymentSourceBankId: null, invoiceDate: '2026-10-01' }),
      ),
    )
    expect(await screen.findByText('Charge updated')).toBeInTheDocument()
  })

  it('[AC P60-F08-statement-payment-and-card-screen-07] moves a charge to a later unpaid statement and focuses the Transactions heading', async () => {
    const user = userEvent.setup()
    moveChargeToStatementMock.mockResolvedValue({ ...TAXI, invoiceDate: '2026-12-01' })
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('button', { name: 'Move card charge to a later statement' }))
    const form = screen.getByRole('heading', { name: 'Move charge to a later statement' }).closest('form') as HTMLElement
    expect(within(form).getByText('Taxi (28/09/2026, 30.00) is on the October 2026 statement.')).toBeInTheDocument()
    const statement = within(form).getByLabelText(/^Statement/)
    await waitFor(() => expect(within(statement).getAllByRole('option')).toHaveLength(13))
    expect(within(statement).getByRole('option', { name: 'November 2026' })).toBeInTheDocument()
    expect(within(statement).getByRole('option', { name: 'December 2026 (new)' })).toBeInTheDocument()

    await user.click(within(form).getByRole('button', { name: 'Move charge' }))
    expect(within(form).getByText('Select a statement.')).toBeInTheDocument()
    expect(moveChargeToStatementMock).not.toHaveBeenCalled()

    getAccountTransactionsMock.mockResolvedValue(CARD_ROWS.slice(1))
    await user.selectOptions(statement, '2026-12')
    await user.click(within(form).getByRole('button', { name: 'Move charge' }))

    expect(moveChargeToStatementMock).toHaveBeenCalledWith('c1', { year: 2026, month: 12 })
    expect(await screen.findByText('Charge moved')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByText('Taxi')).not.toBeInTheDocument())
    expect(screen.queryByRole('heading', { name: 'Move charge to a later statement' })).not.toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Transactions' })).toHaveFocus())
  })

  it('deletes an unpaid charge after confirmation', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('button', { name: 'Delete card charge' }))
    getAccountTransactionsMock.mockResolvedValue(CARD_ROWS.slice(1))
    await user.click(await dialogButton('Delete card charge', 'Delete'))

    expect(deleteExpenseMock).toHaveBeenCalledWith('c1')
    expect(await screen.findByText('Card charge deleted')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByText('Taxi')).not.toBeInTheDocument())
  })

  it('keeps the move form open with the server message when the move is refused', async () => {
    const user = userEvent.setup()
    moveChargeToStatementMock.mockRejectedValue(new ApiError('Charges can only move to a later, unpaid statement.', 400))
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('button', { name: 'Move card charge to a later statement' }))
    const statement = screen.getByLabelText(/^Statement/)
    await waitFor(() => expect(within(statement).getAllByRole('option')).toHaveLength(13))
    await user.selectOptions(statement, '2026-11')
    await user.click(screen.getByRole('button', { name: 'Move charge' }))

    expect(await screen.findByText('Charges can only move to a later, unpaid statement.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Move charge to a later statement' })).toBeInTheDocument()
  })

  it('asks before leaving a move form with a chosen statement', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('button', { name: 'Move card charge to a later statement' }))
    const statement = screen.getByLabelText(/^Statement/)
    await waitFor(() => expect(within(statement).getAllByRole('option')).toHaveLength(13))
    await user.selectOptions(statement, '2026-11')
    await user.click(screen.getByRole('tab', { name: 'Statements' }))

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
    await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))
    expect(screen.getByLabelText(/^Statement/)).toHaveValue('2026-11')
  })

  it('keeps the tab in the URL across tab and month changes', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

    await user.click(await screen.findByRole('tab', { name: 'Statements' }))

    expect(screen.getByText(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)).toBeInTheDocument()
    expect(await screen.findByRole('row', { current: true })).toHaveTextContent('October 2026')

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })

    expect(await screen.findByText(`/cashflow/accounts/${AMEX_ID}?month=2026-09&tab=statements`)).toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('row', { current: true })).toHaveTextContent('September 2026'))

    await user.click(screen.getByRole('tab', { name: 'Transactions' }))

    expect(screen.getByText(`/cashflow/accounts/${AMEX_ID}?month=2026-09`)).toBeInTheDocument()
  })

  it('[AC P60-F08-statement-payment-and-card-screen-11] pays a statement from the Statements tab and refreshes the screen', async () => {
    const user = userEvent.setup()
    markCardStatementPaidMock.mockResolvedValue({
      id: 's-oct', creditCardId: AMEX_ID, creditCardName: 'BA Amex', year: 2026, month: 10, isPaid: true, outstandingTotal: 0, accumulatedOutstandingTotal: 0,
    })
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    await user.click(await screen.findByRole('button', { name: 'Pay October 2026 statement' }))
    await user.selectOptions(screen.getByLabelText(/^Bank/), BARCLAYS_ID)
    await user.click(screen.getByRole('button', { name: 'Pay statement' }))

    expect(markCardStatementPaidMock).toHaveBeenCalledWith('s-oct', { paymentSourceBankId: BARCLAYS_ID, date: '2026-10-15' })
    expect(await screen.findByText('Statement paid')).toBeInTheDocument()
    await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(2))
    expect(getAccountMock).toHaveBeenCalledTimes(2)
    expect(screen.queryByRole('button', { name: 'Pay statement' })).not.toBeInTheDocument()
  })

  it('asks before a tab change discards an unsaved payment', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    await user.click(await screen.findByRole('button', { name: 'Pay October 2026 statement' }))
    await user.selectOptions(screen.getByLabelText(/^Bank/), BARCLAYS_ID)
    await user.click(screen.getByRole('tab', { name: 'Transactions' }))
    await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    expect(screen.getByLabelText(/^Bank/)).toHaveValue(BARCLAYS_ID)

    await user.click(screen.getByRole('tab', { name: 'Transactions' }))
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(await screen.findByRole('tab', { name: 'Transactions', selected: true })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Pay statement' })).not.toBeInTheDocument()
  })

  it('shows no Pay or Unmark on an inactive card', async () => {
    getAccountMock.mockImplementation((id, year, month) => Promise.resolve({ ...detailFor(id, year, month), isActive: false }))
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    expect(await screen.findByText('Paid 12 Oct 2026 from Barclays')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^(Pay|Unmark) / })).not.toBeInTheDocument()
  })
})

describe('AccountScreenPage bank side card payments', () => {
  beforeEach(() => {
    withActions(BANK_ACTIONS)
  })

  async function openBank() {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await screen.findByRole('heading', { level: 1, name: 'Barclays' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Pay card statement' })).toBeEnabled())
  }

  function payForm() {
    return screen.getByRole('heading', { name: 'Pay card statement' }).closest('form') as HTMLElement
  }

  it('[AC P60-F08-statement-payment-and-card-screen-04] Pay card statement chooses a card and statement and pays it with this bank', async () => {
    const user = userEvent.setup()
    markCardStatementPaidMock.mockResolvedValue({
      id: 's-oct',
      creditCardId: AMEX_ID,
      creditCardName: 'BA Amex',
      year: 2026,
      month: 10,
      isPaid: true,
      outstandingTotal: 0,
      accumulatedOutstandingTotal: 12,
    })
    await openBank()
    getAccountMock.mockClear()
    const action = screen.getByRole('button', { name: 'Pay card statement' })

    await user.click(action)
    const card = await screen.findByRole('combobox', { name: /^Card/ })
    await waitFor(() => expect(card).toHaveFocus())
    await waitFor(() => expect(within(card).getAllByRole('option').map((option) => option.textContent)).toEqual(['Select a card', 'BA Amex', 'Platinum Visa 6007']))
    expect(within(payForm()).getByText('Barclays')).toBeInTheDocument()
    await user.selectOptions(card, AMEX_ID)
    const statement = screen.getByRole('combobox', { name: /^Statement/ })
    expect(within(statement).getAllByRole('option').map((option) => option.textContent)).toEqual([
      'Select a statement',
      'October 2026 · £210.45',
      'November 2026 · £12.00',
    ])
    await user.selectOptions(statement, 's-oct')
    expect(screen.getByLabelText(/^Amount/)).toHaveValue('£210.45')
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-15')
    await user.click(within(payForm()).getByRole('button', { name: 'Pay card statement' }))

    await waitFor(() => expect(markCardStatementPaidMock).toHaveBeenCalledWith('s-oct', { paymentSourceBankId: BARCLAYS_ID, date: '2026-10-15' }))
    expect(await screen.findByText('Statement paid')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Pay card statement' })).not.toBeInTheDocument()
    await waitFor(() => expect(getAccountMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 10))
    await waitFor(() => expect(action).toHaveFocus())
  })

  it('selects the only statement of a card, and an empty submit focuses the first missing field', async () => {
    const user = userEvent.setup()
    await openBank()

    await user.click(screen.getByRole('button', { name: 'Pay card statement' }))
    const card = await screen.findByRole('combobox', { name: /^Card/ })
    await waitFor(() => expect(within(card).getAllByRole('option')).toHaveLength(3))
    await user.click(within(payForm()).getByRole('button', { name: 'Pay card statement' }))

    expect(await screen.findByText('Select a card.')).toBeInTheDocument()
    expect(screen.getByText('Select a statement.')).toBeInTheDocument()
    expect(card).toHaveFocus()
    await user.selectOptions(card, VISA_ID)
    expect(screen.getByRole('combobox', { name: /^Statement/ })).toHaveValue('v-oct')
    expect(screen.getByLabelText(/^Amount/)).toHaveValue('£80.00')
    expect(markCardStatementPaidMock).not.toHaveBeenCalled()
  })

  it('keeps the form with the server message and reloads the statements when the payment is refused', async () => {
    const user = userEvent.setup()
    markCardStatementPaidMock.mockRejectedValue(new ApiError('This statement is already paid.', 409))
    await openBank()

    await user.click(screen.getByRole('button', { name: 'Pay card statement' }))
    const card = await screen.findByRole('combobox', { name: /^Card/ })
    await waitFor(() => expect(within(card).getAllByRole('option')).toHaveLength(3))
    await user.selectOptions(card, VISA_ID)
    getPayableStatementsMock.mockClear()
    await user.click(within(payForm()).getByRole('button', { name: 'Pay card statement' }))

    expect(await within(payForm()).findByText('This statement is already paid.')).toBeInTheDocument()
    await waitFor(() => expect(getPayableStatementsMock).toHaveBeenCalledTimes(1))
  })

  it('says when no statement is waiting and offers Try again when the list fails to load', async () => {
    const user = userEvent.setup()
    getPayableStatementsMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue([])
    await openBank()

    await user.click(screen.getByRole('button', { name: 'Pay card statement' }))
    await user.click(await screen.findByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('No card statement is waiting to be paid.')).toBeInTheDocument()
    expect(within(payForm()).getByRole('button', { name: 'Pay card statement' })).toBeDisabled()
  })

  it('asks before leaving a pay form with a chosen card', async () => {
    const user = userEvent.setup()
    await openBank()

    await user.click(screen.getByRole('button', { name: 'Pay card statement' }))
    const card = await screen.findByRole('combobox', { name: /^Card/ })
    await waitFor(() => expect(within(card).getAllByRole('option')).toHaveLength(3))
    await user.selectOptions(card, VISA_ID)
    await user.click(screen.getByRole('button', { name: 'Add expense' }))

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
  })

  it('[AC P60-F08-statement-payment-and-card-screen-02] a card payment row on a bank opens the card statement it paid', async () => {
    const user = userEvent.setup()
    getAccountTransactionsMock.mockResolvedValue([...TRANSACTIONS, CARD_PAYMENT_ROW])
    await openBank()

    const link = await screen.findByRole('link', { name: 'BA Amex payment – Sep 2026 statement' })
    const row = link.closest('tr') as HTMLElement
    expect(within(row).getByText('Card payment')).toBeInTheDocument()
    expect(within(row).queryByRole('button', { name: /^(Edit|Delete) / })).not.toBeInTheDocument()
    await user.click(link)

    expect(await screen.findByText(`/cashflow/accounts/${AMEX_ID}?month=2026-09&tab=statements`)).toBeInTheDocument()
  })

  it('leaves a modified click on a card payment link to the browser', async () => {
    getAccountTransactionsMock.mockResolvedValue([...TRANSACTIONS, CARD_PAYMENT_ROW])
    await openBank()

    fireEvent.click(screen.getByRole('link', { name: 'BA Amex payment \u2013 Sep 2026 statement' }), { ctrlKey: true })

    expect(screen.getByText(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)).toBeInTheDocument()
  })

  it('keeps Pay card statement available when the expense form options fail to load', async () => {
    getCategoriesMock.mockRejectedValue(new ApiError('Server error', 500))
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    await screen.findByRole('heading', { level: 1, name: 'Barclays' })

    await waitFor(() => expect(screen.getByRole('button', { name: 'Pay card statement' })).toBeEnabled())
    expect(screen.getByRole('button', { name: 'Add expense' })).toBeDisabled()
  })

  it('asks before a card payment link discards unsaved changes, and stays on Keep editing', async () => {
    const user = userEvent.setup()
    getAccountTransactionsMock.mockResolvedValue([...TRANSACTIONS, CARD_PAYMENT_ROW])
    await openBank()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(await screen.findByLabelText(/^Description/), 'Lidl')
    await user.click(screen.getByRole('link', { name: 'BA Amex payment – Sep 2026 statement' }))
    await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))

    expect(screen.getByText(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)).toBeInTheDocument()
  })
})
