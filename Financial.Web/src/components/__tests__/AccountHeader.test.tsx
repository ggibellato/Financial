import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AccountDetailDto, AccountDto, AccountStatementDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import AccountHeader from '../AccountHeader'
import { account, settlementAccount } from '../../test/account'

const ACCOUNTS: AccountDto[] = [
  account({ id: 'barclays', name: 'Barclays' }),
  account({ id: 'chase', name: 'Chase' }),
  account({ id: 'old', name: 'Old Savings', isActive: false }),
  account({ id: 'wallet', name: 'Wallet', templateName: 'Cash', currency: 'BRL' }),
  account({ id: 'amex', name: 'BA Amex', templateName: 'CreditCard' }),
  settlementAccount({ id: 'mae', name: 'Mãe' }),
]

const OCTOBER_UNPAID: AccountStatementDto = {
  id: 's-oct',
  year: 2026,
  month: 10,
  total: 650.1,
  creditIn: 0,
  outstandingTotal: 650.1,
  creditCarried: 0,
  status: 'Unpaid',
  isPaid: false,
  dueDate: '2026-11-03',
  paidOn: null,
  paidAmount: null,
  paidFromAccountName: null,
}

const AMEX: AccountDetailDto = {
  id: 'amex',
  name: 'BA Amex',
  templateName: 'CreditCard',
  currency: 'GBP',
  isActive: true,
  year: 2026,
  month: 10,
  balance: -812.33,
  balanceIsOwed: true,
  hasStatements: true,
  totalOwed: 740.12,
  viewedStatement: OCTOBER_UNPAID,
  allowedActions: [],
  disabledActions: {},
  currentBalances: [],
  otherParticipant: null,
}

const BARCLAYS: AccountDetailDto = {
  id: 'barclays',
  name: 'Barclays',
  templateName: 'Bank',
  currency: 'GBP',
  isActive: true,
  year: 2026,
  month: 10,
  balance: 1234.56,
  balanceIsOwed: false,
  hasStatements: false,
  totalOwed: null,
  viewedStatement: null,
  allowedActions: [],
  disabledActions: {},
  currentBalances: [],
  otherParticipant: null,
}

function renderHeader(
  account: AccountDetailDto = BARCLAYS,
  { selectedAccountId = account.id, isBusy = false } = {},
) {
  const handlers = { onMonthChange: vi.fn(), onAccountSelect: vi.fn() }
  render(
    <AccountHeader
      account={account}
      accounts={ACCOUNTS}
      selectedAccountId={selectedAccountId}
      monthInputValue="2026-10"
      isBusy={isBusy}
      onMonthChange={handlers.onMonthChange}
      onAccountSelect={handlers.onAccountSelect}
    />,
  )
  return handlers
}

describe('AccountHeader', () => {
  it('shows the name, template, currency and month-end balance', () => {
    renderHeader()

    expect(screen.getByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
    expect(screen.getByText('Bank · GBP')).toBeInTheDocument()
    expect(screen.getByText('Balance at end of October 2026')).toBeInTheDocument()
    expect(screen.getByText('1,234.56 GBP')).toBeInTheDocument()
  })

  it('[AC P60-F07-credit-card-template-and-card-migration-08] shows a card under its template\'s display name, with no bank balance label', () => {
    renderHeader(AMEX)

    expect(screen.getByText('Credit card · GBP')).toBeInTheDocument()
    expect(screen.queryByText(/at end of/)).not.toBeInTheDocument()
  })

  it("[AC P60-F15-card-refunds-and-credit-07] shows a card's viewed statement as unpaid with its amount to pay, not its month-end balance, and the total owed", () => {
    renderHeader(AMEX)

    const statementFigure = screen.getByText('October 2026 statement').parentElement as HTMLElement
    expect(within(statementFigure).getByText('650.10 GBP')).toBeInTheDocument()
    expect(within(statementFigure).getByText('Unpaid')).toBeInTheDocument()
    expect(screen.getByText('Total owed')).toBeInTheDocument()
    expect(screen.getByText('740.12 GBP')).toBeInTheDocument()
    expect(screen.queryByText('812.33 GBP')).not.toBeInTheDocument()
  })

  it('[AC P60-F15-card-refunds-and-credit-07] shows a paid statement with the amount paid and its date', () => {
    renderHeader({
      ...AMEX,
      viewedStatement: { ...OCTOBER_UNPAID, outstandingTotal: 0, status: 'Paid', isPaid: true, paidOn: '2026-11-12', paidAmount: 650.1, paidFromAccountName: 'Barclays' },
    })

    expect(screen.getByText('650.10 GBP')).toBeInTheDocument()
    expect(screen.getByText('Paid 12 Nov 2026')).toBeInTheDocument()
  })

  it('shows Paid alone when the payment date is unknown, with a missing amount as 0.00', () => {
    renderHeader({ ...AMEX, viewedStatement: { ...OCTOBER_UNPAID, outstandingTotal: 0, status: 'Paid', isPaid: true } })

    expect(screen.getByText('0.00 GBP')).toBeInTheDocument()
    expect(screen.getByText('Paid')).toBeInTheDocument()
  })

  it('[AC P60-F15-card-refunds-and-credit-07] shows a Nothing due statement with the credit it carries', () => {
    renderHeader({ ...AMEX, viewedStatement: { ...OCTOBER_UNPAID, total: -35, outstandingTotal: 0, creditCarried: 35, status: 'NothingDue' } })

    expect(screen.getByText('0.00 GBP')).toBeInTheDocument()
    expect(screen.getByText('Nothing due · 35.00 credit carried')).toBeInTheDocument()
  })

  it('shows a month before the card\'s first statement as 0.00 Nothing due', () => {
    renderHeader({ ...AMEX, viewedStatement: null })

    expect(screen.getByText('October 2026 statement')).toBeInTheDocument()
    expect(screen.getByText('0.00 GBP')).toBeInTheDocument()
    expect(screen.getByText('Nothing due')).toBeInTheDocument()
  })

  it("[AC P60-F14-accounts-page-by-template-and-card-totals-03] a month change changes the statement figure but not the total owed", () => {
    const header = (detail: AccountDetailDto) => (
      <AccountHeader
        account={detail}
        accounts={ACCOUNTS}
        selectedAccountId={detail.id}
        monthInputValue={`${detail.year}-${String(detail.month).padStart(2, '0')}`}
        isBusy={false}
        onMonthChange={vi.fn()}
        onAccountSelect={vi.fn()}
      />
    )
    const { rerender } = render(header({ ...AMEX, month: 9, viewedStatement: { ...OCTOBER_UNPAID, month: 9, outstandingTotal: 154.04 } }))

    expect(screen.getByText('September 2026 statement')).toBeInTheDocument()
    expect(screen.getByText('154.04 GBP')).toBeInTheDocument()
    expect(screen.getByText('740.12 GBP')).toBeInTheDocument()

    rerender(header(AMEX))

    expect(screen.getByText('October 2026 statement')).toBeInTheDocument()
    expect(screen.getByText('650.10 GBP')).toBeInTheDocument()
    expect(screen.getByText('740.12 GBP')).toBeInTheDocument()
  })

  it('shows a total owed of nothing as 0.00, not -0.00', () => {
    renderHeader({ ...AMEX, viewedStatement: null, totalOwed: -0 })

    expect(screen.getAllByText('0.00 GBP')).toHaveLength(2)
  })

  it('[AC P60-F15-card-refunds-and-credit-07] shows a card in credit as Credit instead of a negative total owed', () => {
    renderHeader({ ...AMEX, totalOwed: -35 })

    expect(screen.getByText('Total owed')).toBeInTheDocument()
    expect(screen.getByText('Credit 35.00 GBP')).toBeInTheDocument()
  })

  it('[AC P60-F15-card-refunds-and-credit-07] leaves a bank header on its month-end balance, with no statement or total owed', () => {
    renderHeader()

    expect(screen.getByText('Balance at end of October 2026')).toBeInTheDocument()
    expect(screen.queryByText(/ statement$/)).not.toBeInTheDocument()
    expect(screen.queryByText('Total owed')).not.toBeInTheDocument()
  })

  it('shows no Inactive badge for an active account', () => {
    renderHeader()

    expect(screen.queryByText('Inactive')).not.toBeInTheDocument()
  })

  it('shows an Inactive badge for an inactive account and keeps it as the picker value', () => {
    renderHeader({ ...BARCLAYS, id: 'old', name: 'Old Savings', isActive: false })

    expect(screen.getByText('Inactive')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Account' })).toHaveTextContent('Old Savings')
  })

  it('keeps an account with participants opened from its URL as the picker value', () => {
    renderHeader({ ...BARCLAYS, id: 'mae', name: 'Mãe', templateName: 'Settlement', currency: 'BRL' })

    expect(screen.getByRole('combobox', { name: 'Account' })).toHaveTextContent('Mãe')
  })

  it('lists only active accounts in the picker, grouped by template', async () => {
    const user = userEvent.setup()
    renderHeader()

    await user.click(screen.getByRole('combobox', { name: 'Account' }))

    const bankGroup = screen.getByRole('group', { name: 'Bank' })
    expect(within(bankGroup).getAllByRole('option').map((option) => option.textContent)).toEqual(['Barclays', 'Chase'])
    expect(within(screen.getByRole('group', { name: 'Cash' })).getByRole('option', { name: 'Wallet' })).toBeInTheDocument()
    expect(within(screen.getByRole('group', { name: 'Credit card' })).getByRole('option', { name: 'BA Amex' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Old Savings' })).not.toBeInTheDocument()
  })

  it('leaves accounts with participants out of the picker and keeps the others', async () => {
    const user = userEvent.setup()
    renderHeader()

    await user.click(screen.getByRole('combobox', { name: 'Account' }))

    expect(screen.queryByRole('option', { name: 'Mãe' })).not.toBeInTheDocument()
    expect(screen.queryByRole('group', { name: 'Settlement' })).not.toBeInTheDocument()
    expect(screen.getAllByRole('option').map((option) => option.textContent)).toEqual(['Barclays', 'Chase', 'Wallet', 'BA Amex'])
  })

  it('reports the chosen account', async () => {
    const user = userEvent.setup()
    const { onAccountSelect } = renderHeader()

    await user.click(screen.getByRole('combobox', { name: 'Account' }))
    await user.click(screen.getByRole('option', { name: 'Chase' }))

    expect(onAccountSelect).toHaveBeenCalledWith('chase')
  })

  it('does not report reselecting the current account', async () => {
    const user = userEvent.setup()
    const { onAccountSelect } = renderHeader()

    await user.click(screen.getByRole('combobox', { name: 'Account' }))
    await user.click(screen.getByRole('option', { name: 'Barclays' }))

    expect(onAccountSelect).not.toHaveBeenCalled()
  })

  it('shows the account being navigated to as the picker value while the header still shows the loaded one', () => {
    renderHeader(BARCLAYS, { selectedAccountId: 'chase', isBusy: true })

    expect(screen.getByRole('combobox', { name: 'Account' })).toHaveTextContent('Chase')
    expect(screen.getByRole('heading', { level: 1, name: 'Barclays' })).toBeInTheDocument()
    expect(screen.getByRole('banner')).toHaveAttribute('aria-busy', 'true')
  })

  it('keeps focus on the picker after an account is chosen with the keyboard', async () => {
    const user = userEvent.setup()
    renderHeader()
    const picker = screen.getByRole('combobox', { name: 'Account' })

    picker.focus()
    await user.keyboard('{Enter}{ArrowDown}{Enter}')

    expect(picker).toHaveFocus()
  })

  it('reports a month change', () => {
    const { onMonthChange } = renderHeader()

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2026-09' } })

    expect(onMonthChange).toHaveBeenCalledWith('2026-09')
  })
})
