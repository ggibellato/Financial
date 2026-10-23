import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { Toaster } from '@fluentui/react-components'
import type { AccountDetailDto, AccountDto, AccountTransactionDto, BankDto, CategoryDto, ExpenseDto, IncomeDto, IncomeSourceDto } from '../../api/types'
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
} = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getAccountMock: vi.fn<FinancialApiClient['getAccount']>(),
  getAccountTransactionsMock: vi.fn<FinancialApiClient['getAccountTransactions']>(),
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  getCategoriesMock: vi.fn<FinancialApiClient['getCategories']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  createExpenseMock: vi.fn<FinancialApiClient['createExpense']>(),
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
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
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'
const CHASE_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000002'

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

function renderScreen(entry: string) {
  return render(
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route
          path="/cashflow/accounts/:accountId"
          element={
            <>
              <Toaster toasterId={APP_TOASTER_ID} />
              <AccountScreenPage />
              <LocationText />
            </>
          }
        />
        <Route path="/cashflow/accounts" element={<p>Accounts page</p>} />
      </Routes>
    </MemoryRouter>,
  )
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
    expect(actions.map((button) => button.textContent)).toEqual(['Add expense', 'Add income'])
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

    expect(screen.getByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Keep editing' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Lidl')
    expect(screen.queryByRole('heading', { name: 'New Income' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add income' }))
    await user.click(screen.getByRole('button', { name: 'Discard' }))

    expect(screen.getByRole('heading', { name: 'New Income' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'New Expense' })).not.toBeInTheDocument()
  })

  it('switches between clean forms without asking', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.click(screen.getByRole('button', { name: 'Add income' }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'New Income' })).toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-07] Add expense saves with this bank, refreshes header and grid, and stays open with the last date and category', async () => {
    const user = userEvent.setup()
    await openScreen()
    getAccountMock.mockClear()
    getAccountTransactionsMock.mockClear()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    expect(screen.getByLabelText('Payment Source')).toHaveValue('Barclays')
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

  it('Add income saves with this bank, shows the toast and stays open', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add income' }))
    expect(screen.getByLabelText('Bank')).toHaveValue('Barclays')
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

  it('asks before a month change discards unsaved changes, and keeps the month on Keep editing', async () => {
    const user = userEvent.setup()
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add expense' }))
    await user.type(screen.getByLabelText(/^Description/), 'Lidl')
    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })

    expect(screen.getByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Keep editing' }))

    expect(screen.getByLabelText('Month')).toHaveValue('2026-10')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Lidl')

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })
    await user.click(screen.getByRole('button', { name: 'Discard' }))

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

    expect(screen.getByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Discard' }))

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
