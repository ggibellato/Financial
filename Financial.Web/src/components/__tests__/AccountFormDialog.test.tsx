import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { AccountTemplateDto, CategoryDto } from '../../api/types'
import { SAVINGS_CAPABILITIES, account, cardAccount, potsAccount, savingsAccount, settlementAccount } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { duplicateNameRejection, expectDuplicateUnderName, saveFromTheSaveButton } from '../../test-utils/duplicateName'
import AccountFormDialog from '../AccountFormDialog'
import { findErrorAlert, getErrorAlert } from '../../test-utils/errorAlert'
import { pinDate } from '../../test-utils/pinDate'

const BANK: AccountTemplateDto = {
  name: 'Bank',
  currencies: ['GBP'],
  capabilities: ['HasOpeningBalance', 'ReceivesIncome', 'SupportsRoundUp'],
}
const PLAIN: AccountTemplateDto = { name: 'Plain', currencies: ['GBP', 'BRL'], capabilities: [] }
const CREDIT_CARD: AccountTemplateDto = { name: 'CreditCard', currencies: ['GBP'], capabilities: ['HasStatements', 'HasCategories', 'ReceivesPayments'] }
const POTS: AccountTemplateDto = { name: 'Pots', currencies: ['GBP'], capabilities: ['HasPots', 'ReceivesIncomeSplit', 'CanWithdraw'] }
const SETTLEMENT: AccountTemplateDto = {
  name: 'Settlement',
  currencies: ['BRL', 'GBP'],
  capabilities: ['HasParticipants', 'MultiCurrency', 'CanSettle'],
}
const SAVINGS: AccountTemplateDto = { name: 'Savings', currencies: ['GBP'], capabilities: SAVINGS_CAPABILITIES }
const MAE = settlementAccount({ id: 'm1', name: 'Mãe' })
const SETTLEMENT_USD: AccountTemplateDto = { ...SETTLEMENT, name: 'SettlementUsd', currencies: ['GBP', 'USD'] }

const category = (id: string, name: string, overrides: Partial<CategoryDto> = {}): CategoryDto => ({
  id,
  name,
  active: true,
  isInvestment: false,
  isTithe: false,
  hasReferences: false,
  ...overrides,
})

const CATEGORIES = [
  category('c1', 'Reserva'),
  category('c2', 'Holidays'),
  category('c3', 'Investimento', { isInvestment: true }),
  category('c4', 'Old', { active: false }),
]

function renderDialog(props: Partial<Parameters<typeof AccountFormDialog>[0]> = {}) {
  const onSubmit = vi.fn<(values: unknown) => Promise<unknown>>().mockResolvedValue(undefined)
  const onCancel = vi.fn()
  render(
    <AccountFormDialog account={null} templates={[BANK]} categories={CATEGORIES} onCancel={onCancel} onSubmit={onSubmit} {...props} />,
  )
  return { onSubmit: props.onSubmit ?? onSubmit, onCancel: props.onCancel ?? onCancel }
}

const save = (name = 'Add account') => fireEvent.click(screen.getByRole('button', { name }))

describe('AccountFormDialog', () => {
  beforeEach(() => {
    pinDate('2026-10-24T12:00:00')
  })

  it('opens New account with the template first, then the Bank fields with their defaults, and no Active switch', () => {
    renderDialog()

    const dialog = screen.getByRole('dialog', { name: 'New account' })
    const labels = within(dialog).getAllByText(/^(Template|Name|Currency|Opening balance \(GBP\)|Balance start date|Round-up)$/)
    expect(labels.map((label) => label.textContent?.replace('*', ''))).toEqual([
      'Template',
      'Name',
      'Currency',
      'Opening balance (GBP)',
      'Balance start date',
      'Round-up',
    ])
    expect(screen.getByLabelText(/Template/)).toHaveValue('Bank')
    expect(screen.getByLabelText(/Currency/)).toHaveValue('GBP')
    expect(screen.getByLabelText(/Opening balance/)).toHaveValue(0)
    expect(screen.getByLabelText(/Balance start date/)).toHaveValue('2026-10-24')
    expect(screen.getByLabelText(/Currency/)).toHaveAttribute('readonly')
    expect(screen.getByLabelText(/Currency/)).not.toBeRequired()
    expect(screen.getByText('Bank accounts use GBP.')).toBeInTheDocument()
    expect(screen.getByRole('switch', { name: 'Round-up' })).not.toBeChecked()
    expect(screen.queryByRole('switch', { name: 'Active' })).not.toBeInTheDocument()
  })

  it('shows only the fields the chosen template has, and lists its currencies', () => {
    renderDialog({ templates: [BANK, PLAIN] })

    fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Plain' } })

    expect(screen.queryByLabelText(/Opening balance/)).not.toBeInTheDocument()
    expect(screen.queryByRole('switch', { name: 'Round-up' })).not.toBeInTheDocument()
    expect(within(screen.getByLabelText(/Currency/)).getAllByRole('option').map((option) => option.textContent)).toEqual([
      'GBP',
      'BRL',
    ])
  })

  it('sends the entered values on Add account', async () => {
    const { onSubmit } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: '  Monzo  ' } })
    fireEvent.change(screen.getByLabelText(/Opening balance/), { target: { value: '250.5' } })
    fireEvent.change(screen.getByLabelText(/Balance start date/), { target: { value: '2026-09-01' } })
    fireEvent.click(screen.getByRole('switch', { name: 'Round-up' }))
    save()

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        templateName: 'Bank',
        name: 'Monzo',
        currency: 'GBP',
        currencies: null,
        otherParticipant: null,
        openingBalance: 250.5,
        balanceStartDate: '2026-09-01',
        roundUpEnabled: true,
        cashbackEnabled: null,
        cashbackRatePercent: null,
        categoryId: null,
        savings: null,
        isActive: true,
      }),
    )
  })

  it('sends null capability fields for a template without them', async () => {
    const { onSubmit } = renderDialog({ templates: [PLAIN] })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Wallet' } })
    save()

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ openingBalance: null, balanceStartDate: null, roundUpEnabled: null }),
      ),
    )
  })

  it('shows no Category field for a template without pots', () => {
    renderDialog()

    expect(screen.queryByRole('combobox', { name: /^Bank expense category/ })).not.toBeInTheDocument()
  })

  it('lists the active non-investment categories by name for a Pots account, and sends the chosen one', async () => {
    const { onSubmit } = renderDialog({ templates: [POTS] })

    const select = screen.getByRole('combobox', { name: /^Bank expense category/ })
    expect(select).toBeRequired()
    expect(within(select).getAllByRole('option').map((option) => option.textContent)).toEqual([
      'Select a category',
      'Holidays',
      'Reserva',
    ])
    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Travel' } })
    fireEvent.change(select, { target: { value: 'c2' } })
    save()

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ templateName: 'Pots', name: 'Travel', categoryId: 'c2' })),
    )
  })

  it('says when there is no category to choose for a Pots account', () => {
    renderDialog({ templates: [POTS], categories: [category('c3', 'Investimento', { isInvestment: true })] })

    const select = screen.getByRole('combobox', { name: /^Bank expense category/ })
    expect(select).toBeDisabled()
    expect(within(select).getAllByRole('option').map((option) => option.textContent)).toEqual(['No active categories'])
    expect(screen.getByText('Add a category in Admin → CashFlow → Categories first.')).toBeInTheDocument()
  })

  it('requires a category for a Pots account and focuses it', () => {
    const { onSubmit } = renderDialog({ templates: [POTS] })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Travel' } })
    save()

    expect(screen.getByText("Choose the category for this account's bank expenses.")).toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^Bank expense category/ })).toHaveFocus()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it("keeps a Pots account's inactive category, marked inactive, and sends it on Save", async () => {
    const { onSubmit } = renderDialog({
      account: potsAccount({ id: 'p1', name: 'Reserva', categoryId: 'c4', hasRecords: true }),
      templates: [POTS],
    })

    const select = screen.getByRole('combobox', { name: /^Bank expense category/ })
    expect(select).toHaveValue('c4')
    expect(within(select).getByRole('option', { name: 'Old (inactive)' })).toBeInTheDocument()
    expect(screen.getByText('Changing it applies to later deposits and withdrawals; past ones keep their category.')).toBeInTheDocument()
    save('Save')

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ categoryId: 'c4' })))
  })

  it('[AC P60-F06-admin-accounts-page-07] shows each problem under its field on submit and focuses the error summary listing them', async () => {
    const { onSubmit } = renderDialog()

    expect(screen.queryByText('Account name is required.')).not.toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/Opening balance/), { target: { value: '-1' } })
    fireEvent.change(screen.getByLabelText(/Balance start date/), { target: { value: '2026-10-25' } })
    save()

    expect(screen.getByLabelText(/Name/)).toHaveAccessibleDescription('Account name is required.')
    expect(screen.getByLabelText(/Opening balance/)).toHaveAccessibleDescription('Opening balance cannot be negative.')
    expect(screen.getByLabelText(/Balance start date/)).toHaveAccessibleDescription("Balance start date can't be in the future.")
    expect(screen.getByLabelText(/Name/)).toHaveAttribute('aria-invalid', 'true')
    const summary = screen.getByRole('group', { name: 'Fix 3 fields before saving' })
    await waitFor(() => expect(summary).toHaveFocus())
    expect(within(summary).getAllByRole('button').map((link) => link.textContent)).toEqual([
      'Account name is required.',
      'Opening balance cannot be negative.',
      "Balance start date can't be in the future.",
    ])
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('rejects a name longer than 60 characters and a missing opening balance', () => {
    const { onSubmit } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'x'.repeat(61) } })
    fireEvent.change(screen.getByLabelText(/Opening balance/), { target: { value: '' } })
    save()

    expect(screen.getByLabelText(/Name/)).toHaveAccessibleDescription('Account name must be 60 characters or fewer.')
    expect(screen.getByLabelText(/Opening balance/)).toHaveAccessibleDescription('Opening balance is required.')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('refuses a name of spaces as missing', () => {
    const { onSubmit } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: '   ' } })
    save()

    expect(screen.getByLabelText(/Name/)).toHaveAccessibleDescription('Account name is required.')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('accepts a name of exactly 60 characters', async () => {
    const { onSubmit } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'x'.repeat(60) } })
    save()

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ name: 'x'.repeat(60) })))
  })

  it('sends the currency chosen for a template with several', async () => {
    const { onSubmit } = renderDialog({ templates: [PLAIN] })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Wise' } })
    fireEvent.change(screen.getByLabelText(/Currency/), { target: { value: 'BRL' } })
    save()

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ currency: 'BRL' })))
  })

  it('takes the currency of a one-currency template it switches to', () => {
    renderDialog({ templates: [PLAIN, BANK] })
    fireEvent.change(screen.getByLabelText(/Currency/), { target: { value: 'BRL' } })

    fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Bank' } })

    expect(screen.getByLabelText(/Currency/)).toHaveValue('GBP')
    expect(screen.getByText('Bank accounts use GBP.')).toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-02] shows a duplicate-name 409 under Name, and clears it once the name changes', async () => {
    const message = 'An account named "Barclays" already exists.'
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(duplicateNameRejection(message)) })
    await waitFor(() => expect(screen.getByLabelText(/^Template/)).toHaveFocus())

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'barclays' } })
    saveFromTheSaveButton('Add account')

    await expectDuplicateUnderName(message)
    expect(screen.getByLabelText(/Name/)).toHaveValue('barclays')

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Barclays 2' } })
    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('places a duplicate-name 409 under Name by its code, whatever the message says', async () => {
    const message = 'That account name is taken.'
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(duplicateNameRejection(message)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Barclays' } })
    save()

    expect(await screen.findByText(message)).toBeInTheDocument()
    expect(screen.getByLabelText(/Name/)).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getAllByText(message)).toHaveLength(1)
  })

  it('keeps a 409 without the duplicate-name code in the message bar, even when it reads like a duplicate', async () => {
    const message = 'An account named "Barclays" already exists.'
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError(message, 409)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Barclays' } })
    save()

    expect(await findErrorAlert()).toHaveTextContent(message)
    expect(screen.getByLabelText(/Name/)).not.toHaveAttribute('aria-invalid', 'true')
  })

  it('shows a 409 that is not about the name in the message bar, leaving Name valid', async () => {
    const message = `"Monzo" can't be changed while a statement payment is pending.`
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError(message, 409)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    expect(await findErrorAlert()).toHaveTextContent(message)
    expect(screen.getByLabelText(/Name/)).not.toHaveAttribute('aria-invalid', 'true')
    expect(screen.getAllByText(message)).toHaveLength(1)
  })

  it('[AC P60-F06-admin-accounts-page-07] shows any other server error in the message bar and keeps the entered values', async () => {
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError('Bank accounts must be GBP.', 400)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    expect(await findErrorAlert()).toHaveTextContent('Bank accounts must be GBP.')
    expect(screen.getByLabelText(/Name/)).toHaveValue('Monzo')
    expect(screen.getByRole('button', { name: 'Add account' })).toBeEnabled()
    await waitFor(() => expect(getErrorAlert()).toHaveFocus())
  })

  it('[AC P60-F06-admin-accounts-page-07] disables the fields and the buttons while saving', async () => {
    renderDialog({ onSubmit: () => new Promise(() => undefined) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    await waitFor(() => expect(screen.getByLabelText(/Name/)).toBeDisabled())
    const saving = screen.getByRole('button', { name: 'Saving…' })
    expect(saving).toHaveAttribute('aria-disabled', 'true')
    expect(saving).toHaveFocus()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('names each template by its display name while keeping its name as the value', () => {
    renderDialog({ templates: [BANK, CREDIT_CARD] })

    const template = screen.getByLabelText(/Template/)
    expect(within(template).getAllByRole('option').map((option) => [option.textContent, option.getAttribute('value')])).toEqual([
      ['Bank', 'Bank'],
      ['Credit card', 'CreditCard'],
    ])
    fireEvent.change(template, { target: { value: 'CreditCard' } })
    expect(template).toHaveValue('CreditCard')
    expect(screen.getByText('Credit card accounts use GBP.')).toBeInTheDocument()
  })

  it('shows a card account\'s read-only template by its display name', () => {
    renderDialog({
      account: cardAccount({ id: 'a4', name: 'BA Amex', hasRecords: true }),
      templates: [BANK, CREDIT_CARD],
    })

    expect(screen.getByLabelText(/Template/)).toHaveValue('Credit card')
  })

  it('[AC P60-F06-admin-accounts-page-03] shows template and currency as read-only for an account with records', () => {
    renderDialog({ account: account({ id: 'a1', name: 'Barclays', hasRecords: true }), templates: [BANK, PLAIN] })

    expect(screen.getByRole('dialog', { name: 'Edit account' })).toBeInTheDocument()
    expect(screen.getByLabelText(/Template/)).toHaveAttribute('readonly')
    expect(screen.getByLabelText(/Template/)).not.toBeRequired()
    expect(screen.getByLabelText(/Currency/)).toHaveAttribute('readonly')
    expect(screen.getAllByText("Can't change once the account has transactions.")).toHaveLength(2)
    expect(screen.getByText("Changing it changes this account's past balances.")).toBeInTheDocument()
    expect(screen.getByRole('switch', { name: 'Active' })).toBeChecked()
  })

  it('lets the template change on an account without records', () => {
    renderDialog({ account: account({ id: 'a2', name: 'Monzo' }), templates: [BANK, PLAIN] })

    expect(screen.getByLabelText(/Template/).tagName).toBe('SELECT')
  })

  it('sends the current values and the Active switch on Save', async () => {
    const { onSubmit } = renderDialog({
      account: account({ id: 'a1', name: 'Barclays', openingBalance: 12.3, balanceStartDate: '2026-02-01', roundUpEnabled: true }),
    })

    expect(screen.getByLabelText(/Opening balance/)).toHaveValue(12.3)
    fireEvent.click(screen.getByRole('switch', { name: 'Active' }))
    save('Save')

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        templateName: 'Bank',
        name: 'Barclays',
        currency: 'GBP',
        currencies: null,
        otherParticipant: null,
        openingBalance: 12.3,
        balanceStartDate: '2026-02-01',
        roundUpEnabled: true,
        cashbackEnabled: null,
        cashbackRatePercent: null,
        categoryId: null,
        savings: null,
        isActive: false,
      }),
    )
  })

  it('shows an empty, optional start date for an account created through Admin Banks and keeps that date on save', async () => {
    const { onSubmit } = renderDialog({ account: account({ id: 'a1', name: 'Barclays', balanceStartDate: '0001-01-01' }) })

    expect(screen.getByLabelText(/Balance start date/)).toHaveValue('')
    expect(screen.getByLabelText(/Balance start date/)).not.toBeRequired()
    expect(screen.getByText('Not set. Leave it empty to keep it that way.')).toBeInTheDocument()
    save('Save')

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ balanceStartDate: '0001-01-01' })))
    expect(screen.queryByText('Balance start date is required.')).not.toBeInTheDocument()
  })

  it('requires a start date when editing an account that has one', () => {
    const { onSubmit } = renderDialog({ account: account({ id: 'a1', name: 'Barclays', balanceStartDate: '2026-02-01' }) })

    fireEvent.change(screen.getByLabelText(/Balance start date/), { target: { value: '' } })
    save('Save')

    expect(screen.getByText('Balance start date is required.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('requires a start date on a new account', () => {
    const { onSubmit } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    fireEvent.change(screen.getByLabelText(/Balance start date/), { target: { value: '' } })
    save()

    expect(screen.getByText('Balance start date is required.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('[AC P60-F06-admin-accounts-page-07] closes a clean form without asking', () => {
    const { onCancel } = renderDialog()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalledTimes(1)
    expect(screen.queryByRole('dialog', { name: 'Discard unsaved changes?' })).not.toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-07] asks before discarding changes, and Keep editing keeps them', async () => {
    const { onCancel } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(await screen.findByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Keep editing' }))
    expect(onCancel).not.toHaveBeenCalled()
    expect(screen.getByLabelText(/Name/)).toHaveValue('Monzo')

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Discard' }))
    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('[AC P60-F06-admin-accounts-page-07] sends Escape on a changed form through the discard prompt', async () => {
    const { onCancel } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    fireEvent.keyDown(screen.getByRole('dialog', { name: 'New account' }), { key: 'Escape' })

    expect(await screen.findByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()
    expect(onCancel).not.toHaveBeenCalled()
  })

  describe('Settlement', () => {
    const chooseSettlement = () => fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Settlement' } })
    const currencyBox = (name: string) => screen.getByRole('checkbox', { name })

    it('shows Currencies as checkboxes with none ticked and an Other participant field, in place of Currency', () => {
      renderDialog({ templates: [BANK, SETTLEMENT] })

      chooseSettlement()

      const currencies = screen.getByRole('group', { name: /Currencies/ })
      expect(within(currencies).getAllByRole('checkbox')).toEqual([currencyBox('BRL'), currencyBox('GBP')])
      expect(currencyBox('BRL')).not.toBeChecked()
      expect(currencyBox('GBP')).not.toBeChecked()
      expect(screen.queryByLabelText(/^Currency/)).not.toBeInTheDocument()
      expect(screen.getByLabelText(/Other participant/)).toHaveValue('')
      expect(screen.getByText('The person you settle with, for example Mãe.')).toBeInTheDocument()
      expect(screen.queryByLabelText(/Opening balance/)).not.toBeInTheDocument()
    })

    it('[AC P60-F11-settlement-template-and-mae-migration-05] creates a Settlement account with its currencies and other participant', async () => {
      const { onSubmit } = renderDialog({ templates: [BANK, SETTLEMENT] })

      chooseSettlement()
      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Samuel' } })
      fireEvent.click(currencyBox('GBP'))
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: '  Samuel  ' } })
      save()

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith({
          templateName: 'Settlement',
          name: 'Samuel',
          currency: 'GBP',
          currencies: ['GBP'],
          otherParticipant: 'Samuel',
          openingBalance: null,
          balanceStartDate: null,
          roundUpEnabled: null,
          cashbackEnabled: null,
          cashbackRatePercent: null,
          categoryId: null,
          savings: null,
          isActive: true,
        }),
      )
    })

    it('sends the first ticked currency in code order as the main currency', async () => {
      const { onSubmit } = renderDialog({ templates: [SETTLEMENT] })

      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      fireEvent.click(currencyBox('GBP'))
      fireEvent.click(currencyBox('BRL'))
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: 'Mãe' } })
      save()

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ currency: 'BRL', currencies: ['BRL', 'GBP'] })),
      )
    })

    it('requires a currency and the other participant, listing both in the focused error summary', async () => {
      const { onSubmit } = renderDialog({ templates: [SETTLEMENT] })

      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      save()

      expect(screen.getByRole('group', { name: /Currencies/ })).toHaveAccessibleDescription('Choose at least one currency. Tick at least one.')
      expect(currencyBox('BRL')).toHaveAttribute('aria-invalid', 'true')
      const summary = screen.getByRole('group', { name: 'Fix 2 fields before saving' })
      await waitFor(() => expect(summary).toHaveFocus())
      expect(within(summary).getAllByRole('button').map((link) => link.textContent)).toEqual([
        'Choose at least one currency.',
        "Enter the other participant's name.",
      ])
      fireEvent.click(within(summary).getByRole('button', { name: 'Choose at least one currency.' }))
      expect(currencyBox('BRL')).toHaveFocus()
      expect(onSubmit).not.toHaveBeenCalled()
    })

    it('accepts another participant of exactly 60 characters', async () => {
      const { onSubmit } = renderDialog({ templates: [SETTLEMENT] })

      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      fireEvent.click(currencyBox('BRL'))
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: 'a'.repeat(60) } })
      save()

      await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ otherParticipant: 'a'.repeat(60) })))
    })

    it('focuses the first currency when it is the only problem, and describes only that box with the message', () => {
      renderDialog({ templates: [SETTLEMENT] })
      expect(currencyBox('BRL')).not.toHaveAttribute('aria-describedby')

      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: 'Mãe' } })
      save()

      expect(currencyBox('BRL')).toHaveFocus()
      expect(currencyBox('BRL')).toHaveAccessibleDescription('Choose at least one currency.')
      expect(currencyBox('GBP')).not.toHaveAttribute('aria-describedby')
    })

    it('marks Currencies as required', () => {
      renderDialog({ templates: [SETTLEMENT] })

      const labelId = screen.getByRole('group', { name: /Currencies/ }).getAttribute('aria-labelledby') as string
      expect(document.getElementById(labelId)?.textContent).toContain('*')
    })

    it('keeps the ticked currencies the new template has, and the other participant, when switching between Settlement templates', async () => {
      const { onSubmit } = renderDialog({ templates: [SETTLEMENT, SETTLEMENT_USD] })
      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      fireEvent.click(currencyBox('BRL'))
      fireEvent.click(currencyBox('GBP'))
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: 'Mãe' } })

      fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'SettlementUsd' } })

      expect(currencyBox('GBP')).toBeChecked()
      expect(currencyBox('USD')).not.toBeChecked()
      expect(screen.getByLabelText(/Other participant/)).toHaveValue('Mãe')
      save()
      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ currencies: ['GBP'], otherParticipant: 'Mãe' })),
      )
    })

    it.each([
      ['a'.repeat(61), "The other participant's name must be 60 characters or fewer."],
      [' you ', "The other participant can't be called You."],
    ])('rejects the other participant %j', (otherParticipant, message) => {
      const { onSubmit } = renderDialog({ templates: [SETTLEMENT] })

      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      fireEvent.click(currencyBox('BRL'))
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: otherParticipant } })
      save()

      expect(screen.getByText(message)).toBeInTheDocument()
      expect(screen.getByLabelText(/Other participant/)).toHaveFocus()
      expect(onSubmit).not.toHaveBeenCalled()
    })

    it('edits a two-currency account keeping both currencies ticked and sending them', async () => {
      const { onSubmit } = renderDialog({ account: MAE, templates: [BANK, SETTLEMENT] })

      expect(currencyBox('BRL')).toBeChecked()
      expect(currencyBox('GBP')).toBeChecked()
      expect(screen.getByLabelText(/Other participant/)).toHaveValue('Mãe')
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: 'Mother' } })
      save('Save')

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(
          expect.objectContaining({ currency: 'BRL', currencies: ['BRL', 'GBP'], otherParticipant: 'Mother', isActive: true }),
        ),
      )
    })

    it('shows the currencies as read-only text once the account has records, keeping the other participant editable', () => {
      renderDialog({ account: { ...MAE, hasRecords: true }, templates: [SETTLEMENT] })

      expect(screen.getByLabelText(/Currencies/)).toHaveValue('BRL, GBP')
      expect(screen.getByLabelText(/Currencies/)).toHaveAttribute('readonly')
      expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
      expect(screen.getAllByText("Can't change once the account has transactions.")).toHaveLength(2)
      expect(screen.getByLabelText(/Other participant/)).not.toHaveAttribute('readonly')
    })

    it('describes the Currencies group with its rule before any save', () => {
      renderDialog({ templates: [SETTLEMENT] })

      expect(screen.getByRole('group', { name: /Currencies/ })).toHaveAccessibleDescription('Tick at least one.')
    })

    it('forgets Settlement values when switching to a template without them, so Cancel closes without asking', () => {
      const { onCancel } = renderDialog({ templates: [BANK, SETTLEMENT] })

      chooseSettlement()
      fireEvent.click(currencyBox('GBP'))
      fireEvent.change(screen.getByLabelText(/Other participant/), { target: { value: 'Samuel' } })
      fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Bank' } })
      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onCancel).toHaveBeenCalledTimes(1)
      expect(screen.queryByRole('dialog', { name: 'Discard unsaved changes?' })).not.toBeInTheDocument()
    })

    it('asks before discarding a changed tick, and not after ticking it back', async () => {
      const { onCancel } = renderDialog({ account: MAE, templates: [SETTLEMENT] })

      fireEvent.click(currencyBox('GBP'))
      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
      expect(await screen.findByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()
      fireEvent.click(screen.getByRole('button', { name: 'Keep editing' }))

      fireEvent.click(currencyBox('GBP'))
      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
      expect(onCancel).toHaveBeenCalledTimes(1)
    })
  })

  describe('Cashback', () => {
    const CASHBACK_BANK: AccountTemplateDto = { ...BANK, capabilities: [...BANK.capabilities, 'SupportsCashback'] }
    const renderBank = (props: Partial<Parameters<typeof AccountFormDialog>[0]> = {}) => {
      const dialog = renderDialog({ templates: [CASHBACK_BANK, PLAIN], ...props })
      if (!props.account) fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Chase' } })
      return dialog
    }
    const rateInput = () => screen.getByLabelText(/Cashback rate/)

    it('[AC P65-F03-cashback-web-01] shows the Cashback switch only for a template that supports it, and the rate only while it is on', () => {
      renderBank()

      expect(screen.getByRole('switch', { name: 'Cashback' })).not.toBeChecked()
      expect(screen.queryByLabelText(/Cashback rate/)).not.toBeInTheDocument()
      fireEvent.click(screen.getByRole('switch', { name: 'Cashback' }))
      expect(rateInput()).toHaveAccessibleDescription('Suggests the cashback on each expense paid from this bank.')

      fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Plain' } })
      expect(screen.queryByRole('switch', { name: 'Cashback' })).not.toBeInTheDocument()
    })

    it('[AC P65-F03-cashback-web-01] creates a bank with cashback and its rate', async () => {
      const { onSubmit } = renderBank()

      fireEvent.click(screen.getByRole('switch', { name: 'Cashback' }))
      fireEvent.change(rateInput(), { target: { value: '1.5' } })
      save()

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ cashbackEnabled: true, cashbackRatePercent: 1.5 })),
      )
    })

    it('[AC P65-F03-cashback-web-01] switching cashback off drops the rate and sends it off', async () => {
      const chase = account({ id: 'a1', name: 'Chase', cashbackEnabled: true, cashbackRatePercent: 2 })
      const { onSubmit } = renderBank({ account: chase })

      expect(rateInput()).toHaveValue(2)
      fireEvent.click(screen.getByRole('switch', { name: 'Cashback' }))
      fireEvent.click(screen.getByRole('switch', { name: 'Cashback' }))
      expect(rateInput()).toHaveValue(null)
      fireEvent.click(screen.getByRole('switch', { name: 'Cashback' }))
      save('Save')

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ cashbackEnabled: false, cashbackRatePercent: null })),
      )
    })

    it.each([
      ['', 'Enter the cashback rate.'],
      ['0', 'Cashback rate must be more than 0% and at most 100%.'],
      ['100.5', 'Cashback rate must be more than 0% and at most 100%.'],
      ['1.555', 'Cashback rate can have at most 2 decimal places.'],
    ])('[AC P65-F03-cashback-web-03] refuses the rate "%s" inline and focuses it', async (rate, message) => {
      const { onSubmit } = renderBank()

      fireEvent.click(screen.getByRole('switch', { name: 'Cashback' }))
      fireEvent.change(rateInput(), { target: { value: rate } })
      save()

      expect(rateInput()).toHaveAccessibleDescription(expect.stringContaining(message))
      await waitFor(() => expect(rateInput()).toHaveFocus())
      expect(onSubmit).not.toHaveBeenCalled()
    })

    it('asks before closing once the rate changed', () => {
      const chase = account({ id: 'a1', name: 'Chase', cashbackEnabled: true, cashbackRatePercent: 2 })
      const { onCancel } = renderBank({ account: chase })

      fireEvent.change(rateInput(), { target: { value: '3' } })
      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onCancel).not.toHaveBeenCalled()
    })
  })

  describe('Savings', () => {
    const renderSavings = () => {
      const dialog = renderDialog({ templates: [BANK, SAVINGS] })
      fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Savings' } })
      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Chase Save' } })
      return dialog
    }
    const rateRow = (number: number) => screen.getByRole('group', { name: `Rate ${number}` })
    const rateInput = (number: number) => within(rateRow(number)).getByLabelText(/Annual rate/)
    const dateInput = (number: number) => within(rateRow(number)).getByLabelText(/Effective from/)
    const removeRate = (number: number) => screen.getByRole('button', { name: `Remove rate ${number}` })

    it('[AC P64-F04-savings-web-01] shows the Savings settings only for the Savings template', () => {
      renderDialog({ templates: [BANK, SAVINGS] })

      expect(screen.queryByLabelText(/Interest paid/)).not.toBeInTheDocument()
      expect(screen.queryByRole('group', { name: 'Rates' })).not.toBeInTheDocument()

      fireEvent.change(screen.getByLabelText(/Template/), { target: { value: 'Savings' } })

      expect(screen.getByLabelText(/Interest paid/)).toHaveValue('Monthly')
      expect(screen.getByLabelText(/Payment day/)).toHaveValue(null)
      expect(screen.getByRole('switch', { name: 'Tax-free' })).not.toBeChecked()
      expect(screen.getByRole('group', { name: 'Rates' })).toHaveAccessibleDescription(/Keep at least one rate\./)
      expect(rateInput(1)).toHaveValue(null)
      expect(dateInput(1)).toHaveValue('2026-10-24')
      expect(screen.queryByRole('switch', { name: 'Round-up' })).not.toBeInTheDocument()
    })

    it('[AC P64-F04-savings-web-01] creates a Savings account with its interest settings and rates', async () => {
      const { onSubmit } = renderSavings()

      fireEvent.change(screen.getByLabelText(/Payment day/), { target: { value: '5' } })
      fireEvent.click(screen.getByRole('switch', { name: 'Tax-free' }))
      fireEvent.change(rateInput(1), { target: { value: '4.5' } })
      fireEvent.change(dateInput(1), { target: { value: '2026-09-01' } })
      fireEvent.click(screen.getByRole('button', { name: 'Add rate' }))
      fireEvent.change(rateInput(2), { target: { value: '3.75' } })
      fireEvent.change(dateInput(2), { target: { value: '2027-01-01' } })
      save()

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            templateName: 'Savings',
            name: 'Chase Save',
            roundUpEnabled: null,
            cashbackEnabled: null,
            cashbackRatePercent: null,
            savings: {
              interestFrequency: 'Monthly',
              paymentDay: 5,
              isTaxFree: true,
              rates: [
                { annualPercent: 4.5, effectiveFrom: '2026-09-01' },
                { annualPercent: 3.75, effectiveFrom: '2027-01-01' },
              ],
            },
          }),
        ),
      )
    })

    it('[AC P64-F04-savings-web-01] hides Payment day for Daily and drops its value', async () => {
      const { onSubmit } = renderSavings()

      fireEvent.change(screen.getByLabelText(/Payment day/), { target: { value: '5' } })
      fireEvent.change(screen.getByLabelText(/Interest paid/), { target: { value: 'Daily' } })
      expect(screen.queryByLabelText(/Payment day/)).not.toBeInTheDocument()
      fireEvent.change(rateInput(1), { target: { value: '3' } })
      save()

      await waitFor(() =>
        expect(onSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            savings: expect.objectContaining({ interestFrequency: 'Daily', paymentDay: null }),
          }),
        ),
      )
      fireEvent.change(screen.getByLabelText(/Interest paid/), { target: { value: 'Monthly' } })
      expect(screen.getByLabelText(/Payment day/)).toHaveValue(null)
    })

    it('[AC P64-F04-savings-web-02] adds a rate row with focus in it, and removes rows but never the last one', async () => {
      renderSavings()

      expect(removeRate(1)).toHaveAttribute('aria-disabled', 'true')
      fireEvent.click(removeRate(1))
      expect(rateRow(1)).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: 'Add rate' }))
      await waitFor(() => expect(rateInput(2)).toHaveFocus())
      expect(removeRate(1)).not.toHaveAttribute('aria-disabled', 'true')

      fireEvent.click(removeRate(2))
      expect(screen.queryByRole('group', { name: 'Rate 2' })).not.toBeInTheDocument()
      expect(removeRate(1)).toHaveAttribute('aria-disabled', 'true')
      await waitFor(() => expect(screen.getByRole('button', { name: 'Add rate' })).toHaveFocus())
    })

    it('[AC P64-F04-savings-web-02] shows each invalid setting under its field and focuses the error summary listing them', async () => {
      const { onSubmit } = renderSavings()

      fireEvent.change(screen.getByLabelText(/Payment day/), { target: { value: '32' } })
      fireEvent.change(rateInput(1), { target: { value: '4.567' } })
      fireEvent.click(screen.getByRole('button', { name: 'Add rate' }))
      fireEvent.change(rateInput(2), { target: { value: '101' } })
      save()

      expect(rateInput(1)).toHaveAccessibleDescription('Interest rate can have at most 2 decimal places.')
      expect(dateInput(2)).toHaveAccessibleDescription("Two rates can't take effect on the same date.")
      const summary = screen.getByRole('group', { name: 'Fix 4 fields before saving' })
      await waitFor(() => expect(summary).toHaveFocus())
      expect(onSubmit).not.toHaveBeenCalled()
    })

    it('[AC P64-F04-savings-web-02] focuses the only invalid rate', async () => {
      const { onSubmit } = renderSavings()

      fireEvent.change(screen.getByLabelText(/Payment day/), { target: { value: '5' } })
      save()

      expect(rateInput(1)).toHaveAccessibleDescription('Enter the rate as a number, for example 4.50.')
      await waitFor(() => expect(rateInput(1)).toHaveFocus())
      expect(onSubmit).not.toHaveBeenCalled()
    })

    it('opens a Savings account with its terms, and closes without asking once a removed rate is re-entered', () => {
      const chaseSave = savingsAccount({ id: 's1', name: 'Chase Save' })
      const { onCancel } = renderDialog({ account: chaseSave, templates: [BANK, SAVINGS] })

      expect(screen.getByLabelText(/Payment day/)).toHaveValue(5)
      expect(rateInput(1)).toHaveValue(4.5)
      expect(dateInput(1)).toHaveValue('2026-09-01')
      fireEvent.click(screen.getByRole('button', { name: 'Add rate' }))
      fireEvent.click(removeRate(1))
      fireEvent.change(rateInput(1), { target: { value: '4.50' } })
      fireEvent.change(dateInput(1), { target: { value: '2026-09-01' } })
      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

      expect(onCancel).toHaveBeenCalledOnce()
    })
  })
})
