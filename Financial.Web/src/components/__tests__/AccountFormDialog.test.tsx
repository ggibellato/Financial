import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { AccountTemplateDto } from '../../api/types'
import { account, cardAccount } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import AccountFormDialog from '../AccountFormDialog'

const BANK: AccountTemplateDto = {
  name: 'Bank',
  currencies: ['GBP'],
  capabilities: ['HasOpeningBalance', 'ReceivesIncome', 'SupportsRoundUp'],
}
const PLAIN: AccountTemplateDto = { name: 'Plain', currencies: ['GBP', 'BRL'], capabilities: [] }
const CREDIT_CARD: AccountTemplateDto = { name: 'CreditCard', currencies: ['GBP'], capabilities: ['HasStatements', 'HasCategories', 'ReceivesPayments'] }

function renderDialog(props: Partial<Parameters<typeof AccountFormDialog>[0]> = {}) {
  const onSubmit = vi.fn<(values: unknown) => Promise<unknown>>().mockResolvedValue(undefined)
  const onCancel = vi.fn()
  render(<AccountFormDialog account={null} templates={[BANK]} onCancel={onCancel} onSubmit={onSubmit} {...props} />)
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
        openingBalance: 250.5,
        balanceStartDate: '2026-09-01',
        roundUpEnabled: true,
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
    expect(screen.getByLabelText(/Name/)).toHaveFocus()
    expect(screen.getAllByText(message)).toHaveLength(1)
    expect(screen.getByLabelText(/Name/)).toHaveValue('barclays')

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Barclays 2' } })
    expect(screen.queryByText(message)).not.toBeInTheDocument()
  })

  it('[AC P60-F06-admin-accounts-page-07] shows any other server error in the message bar and keeps the entered values', async () => {
    renderDialog({ onSubmit: vi.fn().mockRejectedValue(new ApiError('Bank accounts must be GBP.', 400)) })

    fireEvent.change(screen.getByLabelText(/Name/), { target: { value: 'Monzo' } })
    save()

    expect(await screen.findByText('Bank accounts must be GBP.')).toBeInTheDocument()
    expect(screen.getByLabelText(/Name/)).toHaveValue('Monzo')
    expect(screen.getByRole('button', { name: 'Add account' })).toBeEnabled()
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
        openingBalance: 12.3,
        balanceStartDate: '2026-02-01',
        roundUpEnabled: true,
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
})
