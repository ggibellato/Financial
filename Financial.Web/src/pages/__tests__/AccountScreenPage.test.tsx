import { act } from '@testing-library/react'
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
  CardCreditDto,
  CategoryDto,
  ExpenseDto,
  IncomeDto,
  IncomeSourceDto,
  PayableStatementDto,
  PotOperationDto,
  PotsSummaryDto,
  RecurringBillDto,
  ReserveMovementDto,
} from '../../api/types'
import { APP_TOASTER_ID } from '../../hooks/useSuccessToast'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import AccountScreenPage from '../AccountScreenPage'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { recurringBill } from '../../test/recurringBill'
import { account, cardAccount, potsAccount, settlementAccount } from '../../test/account'

const {
  getAccountsMock,
  getAccountMock,
  getAccountTransactionsMock,
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
  createCardCreditRefundMock,
  deleteCardCreditRefundMock,
  getCardsInCreditMock,
  createBalanceAdjustmentMock,
  deleteBalanceAdjustmentMock,
  getMensaisBillsMock,
  markMensaisBillPaidMock,
  getAccountStatementsMock,
  setStatementDueDateMock,
  markCardStatementPaidMock,
  unmarkCardStatementPaidMock,
  moveChargeToStatementMock,
  getPayableStatementsMock,
  getPotsMock,
  updatePotsMock,
  updateReserveMovementMock,
  deleteReserveMovementMock,
  createPotDepositMock,
  createPotWithdrawalMock,
  updatePotWithdrawalMock,
  getPotOperationMock,
  deletePotOperationMock,
  getIncomesByMonthMock,
  createSettlementEntryMock,
  updateSettlementEntryMock,
  settleAccountMock,
} = vi.hoisted(() => ({
  createSettlementEntryMock: vi.fn<FinancialApiClient['createSettlementEntry']>(),
  updateSettlementEntryMock: vi.fn<FinancialApiClient['updateSettlementEntry']>(),
  settleAccountMock: vi.fn<FinancialApiClient['settleAccount']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getAccountMock: vi.fn<FinancialApiClient['getAccount']>(),
  getAccountTransactionsMock: vi.fn<FinancialApiClient['getAccountTransactions']>(),
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
  createCardCreditRefundMock: vi.fn<FinancialApiClient['createCardCreditRefund']>(),
  deleteCardCreditRefundMock: vi.fn<FinancialApiClient['deleteCardCreditRefund']>(),
  getCardsInCreditMock: vi.fn<FinancialApiClient['getCardsInCredit']>(),
  createBalanceAdjustmentMock: vi.fn<FinancialApiClient['createBalanceAdjustment']>(),
  deleteBalanceAdjustmentMock: vi.fn<FinancialApiClient['deleteBalanceAdjustment']>(),
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  markMensaisBillPaidMock: vi.fn<FinancialApiClient['markMensaisBillPaid']>(),
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
  setStatementDueDateMock: vi.fn<FinancialApiClient['setStatementDueDate']>(),
  markCardStatementPaidMock: vi.fn<FinancialApiClient['markCardStatementPaid']>(),
  unmarkCardStatementPaidMock: vi.fn<FinancialApiClient['unmarkCardStatementPaid']>(),
  moveChargeToStatementMock: vi.fn<FinancialApiClient['moveChargeToStatement']>(),
  getPayableStatementsMock: vi.fn<FinancialApiClient['getPayableStatements']>(),
  getPotsMock: vi.fn<FinancialApiClient['getPots']>(),
  updatePotsMock: vi.fn<FinancialApiClient['updatePots']>(),
  updateReserveMovementMock: vi.fn<FinancialApiClient['updateReserveMovement']>(),
  deleteReserveMovementMock: vi.fn<FinancialApiClient['deleteReserveMovement']>(),
  createPotDepositMock: vi.fn<FinancialApiClient['createPotDeposit']>(),
  createPotWithdrawalMock: vi.fn<FinancialApiClient['createPotWithdrawal']>(),
  updatePotWithdrawalMock: vi.fn<FinancialApiClient['updatePotWithdrawal']>(),
  getPotOperationMock: vi.fn<FinancialApiClient['getPotOperation']>(),
  deletePotOperationMock: vi.fn<FinancialApiClient['deletePotOperation']>(),
  getIncomesByMonthMock: vi.fn<FinancialApiClient['getIncomesByMonth']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createSettlementEntry: createSettlementEntryMock,
    updateSettlementEntry: updateSettlementEntryMock,
    settleAccount: settleAccountMock,
    getAccounts: getAccountsMock,
    getAccount: getAccountMock,
    getAccountTransactions: getAccountTransactionsMock,
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
    createCardCreditRefund: createCardCreditRefundMock,
    deleteCardCreditRefund: deleteCardCreditRefundMock,
    getCardsInCredit: getCardsInCreditMock,
    createBalanceAdjustment: createBalanceAdjustmentMock,
    deleteBalanceAdjustment: deleteBalanceAdjustmentMock,
    getMensaisBills: getMensaisBillsMock,
    markMensaisBillPaid: markMensaisBillPaidMock,
    getAccountStatements: getAccountStatementsMock,
    setStatementDueDate: setStatementDueDateMock,
    markCardStatementPaid: markCardStatementPaidMock,
    unmarkCardStatementPaid: unmarkCardStatementPaidMock,
    moveChargeToStatement: moveChargeToStatementMock,
    getPayableStatements: getPayableStatementsMock,
    getPots: getPotsMock,
    updatePots: updatePotsMock,
    updateReserveMovement: updateReserveMovementMock,
    deleteReserveMovement: deleteReserveMovementMock,
    createPotDeposit: createPotDepositMock,
    createPotWithdrawal: createPotWithdrawalMock,
    updatePotWithdrawal: updatePotWithdrawalMock,
    getPotOperation: getPotOperationMock,
    deletePotOperation: deletePotOperationMock,
    getIncomesByMonth: getIncomesByMonthMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'
const AMEX_ID = '8f3b1c1a-2e3a-4b1a-9a7f-500000000001'
const CHASE_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000002'
const WISE_EUR_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000004'

const ACCOUNTS: AccountDto[] = [
  account({ id: BARCLAYS_ID, name: 'Barclays', roundUpEnabled: true }),
  account({ id: CHASE_ID, name: 'Chase' }),
  cardAccount({ id: AMEX_ID, name: 'BA Amex' }),
]

function detailFor(id: string, year: number, month: number): AccountDetailDto {
  const account = ACCOUNTS.find((candidate) => candidate.id === id)!
  const isCard = id === AMEX_ID
  return { ...account, year, month, balance: id === BARCLAYS_ID ? 2800.25 : 500, balanceIsOwed: isCard, hasStatements: isCard, totalOwed: isCard ? 740.12 : null, viewedStatement: null, allowedActions: isCard ? ['AddCharge'] : [], disabledActions: {}, currentBalances: [] }
}

const TRANSACTIONS: AccountTransactionDto[] = [
  accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-14', description: 'Tesco', categoryName: 'Groceries', amount: -42.1, roundUp: -0.9, canEdit: true }),
  accountTransactionRow({ id: 't1', kind: 'TransferOut', date: '2026-10-05', counterpart: 'Chase', amount: -200, canEdit: true }),
]

const CARD_STATEMENTS: AccountStatementDto[] = [
  { creditIn: 0, creditCarried: 0, status: 'NothingDue', paidAmount: null, id: 's-nov', year: 2026, month: 11, total: 0, outstandingTotal: 0, isPaid: false, dueDate: null, paidOn: null, paidFromAccountName: null },
  { creditIn: 0, creditCarried: 0, status: 'Unpaid', paidAmount: null, id: 's-oct', year: 2026, month: 10, total: 210.45, outstandingTotal: 210.45, isPaid: false, dueDate: '2026-11-03', paidOn: null, paidFromAccountName: null },
  { creditIn: 0, creditCarried: 0, status: 'Paid', paidAmount: 512.3, id: 's-sep', year: 2026, month: 9, total: 512.3, outstandingTotal: 0, isPaid: true, dueDate: '2026-10-03', paidOn: '2026-10-12', paidFromAccountName: 'Barclays' },
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

const RESERVA_ID = '8f3b1c1a-2e3a-4b1a-9a7f-700000000001'

const POTS_SUMMARY: PotsSummaryDto = {
  potsAccountId: RESERVA_ID,
  currency: 'GBP',
  total: 1500.5,
  pots: [
    { id: 'pot-home', name: 'Home', splitPercentage: 60, isActive: true, balance: 1000.25 },
    { id: 'pot-trips', name: 'Trips', splitPercentage: 40, isActive: true, balance: 500.25 },
    { id: 'pot-old', name: 'Old car', splitPercentage: 0, isActive: false, balance: 0 },
  ],
}

function movementRow(movement: Partial<ReserveMovementDto> & { id: string }, row: Partial<AccountTransactionDto> = {}): AccountTransactionDto {
  const fields: ReserveMovementDto = { bucketId: 'pot-home', bucketName: 'Home', amount: 25, date: '2026-10-05', description: 'Bonus', incomeId: null, splitLineCount: 1, ...movement }
  return accountTransactionRow({
    id: fields.id,
    kind: 'PotTransaction',
    date: fields.date,
    description: fields.description,
    counterpart: fields.bucketName,
    amount: fields.amount,
    movement: fields,
    ...row,
  })
}

const SAVED_OPERATION: PotOperationDto = {
  operationId: 'op-new',
  kind: 'Deposit',
  potsAccountId: RESERVA_ID,
  date: '2026-10-15',
  description: 'Saved',
  amount: 1,
  into: null,
  potId: null,
  bankId: null,
  categoryId: null,
  incomeId: null,
  fundingLines: [],
}

const LEGACY_WITHDRAWAL = movementRow({ id: 'm-boots', bucketId: 'pot-trips', bucketName: 'Trips', amount: -20, date: '2026-10-09', description: 'Boots' })

function withPotsAccount(transactions: AccountTransactionDto[] = [LEGACY_WITHDRAWAL]) {
  const reserva = potsAccount({ id: RESERVA_ID, name: 'Reserva' })
  getAccountsMock.mockResolvedValue([...ACCOUNTS, reserva])
  getAccountMock.mockImplementation((id, year, month) =>
    Promise.resolve(
      id === RESERVA_ID
        ? { ...reserva, year, month, balance: 1500.5, balanceIsOwed: false, hasStatements: false, totalOwed: null, viewedStatement: null, allowedActions: ['Deposit', 'Withdraw', 'ManagePots'], disabledActions: {}, currentBalances: [] }
        : detailFor(id, year, month),
    ),
  )
  getAccountTransactionsMock.mockResolvedValue(transactions)
}

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
  createCardCreditRefundMock.mockReset()
  getCardsInCreditMock.mockReset().mockResolvedValue([])
  deleteCardCreditRefundMock.mockReset()
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
  setStatementDueDateMock.mockReset().mockResolvedValue(undefined)
  markCardStatementPaidMock.mockReset()
  unmarkCardStatementPaidMock.mockReset()
  moveChargeToStatementMock.mockReset()
  getPayableStatementsMock.mockReset().mockResolvedValue(PAYABLE_STATEMENTS)
  getPotsMock.mockReset().mockResolvedValue(POTS_SUMMARY)
  updatePotsMock.mockReset().mockResolvedValue(undefined)
  updateReserveMovementMock.mockReset()
  deleteReserveMovementMock.mockReset().mockResolvedValue(undefined)
  createPotDepositMock.mockReset().mockResolvedValue(SAVED_OPERATION)
  createPotWithdrawalMock.mockReset().mockResolvedValue(SAVED_OPERATION)
  updatePotWithdrawalMock.mockReset()
  getPotOperationMock.mockReset()
  deletePotOperationMock.mockReset()
  getIncomesByMonthMock.mockReset()
  sessionStorage.clear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('AccountScreenPage deposit and withdrawal forms', () => {
  const OPERATION_LINK = { operationId: 'op1', potsAccountId: RESERVA_ID, year: 2026, month: 10, incomeId: null }
  const DEPOSIT: PotOperationDto = {
    operationId: 'op1',
    kind: 'Deposit',
    potsAccountId: RESERVA_ID,
    date: '2026-10-05',
    description: 'Ramsay bonus',
    amount: 90,
    into: 'ActivePots',
    potId: null,
    bankId: null,
    categoryId: null,
    incomeId: null,
    fundingLines: [{ bankId: BARCLAYS_ID, amount: 90 }],
  }
  const WITHDRAWAL: PotOperationDto = {
    operationId: 'op2',
    kind: 'Withdrawal',
    potsAccountId: RESERVA_ID,
    date: '2026-10-09',
    description: 'Boots',
    amount: 20,
    into: null,
    incomeId: null,
    potId: 'pot-trips',
    bankId: BARCLAYS_ID,
    categoryId: 'category-home',
    fundingLines: [],
  }
  const linkedRow = (operation = OPERATION_LINK, description = 'Ramsay bonus') =>
    movementRow({ id: `m-${operation.operationId}`, description }, { canEdit: false, operation })

  function bankWithWithdrawFromPots() {
    const reserva = potsAccount({ id: RESERVA_ID, name: 'Reserva' })
    getAccountsMock.mockResolvedValue([...ACCOUNTS, reserva])
    withActions(['WithdrawFromPots'])
  }

  const operationForm = (name: string) => screen.findByRole('form', { name })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-03] saves a deposit from the Pots account, says so, refreshes the pots and returns focus', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })
    const deposit = screen.getByRole('button', { name: 'Deposit' })
    await waitFor(() => expect(deposit).toBeEnabled())
    expect(deposit).toHaveAttribute('aria-expanded', 'false')

    await user.click(deposit)
    const form = await operationForm('New deposit')
    expect(deposit).toHaveAttribute('aria-expanded', 'true')
    await within(form).findByRole('table', { name: 'Split preview' })
    expect(within(form).getByRole('radio', { name: 'Manual' })).toHaveFocus()
    fireEvent.change(within(form).getAllByLabelText(/^Amount \(GBP\)/)[0], { target: { value: '90' } })
    fireEvent.change(within(form).getByLabelText(/^Description/), { target: { value: 'Ramsay bonus' } })
    await user.click(within(form).getByRole('checkbox', { name: /No bank/ }))
    const potsReadsBeforeSave = getPotsMock.mock.calls.length
    await user.click(within(form).getByRole('button', { name: 'Add deposit' }))

    await waitFor(() =>
      expect(createPotDepositMock).toHaveBeenCalledWith(RESERVA_ID, {
        amount: 90,
        date: '2026-10-15',
        description: 'Ramsay bonus',
        potId: null,
        fundingLines: [],
      }),
    )
    expect(await screen.findByText('Deposit saved')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('form', { name: 'New deposit' })).not.toBeInTheDocument())
    await waitFor(() => expect(getPotsMock.mock.calls.length).toBeGreaterThan(potsReadsBeforeSave))
    await waitFor(() => expect(deposit).toHaveFocus())
  })

  it('asks before discarding a changed deposit', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Deposit' })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: 'Deposit' }))
    await user.type(within(await operationForm('New deposit')).getByLabelText(/^Description/), 'x')

    await user.click(screen.getByRole('button', { name: 'Withdraw' }))
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(await operationForm('New withdrawal')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'New deposit' })).not.toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-07] Deposit from an income opens the shared income form with the split ticked, this Pots account and an unlocked bank, and closes after saving', async () => {
    const user = userEvent.setup()
    getIncomeSourcesMock.mockResolvedValue([...INCOME_SOURCES, { id: 'source-bonus', name: 'Bonus', isActive: true, group: 'Other', potsAccountId: RESERVA_ID, hasReferences: true }])
    createIncomeMock.mockResolvedValue({ id: 'i1', date: '2026-10-15', incomeSourceId: 'source-bonus', incomeSourceName: 'Bonus', grossValue: null, netValue: 100, bankId: BARCLAYS_ID, bankName: 'Barclays', description: null, potsAccountId: RESERVA_ID, hasLinkedDeposit: true, fundingLines: [] })
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Deposit' })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: 'Deposit' }))
    const depositForm = await operationForm('New deposit')
    await user.click(within(depositForm).getByRole('radio', { name: 'From an income' }))
    await user.click(within(depositForm).getByRole('button', { name: 'Continue to the income form' }))

    expect(await screen.findByRole('heading', { name: 'New Income' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Deposit' })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByLabelText(/^Source/)).toHaveValue('source-bonus')
    expect(screen.getByRole('checkbox', { name: 'Split to pots' })).toBeChecked()
    await waitFor(() => expect(screen.getByLabelText(/^Pots account/)).toHaveValue(RESERVA_ID))
    await waitFor(() => expect(screen.getByLabelText(/^Date/)).toHaveFocus())
    fireEvent.change(screen.getByLabelText(/^Bank/), { target: { value: BARCLAYS_ID } })
    fireEvent.change(screen.getByLabelText(/^Net Value/), { target: { value: '100' } })
    await user.click(screen.getByRole('button', { name: 'Add Income' }))

    await waitFor(() => expect(createIncomeMock).toHaveBeenCalled())
    expect(createIncomeMock.mock.calls[0][0]).toMatchObject({ incomeSourceId: 'source-bonus', bankId: BARCLAYS_ID, potsAccountId: RESERVA_ID })
    expect(await screen.findByText('Deposit saved')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'New Income' })).not.toBeInTheDocument())
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-07] Withdraw from pots on a bank sends what Withdraw on the Pots account sends', async () => {
    const user = userEvent.setup()
    bankWithWithdrawFromPots()
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    const action = await screen.findByRole('button', { name: 'Withdraw from pots' })
    await waitFor(() => expect(action).toBeEnabled())

    await user.click(action)
    const form = await operationForm('Withdraw from pots')
    expect(action).toHaveAttribute('aria-expanded', 'true')
    await waitFor(() => expect(within(form).getByLabelText(/^Pot\b/)).toHaveValue('pot-home'))
    expect(getPotsMock).toHaveBeenCalledWith(RESERVA_ID, 2026, 10)
    fireEvent.change(within(form).getByLabelText(/^Description/), { target: { value: 'Boots' } })
    fireEvent.change(within(form).getByLabelText(/^Amount/), { target: { value: '20' } })
    await waitFor(() => expect(within(form).getByLabelText(/^Category/)).toHaveValue('category-home'))
    await user.click(within(form).getByRole('button', { name: 'Withdraw from pots' }))

    await waitFor(() =>
      expect(createPotWithdrawalMock).toHaveBeenCalledWith(RESERVA_ID, {
        potId: 'pot-home',
        amount: 20,
        date: '2026-10-15',
        description: 'Boots',
        bankId: BARCLAYS_ID,
        categoryId: 'category-home',
        confirmed: false,
      }),
    )
    expect(await screen.findByText('Withdrawal saved')).toBeInTheDocument()
    await waitFor(() => expect(action).toHaveFocus())
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-07] Withdraw on the Pots account through the same bank sends the same request', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Withdraw' })).toBeEnabled())

    await user.click(screen.getByRole('button', { name: 'Withdraw' }))
    const form = await operationForm('New withdrawal')
    await waitFor(() => expect(within(form).getByLabelText(/^Pot\b/)).toHaveValue('pot-home'))
    fireEvent.change(within(form).getByLabelText(/^Description/), { target: { value: 'Boots' } })
    fireEvent.change(within(form).getByLabelText(/^Amount/), { target: { value: '20' } })
    fireEvent.change(within(form).getByRole('combobox', { name: /Through bank/ }), { target: { value: BARCLAYS_ID } })
    await user.click(within(form).getByRole('button', { name: 'Add withdrawal' }))

    await waitFor(() =>
      expect(createPotWithdrawalMock).toHaveBeenCalledWith(RESERVA_ID, {
        potId: 'pot-home',
        amount: 20,
        date: '2026-10-15',
        description: 'Boots',
        bankId: BARCLAYS_ID,
        categoryId: 'category-home',
        confirmed: false,
      }),
    )
  })

  it('makes a linked Pots row a link that opens its operation, and takes the link out of the URL on Cancel', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockResolvedValue(DEPOSIT)
    withPotsAccount([linkedRow()])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const link = await screen.findByRole('link', { name: 'Ramsay bonus' })
    expect(link).toHaveAttribute('href', `/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions&operation=op1`)
    await user.click(link)

    const form = await operationForm('Edit deposit')
    expect(getPotOperationMock).toHaveBeenCalledWith('op1')
    await waitFor(() => expect(within(form).getByRole('group', { name: 'Funding line 1' })).toBeInTheDocument())
    await user.click(within(form).getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.queryByRole('form', { name: 'Edit deposit' })).not.toBeInTheDocument())
    await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)).toBeInTheDocument())
    expect(screen.getByRole('link', { name: 'Ramsay bonus' })).toHaveFocus()
  })

  it('opens a withdrawal from the address alone, saves it and clears the address', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockResolvedValue(WITHDRAWAL)
    updatePotWithdrawalMock.mockResolvedValue(WITHDRAWAL)
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions&operation=op2`)

    const form = await operationForm('Edit withdrawal')
    await waitFor(() => expect(within(form).getByLabelText(/^Pot\b/)).toHaveValue('pot-trips'))
    await user.click(within(form).getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updatePotWithdrawalMock).toHaveBeenCalledWith(RESERVA_ID, 'op2', expect.objectContaining({ potId: 'pot-trips', amount: 20 })))
    expect(await screen.findByText('Withdrawal saved')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)).toBeInTheDocument())
  })

  it('keeps the form open when its own row link is followed again', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockResolvedValue(DEPOSIT)
    withPotsAccount([linkedRow()])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)
    await user.click(await screen.findByRole('link', { name: 'Ramsay bonus' }))
    await operationForm('Edit deposit')

    await user.click(screen.getByRole('link', { name: 'Ramsay bonus' }))

    expect(screen.getByRole('form', { name: 'Edit deposit' })).toBeInTheDocument()
    expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions&operation=op1`)).toBeInTheDocument()
    expect(getPotOperationMock).toHaveBeenCalledOnce()
  })

  it('asks before an opening operation replaces a changed form', async () => {
    const user = userEvent.setup()
    let finishLoad: (operation: PotOperationDto) => void = () => undefined
    getPotOperationMock.mockReturnValue(new Promise((resolve) => { finishLoad = resolve }))
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&operation=op1`)
    const deposit = await screen.findByRole('button', { name: 'Deposit' })
    await waitFor(() => expect(deposit).toBeEnabled())
    await waitFor(() => expect(getPotOperationMock).toHaveBeenCalled())
    await user.click(deposit)
    await user.type(within(await operationForm('New deposit')).getByLabelText(/^Description/), 'typed')

    finishLoad(DEPOSIT)

    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))
    const form = await operationForm('Edit deposit')
    await user.click(within(form).getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.queryByRole('form', { name: 'Edit deposit' })).not.toBeInTheDocument())
    await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)).toBeInTheDocument())
  })

  it('returns focus to the selected tab when a form opened from the address closes', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockResolvedValue(DEPOSIT)
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&operation=op1`)

    const form = await operationForm('Edit deposit')
    await user.click(within(form).getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.getByRole('tab', { name: 'Pots', selected: true })).toHaveFocus())
  })

  it('opens the income form for a deposit that belongs to an income', async () => {
    getPotOperationMock.mockResolvedValue({ ...DEPOSIT, incomeId: 'i1' })
    getIncomesByMonthMock.mockResolvedValue([
      { id: 'i1', date: '2026-10-05', incomeSourceId: 'source-salary', incomeSourceName: 'Gleison', grossValue: null, netValue: 100, bankId: BARCLAYS_ID, bankName: 'Barclays', description: 'Ramsay bonus', potsAccountId: RESERVA_ID, hasLinkedDeposit: true, fundingLines: [] },
    ])
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions&operation=op1`)

    expect(await screen.findByRole('heading', { name: 'Edit Income' })).toBeInTheDocument()
    expect(getIncomesByMonthMock).toHaveBeenCalledWith(2026, 10)
    expect(screen.getByLabelText(/^Net Value/)).toHaveValue(100)
    expect(screen.queryByRole('button', { name: /^Delete (deposit|withdrawal)$/ })).not.toBeInTheDocument()
  })

  describe('linked rows on a bank screen', () => {
    const BANK_ROW_LINK = { operationId: 'op1', potsAccountId: RESERVA_ID, year: 2026, month: 10, incomeId: null as string | null }
    const INCOME: IncomeDto = {
      id: 'i1', date: '2026-10-05', incomeSourceId: 'source-bonus', incomeSourceName: 'Bonus', grossValue: null, netValue: 100,
      bankId: BARCLAYS_ID, bankName: 'Barclays', description: 'Ramsay bonus', potsAccountId: RESERVA_ID, hasLinkedDeposit: true,
      fundingLines: [{ bankId: BARCLAYS_ID, amount: 90 }],
    }
    const bankRow = (incomeId: string | null) =>
      accountTransactionRow({ id: 'e-linked', kind: 'Expense', date: '2026-10-05', description: 'Deposit to Reserva', amount: -90, canEdit: false, operation: { ...BANK_ROW_LINK, incomeId } })

    it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-07] goes to the operation on the Pots screen, as the row is read-only here', async () => {
      const user = userEvent.setup()
      withPotsAccount([bankRow(null)])
      getPotOperationMock.mockResolvedValue(DEPOSIT)
      renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

      const link = await screen.findByRole('link', { name: 'Deposit to Reserva' })
      expect(within(link.closest('tr') as HTMLElement).queryByRole('button')).not.toBeInTheDocument()
      await user.click(link)

      await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions&operation=op1`)).toBeInTheDocument())
    })

    it("opens the income's edit form on the bank screen for an income's deposit, and takes the link out of the address on Cancel", async () => {
      const user = userEvent.setup()
      withPotsAccount([bankRow('i1')])
      getIncomeSourcesMock.mockResolvedValue([...INCOME_SOURCES, { id: 'source-bonus', name: 'Bonus', isActive: true, group: 'Other', potsAccountId: RESERVA_ID, hasReferences: true }])
      getPotOperationMock.mockResolvedValue({ ...DEPOSIT, incomeId: 'i1' })
      getIncomesByMonthMock.mockResolvedValue([INCOME])
      renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

      await user.click(await screen.findByRole('link', { name: 'Deposit to Reserva' }))

      expect(await screen.findByRole('heading', { name: 'Edit Income' })).toBeInTheDocument()
      expect(screen.getByText(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10&operation=op1`)).toBeInTheDocument()
      expect(screen.getByRole('group', { name: 'Funded from' })).toHaveTextContent('Barclays')

      await user.click(screen.getByRole('button', { name: 'Cancel' }))

      await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)).toBeInTheDocument())
      expect(screen.getByRole('link', { name: 'Deposit to Reserva' })).toHaveFocus()
    })

    it('says the operation is not here when a bank address names one with no income, and Dismiss moves focus to the heading', async () => {
      const user = userEvent.setup()
      withPotsAccount([])
      getPotOperationMock.mockResolvedValue(DEPOSIT)
      renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10&operation=op1`)

      expect(await screen.findByRole('alert')).toHaveTextContent('This deposit or withdrawal no longer exists.')
      await user.click(screen.getByRole('button', { name: 'Dismiss' }))

      await waitFor(() => expect(screen.getByRole('heading', { name: 'Transactions' })).toHaveFocus())
    })
  })

  it('opens the form on an inactive Pots account, which has no actions to load its options', async () => {
    getPotOperationMock.mockResolvedValue(DEPOSIT)
    const reserva = potsAccount({ id: RESERVA_ID, name: 'Reserva', isActive: false })
    getAccountsMock.mockResolvedValue([...ACCOUNTS, reserva])
    getAccountMock.mockImplementation((id, year, month) =>
      Promise.resolve({ ...reserva, id, year, month, balance: 0, balanceIsOwed: false, hasStatements: false, totalOwed: null, viewedStatement: null, allowedActions: [], disabledActions: {}, currentBalances: [] }),
    )
    getAccountTransactionsMock.mockResolvedValue([])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&operation=op1`)

    expect(await operationForm('Edit deposit')).toBeInTheDocument()
    expect(getIncomeSourcesMock).toHaveBeenCalled()
  })

  it.each([
    ['gone', () => getPotOperationMock.mockRejectedValue(new ApiError('Not found', 404))],
    ['in another Pots account', () => getPotOperationMock.mockResolvedValue({ ...DEPOSIT, potsAccountId: 'another' })],
    ['of an income no longer in its month', () => { getPotOperationMock.mockResolvedValue({ ...DEPOSIT, incomeId: 'gone' }); getIncomesByMonthMock.mockResolvedValue([]) }],
  ])('says so when the operation is %s, and Dismiss clears the address', async (_, arrange) => {
    const user = userEvent.setup()
    arrange()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&operation=op1`)

    expect(await screen.findByRole('alert')).toHaveTextContent('This deposit or withdrawal no longer exists.')
    await user.click(screen.getByRole('button', { name: 'Dismiss' }))

    await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)).toBeInTheDocument())
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Pots', selected: true })).toHaveFocus()
  })

  it('offers a retry when the operation cannot be read', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockRejectedValueOnce(new Error('offline')).mockResolvedValue(DEPOSIT)
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&operation=op1`)

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load this deposit or withdrawal')
    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await operationForm('Edit deposit')).toBeInTheDocument()
  })

  it('deletes the whole operation after confirmation, says so and closes the form', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockResolvedValue(WITHDRAWAL)
    deletePotOperationMock.mockResolvedValue(undefined)
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions&operation=op2`)
    const form = await operationForm('Edit withdrawal')

    await user.click(within(form).getByRole('button', { name: 'Delete withdrawal' }))
    expect(await findDialog('Delete withdrawal')).toHaveTextContent('Its pot lines and any bank expenses are deleted with it.')
    await user.click(await dialogButton('Delete withdrawal', 'Delete'))

    await waitFor(() => expect(deletePotOperationMock).toHaveBeenCalledWith('op2'))
    expect(await screen.findByText('Withdrawal deleted')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('form', { name: 'Edit withdrawal' })).not.toBeInTheDocument())
    await waitFor(() => expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)).toBeInTheDocument())
  })

  it('keeps the form and shows the error when the delete fails, and Cancel leaves it open', async () => {
    const user = userEvent.setup()
    getPotOperationMock.mockResolvedValue(DEPOSIT)
    deletePotOperationMock.mockRejectedValue(new ApiError('This deposit belongs to an income; edit the income.', 409))
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&operation=op1`)
    const form = await operationForm('Edit deposit')

    await user.click(within(form).getByRole('button', { name: 'Delete deposit' }))
    await user.click(await dialogButton('Delete deposit', 'Delete'))

    expect(await findDialog('Delete deposit')).toHaveTextContent('This deposit belongs to an income; edit the income.')
    await user.click(await dialogButton('Delete deposit', 'Cancel'))
    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    expect(screen.getByRole('form', { name: 'Edit deposit' })).toBeInTheDocument()
  })
})

describe('AccountScreenPage on a Pots account', () => {
  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-15] opens on the Pots tab with each pot, its split and balance at the end of the month, and the total', async () => {
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)

    const table = await screen.findByRole('table', { name: 'Pots' })
    expect(screen.getByRole('tab', { name: 'Pots', selected: true })).toBeInTheDocument()
    expect(screen.getByRole('tabpanel', { name: 'Pots' })).toBeInTheDocument()
    expect(getPotsMock).toHaveBeenCalledWith(RESERVA_ID, 2026, 10)
    expect(screen.getByText('Balances at end of October 2026')).toBeInTheDocument()
    const homeRow = within(table).getByText('Home').closest('tr') as HTMLElement
    expect(within(homeRow).getByText('60.00%').closest('td')).toHaveClass('data-table__col--numeric')
    expect(within(homeRow).getByText('1,000.25').closest('td')).toHaveClass('data-table__col--numeric')
    expect(within(within(table).getByText('Old car').closest('tr') as HTMLElement).getByText('Inactive')).toBeInTheDocument()
    expect(screen.getByText('1,500.50', { selector: 'strong' }).closest('p')).toHaveTextContent('Pots total GBP: 1,500.50')
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-15] shows the loading state, then the error with a retry that reloads the pots', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    let failLoad: (reason: Error) => void = () => {}
    getPotsMock.mockReturnValueOnce(new Promise((_, reject) => { failLoad = reject }))
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)

    expect(await screen.findByLabelText('Loading pots')).toBeInTheDocument()
    await waitFor(() => expect(getPotsMock).toHaveBeenCalled())
    failLoad(new Error('offline'))

    expect(await screen.findByText('Unable to load the pots')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table', { name: 'Pots' })).toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-15] says when the account has no pots yet, with no total', async () => {
    withPotsAccount()
    getPotsMock.mockResolvedValue({ ...POTS_SUMMARY, total: 0, pots: [] })
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)

    expect(await screen.findByText('This account has no pots yet')).toBeInTheDocument()
    expect(screen.queryByText(/Pots total/)).not.toBeInTheDocument()
  })

  it('reads the pots again for another month', async () => {
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    await waitFor(() => expect(getPotsMock).toHaveBeenLastCalledWith(RESERVA_ID, 2026, 8))
    expect(await screen.findByText('Balances at end of August 2026')).toBeInTheDocument()
  })

  it('offers Deposit and Withdraw as forms and Manage pots as a dialog', async () => {
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })

    const actions = within(screen.getByRole('group', { name: 'Account actions' })).getAllByRole('button')
    expect(actions.map((button) => button.textContent)).toEqual(['Deposit', 'Withdraw', 'Manage pots'])
    expect(actions[0]).toHaveAttribute('aria-expanded', 'false')
    expect(actions[1]).toHaveAttribute('aria-expanded', 'false')
    expect(actions[2]).toHaveAttribute('aria-haspopup', 'dialog')
    expect(actions[2]).not.toHaveAttribute('aria-expanded')
    await waitFor(() => expect(getIncomeSourcesMock).toHaveBeenCalled())
  })

  it('keeps Manage pots disabled while the pots cannot be read', async () => {
    withPotsAccount()
    getPotsMock.mockRejectedValue(new Error('offline'))
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)

    expect(await screen.findByText('Unable to load the pots')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Manage pots' })).toBeDisabled()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-14] saves Manage pots, says so, refreshes the Pots tab and returns focus to the action', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })
    const manage = screen.getByRole('button', { name: 'Manage pots' })

    await user.click(manage)
    const dialog = await findDialog('Manage pots')
    expect(dialog).toHaveTextContent('Reserva')
    const potsReadsBeforeSave = getPotsMock.mock.calls.length
    const name = within(within(dialog).getByRole('group', { name: 'Pot Home', hidden: true })).getByLabelText(/^Name/)
    await user.clear(name)
    await user.type(name, 'House')
    await user.click(await dialogButton('Manage pots', 'Save'))

    await waitFor(() =>
      expect(updatePotsMock).toHaveBeenCalledWith(RESERVA_ID, {
        pots: [
          { id: 'pot-home', name: 'House', splitPercentage: 60, isActive: true },
          { id: 'pot-trips', name: 'Trips', splitPercentage: 40, isActive: true },
          { id: 'pot-old', name: 'Old car', splitPercentage: 0, isActive: false },
        ],
      }),
    )
    expect(await screen.findByText('Pots saved')).toBeInTheDocument()
    await waitFor(() => expect(getPotsMock.mock.calls.length).toBeGreaterThan(potsReadsBeforeSave))
    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    expect(manage).toHaveFocus()
  })

  it('returns focus to Manage pots when it was opened through the discard prompt', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)
    const edit = within((await screen.findByText('Boots')).closest('tr') as HTMLElement).getByRole('button', { name: 'Edit pot transaction' })
    await waitFor(() => expect(edit).toBeEnabled())
    await user.click(edit)
    await user.type(within(screen.getByRole('form', { name: 'Edit pot transaction' })).getByLabelText(/^Description/), ' x')
    const manage = screen.getByRole('button', { name: 'Manage pots' })

    await user.click(manage)
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))
    await user.click(await dialogButton('Manage pots', 'Cancel'))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    await waitFor(() => expect(manage).toHaveFocus())
  })

  it('closes an open Manage pots when the route moves to another account', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`, [`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`])
    await screen.findByRole('table', { name: 'Pots' })
    await user.click(screen.getByRole('button', { name: 'Manage pots' }))
    await findDialog('Manage pots')

    fireEvent.click(screen.getByText('Browser back'))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
  })

  it('switches to Transactions in the URL and lists the movements as date, pot, description and amount', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })

    await user.click(screen.getByRole('tab', { name: 'Transactions' }))

    expect(screen.getByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)).toBeInTheDocument()
    const row = screen.getByText('Boots').closest('tr') as HTMLElement
    expect(within(row).getByText('Trips')).toBeInTheDocument()
    expect(within(row).getByText('-20.00')).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: /^Pot/ })).toBeInTheDocument()
    expect(screen.queryByRole('columnheader', { name: /^Round-up/ })).not.toBeInTheDocument()
  })

  it('offers no actions on a linked movement', async () => {
    const linked = movementRow(
      { id: 'm-linked', description: 'Ramsay bonus' },
      { canEdit: false, operation: { operationId: 'op1', potsAccountId: RESERVA_ID, year: 2026, month: 10, incomeId: null } },
    )
    withPotsAccount([linked])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const row = (await screen.findByText('Ramsay bonus')).closest('tr') as HTMLElement
    expect(within(row).queryByRole('button')).not.toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-08] edits a legacy movement with no form actions on the account, then refreshes the rows and the pots', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    updateReserveMovementMock.mockResolvedValue(LEGACY_WITHDRAWAL.movement as ReserveMovementDto)
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const row = (await screen.findByText('Boots')).closest('tr') as HTMLElement
    const edit = within(row).getByRole('button', { name: 'Edit pot transaction' })
    await waitFor(() => expect(edit).toBeEnabled())
    await user.click(edit)

    const form = screen.getByRole('form', { name: 'Edit pot transaction' })
    expect(within(form).getByLabelText(/^Pot/)).toHaveValue('pot-trips')
    expect(within(form).getByRole('option', { name: 'Old car (inactive)' })).toBeInTheDocument()
    const potsReadsBeforeSave = getPotsMock.mock.calls.length
    await user.clear(within(form).getByLabelText(/^Description/))
    await user.type(within(form).getByLabelText(/^Description/), 'Boots chemist')
    await user.selectOptions(within(form).getByLabelText(/^Pot/), 'pot-home')
    await user.click(within(form).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateReserveMovementMock).toHaveBeenCalledWith('m-boots', { bucketId: 'pot-home', amount: -20, date: '2026-10-09', description: 'Boots chemist' }),
    )
    expect(await screen.findByText('Pot transaction updated')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Edit pot transaction' })).not.toBeInTheDocument()
    await waitFor(() => expect(getPotsMock.mock.calls.length).toBeGreaterThan(potsReadsBeforeSave))
  })

  it('keeps the values and shows the server message when a movement edit fails', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    updateReserveMovementMock.mockRejectedValue(new ApiError('This pot transaction belongs to a pot deposit or withdrawal; edit it from the Pots account.', 409))
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const edit = within((await screen.findByText('Boots')).closest('tr') as HTMLElement).getByRole('button', { name: 'Edit pot transaction' })
    await waitFor(() => expect(edit).toBeEnabled())
    await user.click(edit)
    const form = screen.getByRole('form', { name: 'Edit pot transaction' })
    await user.clear(within(form).getByLabelText(/^Description/))
    await user.type(within(form).getByLabelText(/^Description/), 'Boots chemist')
    await user.click(within(form).getByRole('button', { name: 'Save' }))

    expect(await within(form).findByText(/belongs to a pot deposit or withdrawal/)).toBeInTheDocument()
    expect(within(form).getByLabelText(/^Description/)).toHaveValue('Boots chemist')
  })

  it('asks before discarding an unsaved movement edit on a tab change', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const edit = within((await screen.findByText('Boots')).closest('tr') as HTMLElement).getByRole('button', { name: 'Edit pot transaction' })
    await waitFor(() => expect(edit).toBeEnabled())
    await user.click(edit)
    await user.type(within(screen.getByRole('form', { name: 'Edit pot transaction' })).getByLabelText(/^Description/), ' x')
    await user.click(screen.getByRole('tab', { name: 'Pots' }))

    expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
    expect(screen.getByRole('form', { name: 'Edit pot transaction' })).toBeInTheDocument()
  })

  it('keeps Edit disabled and offers a retry on Transactions when the pots cannot be read', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    getPotsMock.mockRejectedValueOnce(new Error('offline'))
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    expect(await screen.findByText('Unable to load the pots')).toBeInTheDocument()
    const edit = within(screen.getByText('Boots').closest('tr') as HTMLElement).getByRole('button', { name: 'Edit pot transaction' })
    expect(edit).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(edit).toBeEnabled())
    expect(screen.queryByText('Unable to load the pots')).not.toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-08] deletes a legacy movement after a confirmation that counts the lines of its split', async () => {
    const user = userEvent.setup()
    withPotsAccount([
      movementRow({ id: 'm-1', bucketName: 'Home', splitLineCount: 2 }),
      movementRow({ id: 'm-2', bucketId: 'pot-trips', bucketName: 'Trips', amount: 15, splitLineCount: 2 }),
    ])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const row = (await screen.findByText('Home')).closest('tr') as HTMLElement
    await user.click(within(row).getByRole('button', { name: 'Delete pot transaction' }))

    const dialog = await findDialog('Delete pot transaction')
    expect(dialog).toHaveTextContent('Delete this pot transaction? It is part of a split, so all 2 lines will be deleted. This cannot be undone.')
    await user.click(await dialogButton('Delete pot transaction', 'Delete'))

    await waitFor(() => expect(deleteReserveMovementMock).toHaveBeenCalledWith('m-1'))
    expect(await screen.findByText('2 pot transaction lines deleted')).toBeInTheDocument()
  })

  it('closes the edit form of another line of the split when the split is deleted', async () => {
    const user = userEvent.setup()
    withPotsAccount([
      movementRow({ id: 'm-1', bucketName: 'Home', splitLineCount: 2 }),
      movementRow({ id: 'm-2', bucketId: 'pot-trips', bucketName: 'Trips', amount: 15, splitLineCount: 2 }),
    ])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const tripsRow = (await screen.findByText('Trips')).closest('tr') as HTMLElement
    const edit = within(tripsRow).getByRole('button', { name: 'Edit pot transaction' })
    await waitFor(() => expect(edit).toBeEnabled())
    await user.click(edit)
    await screen.findByRole('form', { name: 'Edit pot transaction' })
    getAccountTransactionsMock.mockResolvedValue([])
    await user.click(within(screen.getByRole('cell', { name: /Home/ }).closest('tr') as HTMLElement).getByRole('button', { name: 'Delete pot transaction' }))
    await user.click(await dialogButton('Delete pot transaction', 'Delete'))

    expect(await screen.findByText('2 pot transaction lines deleted')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('form', { name: 'Edit pot transaction' })).not.toBeInTheDocument())
    await waitFor(() => expect(screen.getByRole('tab', { name: 'Transactions', selected: true })).toHaveFocus())
  })

  it('keeps the edit form of a line the delete did not remove', async () => {
    const user = userEvent.setup()
    const trips = movementRow({ id: 'm-2', bucketId: 'pot-trips', bucketName: 'Trips', amount: -15, description: 'Boots' })
    withPotsAccount([movementRow({ id: 'm-1', bucketName: 'Home', amount: -5, description: 'Taxi' }), trips])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    const tripsRow = (await screen.findByText('Trips')).closest('tr') as HTMLElement
    const edit = within(tripsRow).getByRole('button', { name: 'Edit pot transaction' })
    await waitFor(() => expect(edit).toBeEnabled())
    await user.click(edit)
    await screen.findByRole('form', { name: 'Edit pot transaction' })
    getAccountTransactionsMock.mockResolvedValue([trips])
    await user.click(within(screen.getByRole('cell', { name: /Home/ }).closest('tr') as HTMLElement).getByRole('button', { name: 'Delete pot transaction' }))
    await user.click(await dialogButton('Delete pot transaction', 'Delete'))

    expect(await screen.findByText('Pot transaction deleted')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('cell', { name: /Home/ })).not.toBeInTheDocument())
    expect(screen.getByRole('form', { name: 'Edit pot transaction' })).toBeInTheDocument()
  })

  it('tells where to edit a line of an older split income', async () => {
    withPotsAccount([movementRow({ id: 'm-1', incomeId: 'i1' }, { canEdit: false })])
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=transactions`)

    expect(await screen.findByText('Edit from the income')).toBeInTheDocument()
  })

  it('shows a failed account reload on the Pots tab instead of figures the header disagrees with', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Pots' })

    getAccountMock.mockRejectedValueOnce(new Error('offline'))
    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-08' } })

    expect(await screen.findByText('Unable to load this account')).toBeInTheDocument()
    expect(screen.queryByRole('table', { name: 'Pots' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByRole('table', { name: 'Pots' })).toBeInTheDocument()
  })

  it('paints a neutral skeleton, with no Transactions heading, until the account is known', async () => {
    withPotsAccount()
    let finishAccount: (account: AccountDetailDto) => void = () => {}
    getAccountMock.mockReturnValueOnce(new Promise((resolve) => { finishAccount = resolve }))
    renderScreen(`/cashflow/accounts/${RESERVA_ID}?month=2026-10&tab=pots`)

    expect(screen.getByLabelText('Loading account details')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Transactions' })).not.toBeInTheDocument()

    finishAccount({ ...potsAccount({ id: RESERVA_ID, name: 'Reserva' }), year: 2026, month: 10, balance: 1500.5, balanceIsOwed: false, hasStatements: false, totalOwed: null, viewedStatement: null, allowedActions: [], disabledActions: {}, currentBalances: [] })

    expect(await screen.findByRole('table', { name: 'Pots' })).toBeInTheDocument()
  })

  it('[AC P60-F05-bank-account-screen-02] keeps the month and opens the Pots tab of the account picked', async () => {
    const user = userEvent.setup()
    withPotsAccount()
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-09`)

    await user.click(await screen.findByRole('combobox', { name: 'Account' }))
    await user.click(screen.getByRole('option', { name: 'Reserva' }))

    expect(await screen.findByText(`/cashflow/accounts/${RESERVA_ID}?month=2026-09`)).toBeInTheDocument()
    expect(await screen.findByRole('table', { name: 'Pots' })).toBeInTheDocument()
  })

  it('reads no pots on a bank', async () => {
    renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10&tab=pots`)

    expect(await screen.findByText('Tesco')).toBeInTheDocument()
    expect(screen.queryByRole('tablist')).not.toBeInTheDocument()
    expect(getPotsMock).not.toHaveBeenCalled()
  })
})

describe('AccountScreenPage on a Settlement account', () => {
  const MAE_ID = '8f3b1c1a-2e3a-4b1a-9a7f-1000000000ae'
  const ENTRY_ROW = accountTransactionRow({
    id: 'entry-1',
    kind: 'SettlementEntry',
    date: '2026-10-10',
    description: 'Farmácia',
    amount: -150,
    currency: 'BRL',
    settlement: { id: 'entry-1', accountId: MAE_ID, date: '2026-10-10', description: 'Farmácia', note: '', paidBy: 'Other', currency: 'BRL', amount: 150, convertedCurrency: 'GBP', convertedValue: null, warning: null },
  })

  beforeEach(() => {
    const mae = settlementAccount({ id: MAE_ID, name: 'Mãe' })
    getAccountsMock.mockResolvedValue([...ACCOUNTS, mae])
    getAccountMock.mockImplementation((id, year, month) =>
      Promise.resolve(
        id === MAE_ID
          ? {
              ...mae,
              year,
              month,
              balance: 1,
              balanceIsOwed: false,
              hasStatements: false,
              totalOwed: null,
              viewedStatement: null,
              allowedActions: ['AddEntry', 'Settle'],
              disabledActions: {},
              currentBalances: [{ currency: 'BRL', balance: 167.8768 }, { currency: 'GBP', balance: 7.94214099 }],
            }
          : detailFor(id, year, month),
      ),
    )
    getAccountTransactionsMock.mockResolvedValue([ENTRY_ROW])
    createSettlementEntryMock.mockReset().mockResolvedValue(ENTRY_ROW.settlement!)
    updateSettlementEntryMock.mockReset().mockResolvedValue(ENTRY_ROW.settlement!)
    settleAccountMock.mockReset().mockResolvedValue(ENTRY_ROW.settlement!)
  })

  it('[AC P60-F12-settlement-screen-01] shows the current balance in the header and the entries in the Settlement columns', async () => {
    renderScreen(`/cashflow/accounts/${MAE_ID}?month=2026-10`)

    expect(await screen.findByText('Mãe owes you R$167.88 / £7.94')).toBeInTheDocument()
    expect(screen.getByText('Settlement · BRL, GBP')).toBeInTheDocument()
    const table = await screen.findByRole('table', { name: 'Transactions' })
    await waitFor(() => expect(within(table).getByText('Farmácia')).toBeInTheDocument())
    expect(within(table).getByRole('columnheader', { name: 'Paid by' })).toBeInTheDocument()
    expect(within(table).getByText('R$150.00')).toBeInTheDocument()
    expect(within(table).getByText('—')).toBeInTheDocument()
  })

  const openMaeScreen = async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${MAE_ID}?month=2026-10`)
    await screen.findByRole('table', { name: 'Transactions' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Edit entry' })).toBeEnabled())
    return user
  }

  it('[AC P60-F12-settlement-screen-04] adds an entry, stays open with the kept fields, focus on Description and a toast', async () => {
    const user = await openMaeScreen()
    expect(getIncomeSourcesMock).not.toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Add entry' }))
    const form = screen.getByRole('form', { name: 'Add entry' })
    expect(within(form).getByLabelText(/^Date/)).toHaveFocus()
    await user.click(within(form).getByRole('radio', { name: 'You' }))
    await user.type(within(form).getByLabelText(/^Description/), 'Mercado')
    await user.type(within(form).getByLabelText(/^Amount \(BRL\)/), '80')
    await user.click(within(form).getByRole('button', { name: 'Add entry' }))

    expect(await screen.findByText('Entry added')).toBeInTheDocument()
    expect(createSettlementEntryMock).toHaveBeenCalledWith(MAE_ID, expect.objectContaining({ description: 'Mercado', paidBy: 'You', currency: 'BRL', amount: 80 }))
    await waitFor(() => expect(within(form).getByLabelText(/^Description/)).toHaveFocus())
    expect(within(form).getByLabelText(/^Description/)).toHaveValue('')
    expect(within(form).getByRole('radio', { name: 'You' })).toBeChecked()
  })

  it('[AC P60-F12-settlement-screen-05] settles with the pre-filled values after confirming both totals become zero', async () => {
    const user = await openMaeScreen()

    await user.click(screen.getByRole('button', { name: 'Settle' }))
    const form = screen.getByRole('form', { name: 'Settle' })
    expect(within(form).getByLabelText(/^Amount \(BRL\)/)).toHaveValue(167.88)
    expect(within(form).getByLabelText(/^GBP value/)).toHaveValue(7.94)
    expect(within(form).getByText('Implied rate: 1 GBP = R$21.14')).toBeInTheDocument()
    await user.click(within(form).getByRole('button', { name: 'Save settlement' }))

    expect(await findDialog('Save this settlement?')).toHaveTextContent('Both totals become zero.')
    await user.click(await dialogButton('Save this settlement?', 'Save settlement'))

    expect(await screen.findByText('Settlement saved')).toBeInTheDocument()
    expect(settleAccountMock).toHaveBeenCalledWith(MAE_ID, expect.objectContaining({ paidBy: 'Other', currency: 'BRL', amount: 167.88, convertedValue: 7.94, description: 'Settlement' }))
    await waitFor(() => expect(screen.queryByRole('form', { name: 'Settle' })).not.toBeInTheDocument())
  })

  it('[AC P60-F12-settlement-screen-06] states the leftover before saving an edited settlement', async () => {
    const user = await openMaeScreen()
    await user.click(screen.getByRole('button', { name: 'Settle' }))
    const amount = screen.getByLabelText(/^Amount \(BRL\)/)
    await user.clear(amount)
    await user.type(amount, '100')

    await user.click(screen.getByRole('button', { name: 'Save settlement' }))

    expect(await findDialog('Save this settlement?')).toHaveTextContent('This leaves R$67.88 / £0.00 outstanding (Mãe owes you).')
  })

  it('[AC P60-F12-settlement-screen-08] puts focus on the chosen currency when Settle opens on a later one', async () => {
    const detail = await getAccountMock(MAE_ID, 2026, 10)
    getAccountMock.mockResolvedValue({ ...detail, currentBalances: [{ currency: 'BRL', balance: 0 }, { currency: 'GBP', balance: 5 }] })
    const user = await openMaeScreen()

    await user.click(screen.getByRole('button', { name: 'Settle' }))

    expect(within(screen.getByRole('form', { name: 'Settle' })).getByRole('radio', { name: 'GBP' })).toHaveFocus()
  })

  it('[AC P60-F12-settlement-screen-07] shows Settle disabled with its reason when the API disables it', async () => {
    const detail = await getAccountMock(MAE_ID, 2026, 10)
    getAccountMock.mockResolvedValue({ ...detail, disabledActions: { Settle: 'Nothing to settle.' }, currentBalances: [{ currency: 'BRL', balance: 0 }, { currency: 'GBP', balance: 0 }] })
    renderScreen(`/cashflow/accounts/${MAE_ID}?month=2026-10`)

    expect(await screen.findByText('Nothing to settle.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Settle' })).toHaveAttribute('aria-disabled', 'true')
    expect(screen.getByRole('button', { name: 'Add entry' })).toBeEnabled()
  })

  it('[AC P60-F12-settlement-screen-08] hides the Converted column on a one-currency account', async () => {
    const detail = await getAccountMock(MAE_ID, 2026, 10)
    getAccountMock.mockResolvedValue({ ...detail, currentBalances: [{ currency: 'GBP', balance: 15 }] })
    renderScreen(`/cashflow/accounts/${MAE_ID}?month=2026-10`)

    const table = await screen.findByRole('table', { name: 'Transactions' })
    await waitFor(() => expect(within(table).getByRole('columnheader', { name: 'Paid by' })).toBeInTheDocument())
    expect(within(table).queryByRole('columnheader', { name: 'Converted' })).not.toBeInTheDocument()
  })

  it('[AC P60-F12-settlement-screen-02] edits an entry row, closes and toasts', async () => {
    const user = await openMaeScreen()

    await user.click(screen.getByRole('button', { name: 'Edit entry' }))
    const form = screen.getByRole('form', { name: 'Edit entry' })
    expect(within(form).getByLabelText(/^Description/)).toHaveValue('Farmácia')
    await user.click(within(form).getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Entry updated')).toBeInTheDocument()
    expect(updateSettlementEntryMock).toHaveBeenCalledWith(MAE_ID, 'entry-1', expect.objectContaining({ description: 'Farmácia', paidBy: 'Other', amount: 150, convertedValue: null }))
    await waitFor(() => expect(screen.queryByRole('form', { name: 'Edit entry' })).not.toBeInTheDocument())
  })

  it('[AC P60-F12-settlement-screen-08] keeps the edit open with the warning the save returned', async () => {
    const warning = 'The GBP value rounds to 0.00, so it was left empty.'
    updateSettlementEntryMock.mockResolvedValue({ ...ENTRY_ROW.settlement!, warning })
    const user = await openMaeScreen()

    await user.click(screen.getByRole('button', { name: 'Edit entry' }))
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText(warning)).toBeInTheDocument()
    expect(screen.getByRole('form', { name: 'Edit entry' })).toBeInTheDocument()
  })

  it('[AC P60-F12-settlement-screen-08] asks before discarding a typed entry when a row is opened, and keeps it on Keep editing', async () => {
    const user = await openMaeScreen()
    await user.click(screen.getByRole('button', { name: 'Add entry' }))
    await user.type(screen.getByLabelText(/^Description/), 'Mercado')

    await user.click(screen.getByRole('button', { name: 'Edit entry' }))

    await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Mercado')
    expect(screen.queryByRole('form', { name: 'Edit entry' })).not.toBeInTheDocument()
  })

  it('[AC P60-F12-settlement-screen-08] shows the server refusal in the form and keeps the values', async () => {
    createSettlementEntryMock.mockRejectedValue(new ApiError('Mãe is inactive.', 400))
    const user = await openMaeScreen()
    await user.click(screen.getByRole('button', { name: 'Add entry' }))
    await user.click(screen.getByRole('radio', { name: 'Mãe' }))
    await user.type(screen.getByLabelText(/^Description/), 'Mercado')
    await user.type(screen.getByLabelText(/^Amount/), '80')

    await user.click(within(screen.getByRole('form', { name: 'Add entry' })).getByRole('button', { name: 'Add entry' }))

    expect(await screen.findByText('Mãe is inactive.')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Mercado')
  })

  it('closes the form and returns focus to Add entry on Cancel', async () => {
    const user = await openMaeScreen()
    const addEntry = screen.getByRole('button', { name: 'Add entry' })
    await user.click(addEntry)

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(screen.queryByRole('form', { name: 'Add entry' })).not.toBeInTheDocument()
    expect(addEntry).toHaveFocus()
  })

  it('shows the empty message for a month without entries, and the header still shows the current balance', async () => {
    getAccountTransactionsMock.mockResolvedValue([])
    renderScreen(`/cashflow/accounts/${MAE_ID}?month=2022-03`)

    expect(await screen.findByText('No transactions in March 2022')).toBeInTheDocument()
    expect(screen.getByText('Mãe owes you R$167.88 / £7.94')).toBeInTheDocument()
  })
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
    expect(screen.getByLabelText('Loading account details')).toBeInTheDocument()
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
    potsAccountId: null,
    hasLinkedDeposit: false,
    fundingLines: [],
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
    expect(screen.getByLabelText('Loading account details')).toBeInTheDocument()
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
    expect(getIncomeSourcesMock).not.toHaveBeenCalled()
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

  it('names the Pots account the income was split to', async () => {
    const user = userEvent.setup()
    getAccountsMock.mockResolvedValue([...ACCOUNTS, potsAccount({ id: 'reserva', name: 'Reserva' })])
    createIncomeMock.mockResolvedValue({ ...SAVED_INCOME, potsAccountId: 'reserva' })
    await openScreen()

    await user.click(screen.getByRole('button', { name: 'Add income' }))
    await user.type(screen.getByLabelText(/^Net Value/), '2500')
    await user.click(screen.getByRole('button', { name: 'Add Income' }))

    expect(await screen.findByText('Income added and split to Reserva')).toBeInTheDocument()
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
    getIncomeSourcesMock.mockRejectedValueOnce(new Error('boom'))

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

  describe('credit refund to a bank', () => {
    const REFUND_TRANSFER: AccountTransferDto = {
      id: 'cr1',
      date: '2026-10-15',
      sourceAccountId: AMEX_ID,
      sourceAccountName: 'BA Amex',
      sourceCurrency: 'GBP',
      destinationAccountId: BARCLAYS_ID,
      destinationAccountName: 'Barclays',
      destinationCurrency: 'GBP',
      amount: 35,
      destinationAmount: 35,
      note: null,
    }

    function cardInCredit(credit: number) {
      getAccountMock.mockImplementation((id, year, month) =>
        Promise.resolve({ ...detailFor(id, year, month), totalOwed: -credit, allowedActions: ['AddCharge', 'AddRefund', 'RefundCreditToBank'] }),
      )
    }

    async function openRefundForm(user: ReturnType<typeof userEvent.setup>) {
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)
      await user.click(await screen.findByRole('button', { name: 'Refund credit to bank' }))
      return screen.getByRole('heading', { name: 'Refund credit to bank' }).closest('form') as HTMLElement
    }

    it('[AC P60-F15-card-refunds-and-credit-05] refunds the credit to the chosen bank, closes, reloads and returns focus to the action', async () => {
      const user = userEvent.setup()
      cardInCredit(35)
      createCardCreditRefundMock.mockResolvedValue(REFUND_TRANSFER)
      const form = await openRefundForm(user)
      const bank = within(form).getByRole('combobox', { name: /^To bank/ })
      expect(bank).toHaveFocus()
      expect(within(form).getByText('BA Amex')).toBeInTheDocument()
      expect(within(form).getByLabelText(/^Amount \(GBP\)/)).toHaveValue(35)
      expect(within(form).getByLabelText(/^Date/)).toHaveValue('2026-10-15')
      await within(form).findByRole('option', { name: 'Barclays' })
      expect(within(form).queryByRole('option', { name: 'BA Amex' })).not.toBeInTheDocument()
      const loadsBefore = getAccountMock.mock.calls.length

      await user.selectOptions(bank, BARCLAYS_ID)
      await user.click(within(form).getByRole('button', { name: 'Refund credit to bank' }))

      await waitFor(() =>
        expect(createCardCreditRefundMock).toHaveBeenCalledWith({
          date: '2026-10-15',
          sourceAccountId: AMEX_ID,
          destinationAccountId: BARCLAYS_ID,
          amount: 35,
          destinationAmount: null,
          note: null,
        }),
      )
      expect(await screen.findByText('Credit refunded')).toBeInTheDocument()
      expect(screen.queryByRole('heading', { name: 'Refund credit to bank' })).not.toBeInTheDocument()
      await waitFor(() => expect(getAccountMock.mock.calls.length).toBeGreaterThan(loadsBefore))
      await waitFor(() => expect(screen.getByRole('button', { name: 'Refund credit to bank' })).toHaveFocus())
    })

    it('[AC P60-F15-card-refunds-and-credit-05] blocks an amount above the credit on the focused field', async () => {
      const user = userEvent.setup()
      cardInCredit(35)
      const form = await openRefundForm(user)
      await user.selectOptions(within(form).getByRole('combobox', { name: /^To bank/ }), await within(form).findByRole('option', { name: 'Barclays' }))
      const amount = within(form).getByLabelText(/^Amount/)

      await user.clear(amount)
      await user.type(amount, '35.01')
      await user.click(within(form).getByRole('button', { name: 'Refund credit to bank' }))

      expect(await within(form).findByText("The card's credit is 35.00 GBP.")).toBeInTheDocument()
      expect(amount).toHaveFocus()
      expect(createCardCreditRefundMock).not.toHaveBeenCalled()
    })

    it('keeps the form and shows the message when the server refuses', async () => {
      const user = userEvent.setup()
      cardInCredit(35)
      createCardCreditRefundMock.mockRejectedValue(new ApiError('On that date the card had less credit than this refund; choose a later date.', 400))
      const form = await openRefundForm(user)
      await user.selectOptions(within(form).getByRole('combobox', { name: /^To bank/ }), await within(form).findByRole('option', { name: 'Barclays' }))

      await user.click(within(form).getByRole('button', { name: 'Refund credit to bank' }))

      expect(await within(form).findByText('On that date the card had less credit than this refund; choose a later date.')).toBeInTheDocument()
      expect(within(form).getByRole('combobox', { name: /^To bank/ })).toHaveValue(BARCLAYS_ID)
    })

    it('prompts before discarding a changed form', async () => {
      const user = userEvent.setup()
      cardInCredit(35)
      const form = await openRefundForm(user)
      await user.selectOptions(within(form).getByRole('combobox', { name: /^To bank/ }), await within(form).findByRole('option', { name: 'Barclays' }))

      await user.click(screen.getByRole('button', { name: 'Add charge' }))

      expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Refund credit to bank' })).toBeInTheDocument()
    })

    it('moves focus to the Transactions heading when the whole credit is refunded and the action goes', async () => {
      const user = userEvent.setup()
      cardInCredit(35)
      createCardCreditRefundMock.mockResolvedValue(REFUND_TRANSFER)
      const form = await openRefundForm(user)
      await user.selectOptions(within(form).getByRole('combobox', { name: /^To bank/ }), await within(form).findByRole('option', { name: 'Barclays' }))
      withActions(['AddCharge', 'AddRefund'])
      getAccountTransactionsMock.mockImplementation(() => Promise.resolve([...TRANSACTIONS]))

      await user.click(within(form).getByRole('button', { name: 'Refund credit to bank' }))

      await waitFor(() => expect(screen.queryByRole('button', { name: 'Refund credit to bank' })).not.toBeInTheDocument())
      await waitFor(() => expect(screen.getByRole('tab', { name: 'Transactions' })).toHaveAttribute('aria-selected', 'true'))
      await waitFor(() => expect(screen.getByRole('heading', { name: 'Transactions' })).toHaveFocus())
    })

    it('reloads the Statements tab after a refund made from it, and moves focus to the tab when the action goes', async () => {
      const user = userEvent.setup()
      cardInCredit(35)
      getAccountStatementsMock.mockResolvedValue(CARD_STATEMENTS)
      createCardCreditRefundMock.mockResolvedValue(REFUND_TRANSFER)
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)
      await user.click(await screen.findByRole('button', { name: 'Refund credit to bank' }))
      const form = screen.getByRole('heading', { name: 'Refund credit to bank' }).closest('form') as HTMLElement
      await user.selectOptions(within(form).getByRole('combobox', { name: /^To bank/ }), await within(form).findByRole('option', { name: 'Barclays' }))
      await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(1))
      withActions(['AddCharge', 'AddRefund'])
      getAccountTransactionsMock.mockImplementation(() => Promise.resolve([...TRANSACTIONS]))

      await user.click(within(form).getByRole('button', { name: 'Refund credit to bank' }))

      await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(2))
      await waitFor(() => expect(screen.queryByRole('button', { name: 'Refund credit to bank' })).not.toBeInTheDocument())
      await waitFor(() => expect(screen.getByRole('tab', { name: 'Statements' })).toHaveFocus())
    })

    it('does not offer the action unless the server allows it', async () => {
      withActions(['AddCharge', 'AddRefund'])
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      expect(await screen.findByRole('button', { name: 'Add refund' })).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Refund credit to bank' })).not.toBeInTheDocument()
    })

    const CREDIT_REFUND_ROW = accountTransactionRow({ id: 'cr1', kind: 'CardCreditRefund', date: '2026-10-15', description: 'BA Amex credit refund', counterpart: 'Barclays', amount: -35, canEdit: false, transfer: REFUND_TRANSFER })

    async function findCreditRefundRow(accountId: string) {
      getAccountTransactionsMock.mockResolvedValue([CREDIT_REFUND_ROW])
      renderScreen(`/cashflow/accounts/${accountId}?month=2026-10`)
      return (await screen.findByText('BA Amex credit refund')).closest('tr') as HTMLElement
    }

    it('[AC P60-F15-card-refunds-and-credit-05] labels a credit refund row with Delete as its only action', async () => {
      const row = await findCreditRefundRow(AMEX_ID)

      expect(within(row).getByText('Card credit refund')).toBeInTheDocument()
      expect(within(row).getByText('-35.00')).toBeInTheDocument()
      expect(within(row).getAllByRole('button').map((button) => button.getAttribute('aria-label'))).toEqual(['Delete credit refund'])
    })

    it.each([
      ['card', AMEX_ID],
      ['bank', BARCLAYS_ID],
    ])('[AC P60-F15-card-refunds-and-credit-05] deletes a credit refund from the %s screen after the confirmation, then reloads', async (_, accountId) => {
      const user = userEvent.setup()
      deleteCardCreditRefundMock.mockResolvedValue(undefined)
      const row = await findCreditRefundRow(accountId)
      const loadsBefore = getAccountMock.mock.calls.length

      await user.click(within(row).getByRole('button', { name: 'Delete credit refund' }))
      const dialog = await findDialog('Delete credit refund')
      expect(dialog).toHaveTextContent("Delete this credit refund? The card's credit returns.")
      let finishReload: (rows: AccountTransactionDto[]) => void = () => {}
      getAccountTransactionsMock.mockImplementation(() => new Promise((resolve) => { finishReload = resolve }))
      await user.click(within(dialog).getByText('Delete', { selector: 'button' }))

      await waitFor(() => expect(deleteCardCreditRefundMock).toHaveBeenCalledWith('cr1'))
      expect(deleteAccountTransferMock).not.toHaveBeenCalled()
      expect(await screen.findByText('Credit refund deleted')).toBeInTheDocument()
      await waitFor(() => expect(getAccountMock.mock.calls.length).toBeGreaterThan(loadsBefore))
      act(() => finishReload([]))
      await waitFor(() => expect(screen.queryByText('BA Amex credit refund')).not.toBeInTheDocument())
      await waitFor(() => expect(screen.getByRole('heading', { name: 'Transactions' })).toHaveFocus())
    })

    it('keeps the dialog and shows the message when the delete fails', async () => {
      const user = userEvent.setup()
      deleteCardCreditRefundMock.mockRejectedValue(new ApiError('Transfer not found.', 404))
      const row = await findCreditRefundRow(AMEX_ID)

      await user.click(within(row).getByRole('button', { name: 'Delete credit refund' }))
      const dialog = await findDialog('Delete credit refund')
      await user.click(within(dialog).getByText('Delete', { selector: 'button' }))

      expect(await within(dialog).findByText('Transfer not found.')).toBeInTheDocument()
      expect(screen.queryByText('Credit refund deleted')).not.toBeInTheDocument()
    })

    it('offers no Delete on the row when the screen account is inactive', async () => {
      getAccountMock.mockImplementation((id, year, month) => Promise.resolve({ ...detailFor(id, year, month), isActive: false, allowedActions: [] }))

      const row = await findCreditRefundRow(AMEX_ID)

      expect(within(row).queryByRole('button')).not.toBeInTheDocument()
    })

    it('shows the fresh statements on opening the Statements tab after a delete', async () => {
      const user = userEvent.setup()
      deleteCardCreditRefundMock.mockResolvedValue(undefined)
      getAccountStatementsMock.mockResolvedValue(CARD_STATEMENTS)
      const row = await findCreditRefundRow(AMEX_ID)
      await user.click(within(row).getByRole('button', { name: 'Delete credit refund' }))
      const dialog = await findDialog('Delete credit refund')
      await user.click(within(dialog).getByText('Delete', { selector: 'button' }))
      await screen.findByText('Credit refund deleted')
      expect(getAccountStatementsMock).not.toHaveBeenCalled()

      await user.click(screen.getByRole('tab', { name: 'Statements' }))

      await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(1))
    })
  })

  describe('receive card credit on a bank', () => {
    const CARD_CREDIT: CardCreditDto = { accountId: AMEX_ID, name: 'BA Amex', currency: 'GBP', isActive: false, credit: 35 }

    async function openReceiveForm(user: ReturnType<typeof userEvent.setup>) {
      withActions(['AddExpense', 'ReceiveCardCredit'])
      renderScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
      await user.click(await screen.findByRole('button', { name: 'Receive card credit' }))
      return screen.getByRole('heading', { name: 'Receive card credit' }).closest('form') as HTMLElement
    }

    it('[AC P60-F15-card-refunds-and-credit-05] receives the chosen card credit into the bank, closes with a toast and returns focus to the action', async () => {
      const user = userEvent.setup()
      getCardsInCreditMock.mockResolvedValue([CARD_CREDIT])
      createCardCreditRefundMock.mockResolvedValue({
        id: 'cr2',
        date: '2026-10-15',
        sourceAccountId: AMEX_ID,
        sourceAccountName: 'BA Amex',
        sourceCurrency: 'GBP',
        destinationAccountId: BARCLAYS_ID,
        destinationAccountName: 'Barclays',
        destinationCurrency: 'GBP',
        amount: 35,
        destinationAmount: 35,
        note: null,
      })
      const form = await openReceiveForm(user)
      const card = within(form).getByRole('combobox', { name: /^From/ })
      expect(card).toHaveFocus()
      expect(within(form).getByText('Barclays')).toBeInTheDocument()

      await user.selectOptions(card, await within(form).findByRole('option', { name: 'BA Amex (inactive) · Credit 35.00' }))
      expect(within(form).getByLabelText(/^Amount \(GBP\)/)).toHaveValue(35)
      await user.click(within(form).getByRole('button', { name: 'Receive card credit' }))

      await waitFor(() =>
        expect(createCardCreditRefundMock).toHaveBeenCalledWith({
          date: '2026-10-15',
          sourceAccountId: AMEX_ID,
          destinationAccountId: BARCLAYS_ID,
          amount: 35,
          destinationAmount: null,
          note: null,
        }),
      )
      expect(await screen.findByText('Card credit received')).toBeInTheDocument()
      expect(screen.queryByRole('heading', { name: 'Receive card credit' })).not.toBeInTheDocument()
      await waitFor(() => expect(screen.getByRole('button', { name: 'Receive card credit' })).toHaveFocus())
    })

    it('shows the hint and keeps the submit disabled when no card has credit', async () => {
      const user = userEvent.setup()
      getCardsInCreditMock.mockResolvedValue([])
      const form = await openReceiveForm(user)

      expect(await within(form).findByText('No card in GBP has credit to refund.')).toBeInTheDocument()
      expect(within(form).getByRole('button', { name: 'Receive card credit' })).toBeDisabled()
    })

    it('prompts before discarding a chosen card', async () => {
      const user = userEvent.setup()
      getCardsInCreditMock.mockResolvedValue([CARD_CREDIT])
      const form = await openReceiveForm(user)
      await user.selectOptions(within(form).getByRole('combobox', { name: /^From/ }), await within(form).findByRole('option', { name: /^BA Amex/ }))

      await user.click(screen.getByRole('button', { name: 'Add expense' }))

      expect(await findDialog('Discard unsaved changes?')).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'Receive card credit' })).toBeInTheDocument()
    })
  })

  describe('refunds', () => {
    const SHOES = { ...TAXI, id: 'r1', description: 'Shoes returned', value: -40, date: '2026-10-08', chargeDate: '2026-10-08' }
    const REFUND_ROWS = [
      ...CARD_ROWS,
      accountTransactionRow({ id: 'r1', kind: 'CardRefund', date: '2026-10-08', description: 'Shoes returned', amount: 40, canEdit: true, expense: SHOES }),
    ]

    it('[AC P60-F15-card-refunds-and-credit-01] adds a refund from a positive amount, saved negative, and stays open for the next one', async () => {
      const user = userEvent.setup()
      withActions(['AddCharge', 'AddRefund'])
      createExpenseMock.mockResolvedValue({ ...SHOES, id: 'r2' })
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      await user.click(await screen.findByRole('button', { name: 'Add refund' }))
      const form = screen.getByRole('heading', { name: 'New Refund' }).closest('form') as HTMLElement
      expect(within(form).getByText('BA Amex')).toBeInTheDocument()
      expect(within(form).getByLabelText(/^Date/)).toHaveValue('2026-10-15')
      expect(within(form).getByLabelText('Statement month')).toHaveValue('2026-10')
      await user.type(within(form).getByLabelText(/^Description/), 'Shoes returned')
      await user.type(within(form).getByLabelText(/^Amount/), '40')
      await user.click(within(form).getByRole('button', { name: 'Add Refund' }))

      await waitFor(() =>
        expect(createExpenseMock).toHaveBeenCalledWith(
          expect.objectContaining({ date: '2026-10-15', description: 'Shoes returned', value: -40, creditCardId: AMEX_ID, paymentSourceBankId: null, invoiceDate: '2026-10-01' }),
        ),
      )
      expect(await screen.findByText('Refund added')).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: 'New Refund' })).toBeInTheDocument()
      await waitFor(() => expect(screen.getByLabelText(/^Description/)).toHaveFocus())
    })

    it('blocks a refund with no amount, showing the message on the focused field', async () => {
      const user = userEvent.setup()
      withActions(['AddRefund'])
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      await user.click(await screen.findByRole('button', { name: 'Add refund' }))
      const form = screen.getByRole('heading', { name: 'New Refund' }).closest('form') as HTMLElement
      await user.type(within(form).getByLabelText(/^Description/), 'Shoes returned')
      await user.click(within(form).getByRole('button', { name: 'Add Refund' }))

      expect(await within(form).findByText('Amount must be greater than zero.')).toBeInTheDocument()
      expect(within(form).getByLabelText(/^Amount/)).toHaveFocus()
      expect(createExpenseMock).not.toHaveBeenCalled()
    })

    it('labels a refund row Refund with a positive amount and opens Edit in refund mode', async () => {
      const user = userEvent.setup()
      getAccountTransactionsMock.mockResolvedValue(REFUND_ROWS)
      updateExpenseMock.mockResolvedValue({ ...SHOES, value: -55 })
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      const row = (await screen.findByText('Shoes returned')).closest('tr') as HTMLElement
      expect(within(row).getByText('Refund')).toBeInTheDocument()
      expect(within(row).getByText('+40.00')).toBeInTheDocument()
      expect(within(row).getByRole('button', { name: 'Move refund to a later statement' })).toBeInTheDocument()

      await user.click(within(row).getByRole('button', { name: 'Edit refund' }))
      const form = screen.getByRole('heading', { name: 'Edit Refund' }).closest('form') as HTMLElement
      expect(within(form).getByLabelText(/^Amount/)).toHaveValue(40)
      await user.clear(within(form).getByLabelText(/^Amount/))
      await user.type(within(form).getByLabelText(/^Amount/), '55')
      await user.click(within(form).getByRole('button', { name: 'Save' }))

      await waitFor(() => expect(updateExpenseMock).toHaveBeenCalledWith('r1', expect.objectContaining({ value: -55, creditCardId: AMEX_ID })))
      expect(await screen.findByText('Refund updated')).toBeInTheDocument()
    })

    const openMoveRefund = async (user: ReturnType<typeof userEvent.setup>) => {
      await user.click(await screen.findByRole('button', { name: 'Move refund to a later statement' }))
      return screen.getByRole('heading', { name: 'Move refund to a later statement' }).closest('form') as HTMLElement
    }

    const moveToDecember = async (user: ReturnType<typeof userEvent.setup>, form: HTMLElement) => {
      const statement = within(form).getByLabelText(/^Statement/)
      await waitFor(() => expect(within(statement).getAllByRole('option')).toHaveLength(13))
      await user.selectOptions(statement, '2026-12')
      await user.click(within(form).getByRole('button', { name: 'Move refund' }))
    }

    it('moves a refund with refund wording and the positive amount the grid shows', async () => {
      const user = userEvent.setup()
      getAccountTransactionsMock.mockResolvedValue(REFUND_ROWS)
      moveChargeToStatementMock.mockResolvedValue({ ...SHOES, invoiceDate: '2026-12-01' })
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      const form = await openMoveRefund(user)
      expect(within(form).getByText('Shoes returned (08/10/2026, 40.00) is on the October 2026 statement.')).toBeInTheDocument()
      await moveToDecember(user, form)

      expect(moveChargeToStatementMock).toHaveBeenCalledWith('r1', { year: 2026, month: 12 })
      expect(await screen.findByText('Refund moved')).toBeInTheDocument()
    })

    it('says the refund failed to move when the failure has no server message', async () => {
      const user = userEvent.setup()
      getAccountTransactionsMock.mockResolvedValue(REFUND_ROWS)
      moveChargeToStatementMock.mockRejectedValue('no message')
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      const form = await openMoveRefund(user)
      await moveToDecember(user, form)

      expect(await within(form).findByText('Failed to move the refund')).toBeInTheDocument()
      expect(screen.queryByText('Refund moved')).not.toBeInTheDocument()
    })

    it('asks before discarding a typed refund when another action opens', async () => {
      const user = userEvent.setup()
      withActions(['AddCharge', 'AddRefund'])
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      await user.click(await screen.findByRole('button', { name: 'Add refund' }))
      await user.type(screen.getByLabelText(/^Amount/), '40')
      await user.click(screen.getByRole('button', { name: 'Add charge' }))

      await user.click(await dialogButton('Discard unsaved changes?', 'Keep editing'))
      expect(screen.getByRole('heading', { name: 'New Refund' })).toBeInTheDocument()
      expect(screen.getByLabelText(/^Amount/)).toHaveValue(40)
    })

    it('deletes a refund after confirming with its own wording', async () => {
      const user = userEvent.setup()
      getAccountTransactionsMock.mockResolvedValue(REFUND_ROWS)
      renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10`)

      await user.click(await screen.findByRole('button', { name: 'Delete refund' }))
      getAccountTransactionsMock.mockResolvedValue(CARD_ROWS)
      await user.click(await dialogButton('Delete refund', 'Delete'))

      expect(deleteExpenseMock).toHaveBeenCalledWith('r1')
      expect(await screen.findByText('Refund deleted')).toBeInTheDocument()
    })
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

  it('saves a due date from the Statements tab, shows the toast and reloads the statements', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    await user.click(await screen.findByRole('button', { name: 'Set the October 2026 due date' }))
    fireEvent.change(screen.getByLabelText(/^Due date/), { target: { value: '2026-10-25' } })
    await user.click(screen.getByRole('button', { name: 'Save due date' }))

    expect(setStatementDueDateMock).toHaveBeenCalledWith(AMEX_ID, 2026, 10, { dueDate: '2026-10-25' })
    expect(await screen.findByText('Due date saved')).toBeInTheDocument()
    await waitFor(() => expect(getAccountStatementsMock).toHaveBeenCalledTimes(2))
  })

  it('asks before a tab change discards an edited due date', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    await user.click(await screen.findByRole('button', { name: 'Set the October 2026 due date' }))
    fireEvent.change(screen.getByLabelText(/^Due date/), { target: { value: '2026-10-25' } })
    await user.click(screen.getByRole('tab', { name: 'Transactions' }))
    await user.click(await dialogButton('Discard unsaved changes?', 'Discard'))

    expect(await screen.findByRole('tab', { name: 'Transactions', selected: true })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save due date' })).not.toBeInTheDocument()
  })

  it('keeps one form open: opening Pay closes an untouched due-date form', async () => {
    const user = userEvent.setup()
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    await user.click(await screen.findByRole('button', { name: 'Set the October 2026 due date' }))
    await user.click(screen.getByRole('button', { name: 'Pay October 2026 statement' }))

    expect(screen.queryByRole('button', { name: 'Save due date' })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pay statement' })).toBeInTheDocument()
  })

  it('shows no Pay or Unmark on an inactive card', async () => {
    getAccountMock.mockImplementation((id, year, month) => Promise.resolve({ ...detailFor(id, year, month), isActive: false }))
    renderScreen(`/cashflow/accounts/${AMEX_ID}?month=2026-10&tab=statements`)

    expect(await screen.findByText('Paid 512.30 on 12 Oct 2026 from Barclays')).toBeInTheDocument()
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

  it('reloads an open Pay card statement form after a credit refund is deleted, because its amounts can change', async () => {
    const user = userEvent.setup()
    deleteCardCreditRefundMock.mockResolvedValue(undefined)
    getAccountTransactionsMock.mockResolvedValue([
      accountTransactionRow({ id: 'cr1', kind: 'CardCreditRefund', date: '2026-10-15', description: 'BA Amex credit refund', counterpart: 'BA Amex', amount: 35, canEdit: false, transfer: { id: 'cr1' } as AccountTransactionDto['transfer'] }),
    ])
    await openBank()
    await user.click(screen.getByRole('button', { name: 'Pay card statement' }))
    await waitFor(() => expect(getPayableStatementsMock).toHaveBeenCalledTimes(1))

    await user.click(await screen.findByRole('button', { name: 'Delete credit refund' }))
    const dialog = await findDialog('Delete credit refund')
    await user.click(within(dialog).getByText('Delete', { selector: 'button' }))

    await waitFor(() => expect(getPayableStatementsMock).toHaveBeenCalledTimes(2))
    expect(payForm()).toBeInTheDocument()
  })

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
