import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { AccountTemplateDto, CategoryDto } from '../../api/types'
import { account, cardAccount, potsAccount, settlementAccount } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import AccountFormDialog from '../AccountFormDialog'

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
const MAE = settlementAccount({ id: 'm1', name: 'Mãe' })

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
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date('2026-10-24T12:00:00'))
  })

  afterEach(() => {
    vi.useRealTimers()
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
        categoryId: null,
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
    expect(screen.getByText('Add a category in Admin → Categories first.')).toBeInTheDocument()
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

  it('[AC P60-F06-admin-accounts-page-07] shows each problem under its field on submit and focuses the first one', () => {
    const { onSubmit } = renderDialog()

    expect(screen.queryByText('Account name is required.')).not.toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/Opening balance/), { target: { value: '-1' } })
    fireEvent.change(screen.getByLabelText(/Balance start date/), { target: { value: '2026-10-25' } })
    save()

    expect(screen.getByText('Account name is required.')).toBeInTheDocument()
    expect(screen.getByText('Opening balance cannot be negative.')).toBeInTheDocument()
    expect(screen.getByText("Balance start date can't be in the future.")).toBeInTheDocument()
    expect(screen.getByLabelText(/Name/)).toHaveFocus()
    expect(screen.getByLabelText(/Name/)).toHaveAttribute('aria-invalid', 'true')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('rejects a name longer than 60 characters and a missing opening balance', () => {
    const { onSubmit } = renderDialog()

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'x'.repeat(61) } })
    fireEvent.change(screen.getByLabelText(/Opening balance/), { target: { value: '' } })
    save()

    expect(screen.getByText('Account name must be 60 characters or fewer.')).toBeInTheDocument()
    expect(screen.getByText('Opening balance is required.')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('[AC P60-F06-admin-accounts-page-02] shows a duplicate-name 409 under Name, and clears it once the name changes', async () => {
    const message = 'An account named "Barclays" already exists.'
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError(message, 409)) })
    await waitFor(() => expect(screen.getByRole('dialog')).toHaveFocus())

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'barclays' } })
    save()

    expect(await screen.findByText(message)).toBeInTheDocument()
    expect(screen.getByLabelText(/Name/)).toHaveAttribute('aria-invalid', 'true')
    await waitFor(() => expect(screen.getByLabelText(/Name/)).toHaveFocus())
    expect(screen.getAllByText(message)).toHaveLength(1)
    expect(screen.getByLabelText(/Name/)).toHaveValue('barclays')

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Barclays 2' } })
    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('shows a 409 that is not about the name in the message bar, leaving Name valid', async () => {
    const message = "Monzo can't be changed while a statement payment is pending."
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError(message, 409)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(screen.getByLabelText(/Name/)).not.toHaveAttribute('aria-invalid', 'true')
    expect(screen.getAllByText(message)).toHaveLength(1)
  })

  it("shows the server's template-and-currency refusal in the message bar", async () => {
    const message = "Template and currency can't change once the account has transactions."
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError(message, 400)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    expect(await screen.findByRole('alert')).toHaveTextContent(message)
    expect(screen.getByLabelText(/Name/)).not.toHaveAttribute('aria-invalid', 'true')
  })

  it('[AC P60-F06-admin-accounts-page-07] shows any other server error in the message bar and keeps the entered values', async () => {
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError('Bank accounts must be GBP.', 400)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    expect(await screen.findByRole('alert')).toHaveTextContent('Bank accounts must be GBP.')
    expect(screen.getByLabelText(/Name/)).toHaveValue('Monzo')
    expect(screen.getByRole('button', { name: 'Add account' })).toBeEnabled()
    await waitFor(() => expect(screen.getByRole('alert')).toHaveFocus())
  })

  it('[AC P60-F06-admin-accounts-page-07] disables the fields and the buttons while saving', async () => {
    renderDialog({ onSubmit: () => new Promise(() => undefined) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    await waitFor(() => expect(screen.getByLabelText(/Name/)).toBeDisabled())
    expect(screen.getByRole('button', { name: 'Add account' })).toBeDisabled()
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
        categoryId: null,
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
          categoryId: null,
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

    it('requires a currency and the other participant, focusing the currencies first', () => {
      const { onSubmit } = renderDialog({ templates: [SETTLEMENT] })

      fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Mãe' } })
      save()

      expect(screen.getByText('Choose at least one currency.')).toBeInTheDocument()
      expect(screen.getByRole('group', { name: /Currencies/ })).toHaveAccessibleDescription('Tick at least one. Choose at least one currency.')
      expect(screen.getByText("Enter the other participant's name.")).toBeInTheDocument()
      expect(currencyBox('BRL')).toHaveFocus()
      expect(onSubmit).not.toHaveBeenCalled()
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
})
