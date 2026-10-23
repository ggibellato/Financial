import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { AccountDetailDto, AccountDto } from '../../api/types'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import AccountHeader from '../AccountHeader'

const ACCOUNTS: AccountDto[] = [
  { id: 'barclays', name: 'Barclays', templateName: 'Bank', currency: 'GBP', isActive: true },
  { id: 'chase', name: 'Chase', templateName: 'Bank', currency: 'GBP', isActive: true },
  { id: 'old', name: 'Old Savings', templateName: 'Bank', currency: 'GBP', isActive: false },
  { id: 'wallet', name: 'Wallet', templateName: 'Cash', currency: 'BRL', isActive: true },
]

const BARCLAYS: AccountDetailDto = {
  id: 'barclays',
  name: 'Barclays',
  templateName: 'Bank',
  currency: 'GBP',
  isActive: true,
  year: 2026,
  month: 10,
  balance: 1234.56,
  allowedActions: [],
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

  it('shows no Inactive badge for an active account', () => {
    renderHeader()

    expect(screen.queryByText('Inactive')).not.toBeInTheDocument()
  })

  it('shows an Inactive badge for an inactive account and keeps it as the picker value', () => {
    renderHeader({ ...BARCLAYS, id: 'old', name: 'Old Savings', isActive: false })

    expect(screen.getByText('Inactive')).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: 'Account' })).toHaveTextContent('Old Savings')
  })

  it('lists only active accounts in the picker, grouped by template', async () => {
    const user = userEvent.setup()
    renderHeader()

    await user.click(screen.getByRole('combobox', { name: 'Account' }))

    const bankGroup = screen.getByRole('group', { name: 'Bank' })
    expect(within(bankGroup).getAllByRole('option').map((option) => option.textContent)).toEqual(['Barclays', 'Chase'])
    expect(within(screen.getByRole('group', { name: 'Cash' })).getByRole('option', { name: 'Wallet' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Old Savings' })).not.toBeInTheDocument()
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
