import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { Toaster } from '@fluentui/react-components'
import type {
  AccountDetailDto,
  AccountDto,
  AccountTransactionDto,
  AccountTransferDto,
  BalanceAdjustmentDto,
  BankDto,
  CategoryDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
} from '../../api/types'
import { APP_TOASTER_ID } from '../../hooks/useSuccessToast'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountScreenPage from '../AccountScreenPage'
import { accountTransactionRow } from '../../test/accountTransactionRow'

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
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'
const CHASE_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000002'
const WISE_EUR_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000004'

const ACCOUNTS: AccountDto[] = [
  { id: BARCLAYS_ID, name: 'Barclays', templateName: 'Bank', currency: 'GBP', isActive: true },
  { id: CHASE_ID, name: 'Chase', templateName: 'Bank', currency: 'GBP', isActive: true },
]

function detailFor(id: string, year: number, month: number): AccountDetailDto {
  const account = ACCOUNTS.find((candidate) => candidate.id === id)!
  return { ...account, year, month, balance: id === BARCLAYS_ID ? 2800.25 : 500, allowedActions: [] }
}

const TRANSACTIONS: AccountTransactionDto[] = [
  accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', description: 'Tesco', categoryName: 'Groceries', amount: -42.1, roundUp: -0.9, canEdit: true }),
  accountTransactionRow({ id: 't1', kind: 'TransferOut', date: '2026-10-05', counterpart: 'Chase', amount: -200, canEdit: true }),
]

const BANKS: BankDto[] = [
  { id: BARCLAYS_ID, name: 'Barclays', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true },
  { id: CHASE_ID, name: 'Chase', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-groceries', name: 'Groceries', active: true, isInvestment: false, isTithe: false, hasReferences: true },
  { id: 'category-home', name: 'Home', active: true, isInvestment: false, isTithe: false, hasReferences: true },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: 'source-salary', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false, hasReferences: true },
]

const BANK_ACTIONS = ['AddExpense', 'AddIncome', 'TransferTo', 'TransferFrom', 'PayRecurringBill', 'Reconcile']

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

  it('renders the actions this stage has forms for, in the order the API sends them', async () => {
    await openScreen()

    const actions = within(screen.getByRole('group', { name: 'Account actions' })).getAllByRole('button')
    expect(actions.map((button) => button.textContent)).toEqual([
      'Add expense',
      'Add income',
      'Transfer to',
      'Transfer from',
      'Adjust balance',
    ])
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
