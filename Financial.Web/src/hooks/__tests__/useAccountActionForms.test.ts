import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDetailDto, BankDto, CategoryDto, ExpenseDto, IncomeDto, IncomeSourceDto } from '../../api/types'
import { accountTransactionRow } from '../../test/accountTransactionRow'
import { pinDate } from '../../test-utils/pinDate'
import type { AccountFormOptions } from '../useAccountFormOptions'
import { useAccountActionForms } from '../useAccountActionForms'

const { createExpenseMock, createIncomeMock, getMensaisBillsMock } = vi.hoisted(() => ({
  createExpenseMock: vi.fn<FinancialApiClient['createExpense']>(),
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createExpense: createExpenseMock,
    createIncome: createIncomeMock,
    getMensaisBills: getMensaisBillsMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS: AccountDetailDto = {
  id: 'bank-barclays',
  name: 'Barclays',
  currency: 'GBP',
  templateName: 'Bank',
  balance: 1250,
  balanceIsOwed: false,
  isActive: true,
  allowedActions: ['AddExpense', 'AddIncome', 'TransferTo', 'TransferFrom', 'PayRecurringBill', 'Reconcile'],
  year: 2026,
  month: 10,
}

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
]
const CATEGORIES: CategoryDto[] = [
  { id: 'category-extras', name: 'Extras', active: true, isInvestment: false, isTithe: false, hasReferences: false },
]
const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: 'source-salary', name: 'Salary', isActive: true, group: 'Salary', autoSplitToReserve: true, hasReferences: false },
]
const OPTIONS: AccountFormOptions = {
  banks: BANKS,
  categories: CATEGORIES,
  incomeSources: INCOME_SOURCES,
  destinations: [{ id: 'bank-chase', name: 'Chase', currency: 'GBP' }],
  sources: [],
}

const EXPENSE: ExpenseDto = {
  id: 'e1',
  date: '2026-10-05',
  description: 'Lidl',
  value: 42.5,
  categoryId: 'category-extras',
  categoryName: 'Extras',
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
}

const INCOME: IncomeDto = {
  id: 'i1',
  date: '2026-10-01',
  incomeSourceId: 'source-salary',
  incomeSourceName: 'Salary',
  grossValue: null,
  netValue: 2450,
  bankId: 'bank-barclays',
  bankName: 'Barclays',
  description: null,
  splitToReserve: false,
}

const EXPENSE_ROW = accountTransactionRow({ id: 'e1', kind: 'Expense', date: '2026-10-05', amount: -42.5, expense: EXPENSE })
const INCOME_ROW = accountTransactionRow({ id: 'i1', kind: 'Income', date: '2026-10-01', amount: 2450, income: INCOME })

interface HookProps {
  routeAccountId: string
  account: AccountDetailDto | null
}

function renderActionForms(onSaved = vi.fn<(message: string) => void>()) {
  const rendered = renderHook(
    ({ routeAccountId, account }: HookProps) => useAccountActionForms(routeAccountId, account, OPTIONS, { year: 2026, month: 10 }, onSaved),
    { initialProps: { routeAccountId: 'bank-barclays', account: BARCLAYS } },
  )
  const formArea = document.createElement('div')
  formArea.innerHTML = '<input name="date" /><input name="description" /><button>Save</button>'
  document.body.appendChild(formArea)
  rendered.result.current.formAreaRef.current = formArea
  return { ...rendered, formArea, onSaved }
}

function openerButton(): HTMLButtonElement {
  const button = document.createElement('button')
  document.body.appendChild(button)
  return button
}

describe('useAccountActionForms', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    createExpenseMock.mockReset()
    createIncomeMock.mockReset()
    getMensaisBillsMock.mockReset()
    getMensaisBillsMock.mockResolvedValue([])
    sessionStorage.clear()
  })

  afterEach(() => {
    vi.useRealTimers()
    document.body.innerHTML = ''
  })

  it('locks the forms to the route account, matching its id case-insensitively', () => {
    const { result } = renderHook(() =>
      useAccountActionForms('BANK-BARCLAYS', BARCLAYS, OPTIONS, { year: 2026, month: 10 }, vi.fn()),
    )

    expect(result.current.isAccountCurrent).toBe(true)
    expect(result.current.lockedBank).toEqual({ id: 'bank-barclays', name: 'Barclays', currency: 'GBP' })
  })

  it('locks nothing while the loaded account is still the previous route account', () => {
    const { result } = renderHook(() => useAccountActionForms('bank-chase', BARCLAYS, OPTIONS, { year: 2026, month: 10 }, vi.fn()))

    expect(result.current.isAccountCurrent).toBe(false)
    expect(result.current.lockedBank).toBeUndefined()
  })

  it('locks nothing before the account has loaded', () => {
    const { result } = renderHook(() => useAccountActionForms('bank-barclays', null, OPTIONS, { year: 2026, month: 10 }, vi.fn()))

    expect(result.current.isAccountCurrent).toBe(false)
    expect(result.current.lockedBank).toBeUndefined()
  })

  it.each([
    ['AddExpense', (r: ReturnType<typeof useAccountActionForms>) => r.expenseForm.isOpen],
    ['AddIncome', (r: ReturnType<typeof useAccountActionForms>) => r.incomeForm.isIncomeFormOpen],
    ['TransferTo', (r: ReturnType<typeof useAccountActionForms>) => r.transferForm.isOpen && r.transferForm.direction === 'to'],
    ['TransferFrom', (r: ReturnType<typeof useAccountActionForms>) => r.transferForm.isOpen && r.transferForm.direction === 'from'],
    ['Reconcile', (r: ReturnType<typeof useAccountActionForms>) => r.adjustmentForm.isOpen],
    ['PayRecurringBill', (r: ReturnType<typeof useAccountActionForms>) => r.payBillForm.isOpen],
  ] as const)('opens the %s form as the active action and focuses its first field', (action, isOpen) => {
    const { result, formArea } = renderActionForms()

    act(() => result.current.openAction(action, openerButton()))

    expect(isOpen(result.current)).toBe(true)
    expect(result.current.activeAction).toBe(action)
    expect(document.activeElement).toBe(formArea.querySelector('input[name="date"]'))
  })

  it('closes the open form when another action is opened', () => {
    const { result } = renderActionForms()
    act(() => result.current.openAction('AddExpense', openerButton()))

    act(() => result.current.openAction('AddIncome', openerButton()))

    expect(result.current.expenseForm.isOpen).toBe(false)
    expect(result.current.incomeForm.isIncomeFormOpen).toBe(true)
    expect(result.current.activeAction).toBe('AddIncome')
  })

  it('closes the form and returns focus to its button when the active action is opened again', () => {
    const { result } = renderActionForms()
    const button = openerButton()
    act(() => result.current.openAction('AddExpense', button))

    act(() => result.current.openAction('AddExpense', button))

    expect(result.current.expenseForm.isOpen).toBe(false)
    expect(result.current.activeAction).toBeNull()
    expect(document.activeElement).toBe(button)
  })

  it('asks before discarding unsaved changes and opens the next action only once discarded', () => {
    const { result } = renderActionForms()
    act(() => result.current.openAction('AddExpense', openerButton()))
    act(() => result.current.expenseForm.setField('description', 'Tesco'))

    act(() => result.current.openAction('AddIncome', openerButton()))

    expect(result.current.discardPrompt.isPromptOpen).toBe(true)
    expect(result.current.expenseForm.isOpen).toBe(true)
    expect(result.current.incomeForm.isIncomeFormOpen).toBe(false)

    act(() => result.current.discardPrompt.discard())

    expect(result.current.expenseForm.isOpen).toBe(false)
    expect(result.current.incomeForm.isIncomeFormOpen).toBe(true)
  })

  it('keeps the edited form when the discard prompt is answered with keep editing', () => {
    const { result } = renderActionForms()
    act(() => result.current.openAction('AddExpense', openerButton()))
    act(() => result.current.expenseForm.setField('description', 'Tesco'))
    act(() => result.current.openAction('AddIncome', openerButton()))

    act(() => result.current.discardPrompt.keepEditing())

    expect(result.current.discardPrompt.isPromptOpen).toBe(false)
    expect(result.current.expenseForm.description).toBe('Tesco')
    expect(result.current.incomeForm.isIncomeFormOpen).toBe(false)
  })

  it('closes every form when the route moves to another account', () => {
    const { result, rerender } = renderActionForms()
    act(() => result.current.openAction('AddExpense', openerButton()))

    rerender({ routeAccountId: 'bank-chase', account: BARCLAYS })

    expect(result.current.expenseForm.isOpen).toBe(false)
    expect(result.current.activeAction).toBeNull()
  })

  it('opens a row edit without making it a create action', () => {
    const { result } = renderActionForms()

    act(() => result.current.openRowEdit(EXPENSE_ROW, openerButton()))

    expect(result.current.expenseForm.isOpen).toBe(true)
    expect(result.current.expenseForm.isEditing).toBe(true)
    expect(result.current.expenseForm.description).toBe('Lidl')
    expect(result.current.activeAction).toBeNull()
  })

  it('opens the income form for an income row', () => {
    const { result } = renderActionForms()

    act(() => result.current.openRowEdit(INCOME_ROW, openerButton()))

    expect(result.current.incomeForm.isIncomeEditing).toBe(true)
    expect(result.current.expenseForm.isOpen).toBe(false)
  })

  it('closes the forms of the edited row only', () => {
    const { result } = renderActionForms()
    act(() => result.current.openRowEdit(EXPENSE_ROW, openerButton()))

    act(() => result.current.closeFormsOf(INCOME_ROW))
    expect(result.current.expenseForm.isOpen).toBe(true)

    act(() => result.current.closeFormsOf(EXPENSE_ROW))
    expect(result.current.expenseForm.isOpen).toBe(false)
  })

  it('closes the forms before running a guarded change', () => {
    const { result } = renderActionForms()
    const change = vi.fn(() => expect(result.current.expenseForm.isOpen).toBe(true))
    act(() => result.current.openAction('AddExpense', openerButton()))

    act(() => result.current.guardChange(change))

    expect(change).toHaveBeenCalledOnce()
    expect(result.current.expenseForm.isOpen).toBe(false)
  })

  it('returns focus to the opening button when the active form is closed', () => {
    const { result } = renderActionForms()
    const button = openerButton()
    act(() => result.current.openAction('Reconcile', button))

    act(() => result.current.closeActiveForm())

    expect(result.current.adjustmentForm.isOpen).toBe(false)
    expect(document.activeElement).toBe(button)
  })

  it('announces a saved expense and moves focus to the description for the next entry', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSE, id: 'e2' })
    const { result, formArea, onSaved } = renderActionForms()
    act(() => result.current.openAction('AddExpense', openerButton()))
    act(() => result.current.expenseForm.setField('date', '2026-10-14'))
    act(() => result.current.expenseForm.setField('description', 'Tesco'))
    act(() => result.current.expenseForm.setField('value', '42.10'))
    act(() => result.current.expenseForm.setField('categoryId', 'category-extras'))

    await act(() => result.current.expenseForm.submit())

    expect(onSaved).toHaveBeenCalledWith('Expense added')
    expect(result.current.expenseForm.isOpen).toBe(true)
    expect(document.activeElement).toBe(formArea.querySelector('input[name="description"]'))
  })

  it.each([
    [true, 'Income added and split to reserve'],
    [false, 'Income added'],
  ])('announces a saved income whose split to reserve is %s', async (splitToReserve, message) => {
    createIncomeMock.mockResolvedValue({ ...INCOME, id: 'i2', splitToReserve })
    const { result, onSaved } = renderActionForms()
    act(() => result.current.openAction('AddIncome', openerButton()))
    act(() => result.current.incomeForm.setIncomeField('date', '2026-10-14'))
    act(() => result.current.incomeForm.setIncomeField('netValue', '2450'))

    await act(async () => {
      result.current.incomeForm.submitIncome()
    })

    expect(onSaved).toHaveBeenCalledWith(message)
  })
})
