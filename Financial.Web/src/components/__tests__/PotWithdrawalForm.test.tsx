import { useEffect } from 'react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, overdraftBalance } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDto, AccountStatementDto, BankOption, CategoryDto, PotOperationDto, PotsSummaryDto } from '../../api/types'
import { overdraftMessage, usePotWithdrawalForm } from '../../hooks/usePotWithdrawalForm'
import { account, cardAccount, potsAccount } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import PotWithdrawalForm from '../PotWithdrawalForm'
import { findErrorAlert } from '../../test-utils/errorAlert'
import { optionNames } from '../../test-utils/optionNames'

const { createPotWithdrawalMock, updatePotWithdrawalMock, getPotsMock, getAccountStatementsMock } = vi.hoisted(() => ({
  createPotWithdrawalMock: vi.fn<FinancialApiClient['createPotWithdrawal']>(),
  updatePotWithdrawalMock: vi.fn<FinancialApiClient['updatePotWithdrawal']>(),
  getPotsMock: vi.fn<FinancialApiClient['getPots']>(),
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createPotWithdrawal: createPotWithdrawalMock,
    updatePotWithdrawal: updatePotWithdrawalMock,
    getPots: getPotsMock,
    getAccountStatements: getAccountStatementsMock,
  } as Partial<FinancialApiClient>,
}))

const POTS: PotsSummaryDto = {
  potsAccountId: 'reserva',
  currency: 'GBP',
  total: 100,
  pots: [
    { id: 'pot-a', name: 'Ariana', splitPercentage: 50, isActive: true, balance: 50 },
    { id: 'pot-b', name: 'Casa', splitPercentage: 50, isActive: true, balance: 50 },
    { id: 'pot-old', name: 'Old car', splitPercentage: 0, isActive: false, balance: 0 },
  ],
}

const HOLIDAY_POTS: PotsSummaryDto = {
  potsAccountId: 'holidays',
  currency: 'GBP',
  total: 5,
  pots: [{ id: 'pot-h', name: 'Summer', splitPercentage: 100, isActive: true, balance: 5 }],
}

const ACCOUNTS: AccountDto[] = [
  account({ id: 'b1', name: 'Barclays', categoryId: 'cat-bank' }),
  potsAccount({ id: 'holidays', name: 'Holidays', categoryId: 'cat-holidays' }),
  potsAccount({ id: 'reserva', name: 'Reserva', categoryId: 'cat-reserva' }),
  potsAccount({ id: 'closed', name: 'Closed', isActive: false }),
  cardAccount({ id: 'amex', name: 'Amex' }),
  cardAccount({ id: 'old-card', name: 'Old card', isActive: false }),
]

const NO_CARDS = ACCOUNTS.filter((candidate) => !candidate.capabilities.includes('HasStatements'))

const paidStatement = (year: number, month: number): AccountStatementDto => ({
  id: `s-${year}-${month}`,
  year,
  month,
  total: 10,
  creditIn: 0,
  outstandingTotal: 0,
  creditCarried: 0,
  status: 'Paid',
  isPaid: true, settledOn: null,
  dueDate: null,
  paidOn: '2026-11-03',
  paidAmount: 10,
  paidFromAccountName: 'Barclays',
  potLinkedTotal: null, potWithdrawals: [],
})

const BANKS: BankOption[] = [
  { id: 'b1', name: 'Barclays', roundUpEnabled: false, isActive: true },
  { id: 'b9', name: 'Old bank', roundUpEnabled: false, isActive: false },
]

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
  category('cat-ariana', 'Ariana'),
  category('cat-casa', 'Casa'),
  category('cat-saude', 'Saude'),
  category('cat-reserva', 'Reserva'),
  category('cat-holidays', 'Holidays'),
  category('cat-bank', 'Bank costs'),
  category('cat-inv', 'Investimento', { isInvestment: true }),
  category('cat-off', 'Off', { active: false }),
]

const OPERATION: PotOperationDto = {
  operationId: 'op1',
  kind: 'Withdrawal',
  potsAccountId: 'reserva',
  date: '2026-10-12',
  description: 'Boots',
  amount: 38.25,
  into: null,
  incomeId: null,
  creditCardId: null,
  statementYear: null,
  statementMonth: null,
  investmentWithdrawal: null,
  investmentWithdrawalBankId: null,
  investmentWithdrawalDate: null,
  roundUpAmount: null,
  potId: 'pot-b',
  bankId: 'b1',
  categoryId: 'cat-saude',
  fundingLines: [],
}

interface HarnessProps {
  mode?: 'pots' | 'bank' | 'card'
  edit?: PotOperationDto
  accounts?: AccountDto[]
  banks?: BankOption[]
  onSaved?: () => void
  onDelete?: () => void
  onCancel?: () => void
}

function Harness({ mode = 'pots', edit, accounts = ACCOUNTS, banks = BANKS, onSaved = vi.fn(), onDelete = vi.fn(), onCancel = vi.fn() }: HarnessProps) {
  const form = usePotWithdrawalForm(accounts, CATEGORIES, { year: 2026, month: 10 }, onSaved)
  const { openEdit, openFromBank, openFromCard, openFromPotsAccount } = form

  useEffect(() => {
    if (edit) openEdit(edit)
    else if (mode === 'bank') openFromBank('b1')
    else if (mode === 'card') openFromCard('amex')
    else openFromPotsAccount('reserva')
    // eslint-disable-next-line react-hooks/exhaustive-deps -- opens once, as the action button does
  }, [])

  return (
    <>
      {form.isOpen && (
        <PotWithdrawalForm
          form={form}
          banks={banks}
          lockedBank={mode === 'bank' ? { id: 'b1', name: 'Barclays' } : undefined}
          lockedCard={mode === 'card' ? { id: 'amex', name: 'Amex' } : undefined}
          onDelete={onDelete}
          onCancel={onCancel}
        />
      )}
      <p>{form.isDirty ? 'dirty' : 'clean'}</p>
    </>
  )
}

const setValue = (label: RegExp, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })
const throughBank = () => screen.getByRole('combobox', { name: /^Bank/ })
const choosePaidWith = (name: 'None' | 'Bank' | 'Credit card') => fireEvent.click(screen.getByRole('radio', { name }))
const setThroughBank = (value: string) => {
  if (value === '') {
    choosePaidWith('None')
    return
  }
  if (!screen.queryByRole('combobox', { name: /^Bank/ })) choosePaidWith('Bank')
  fireEvent.change(throughBank(), { target: { value } })
}
const potSelect = () => screen.getByLabelText(/^Pot\b/)
const potsLoaded = () => waitFor(() => expect(screen.getByRole('button', { name: /^(Add withdrawal|Withdraw from pots|Save)$/ })).toBeEnabled())
const save = (name = 'Add withdrawal') => fireEvent.click(screen.getByRole('button', { name }))

function fillWithdrawal() {
  setValue(/^Description/, 'Boots')
  setValue(/^Amount/, '38.25')
}

function overdraftError(balance = 25.85) {
  return new ApiError(`This withdrawal exceeds Ariana's balance of ${balance}. Set confirmed=true to proceed.`, 409, {
    code: 'overdraftConfirmationRequired',
    balance,
  })
}

const findDialog = () =>
  waitFor(() => {
    const dialog = screen.getAllByRole('dialog', { hidden: true }).find((candidate) => candidate.querySelector('h2')?.textContent === 'Pot goes below zero')
    expect(dialog).toBeDefined()
    return dialog as HTMLElement
  })

beforeEach(() => {
  pinDate('2026-10-15T12:00:00+01:00')
  createPotWithdrawalMock.mockReset().mockResolvedValue(OPERATION)
  updatePotWithdrawalMock.mockReset().mockResolvedValue(OPERATION)
  getPotsMock.mockReset().mockImplementation((id) => Promise.resolve(id === 'holidays' ? HOLIDAY_POTS : POTS))
  getAccountStatementsMock.mockReset().mockResolvedValue([paidStatement(2026, 11)])
  sessionStorage.clear()
})

describe('overdraftMessage', () => {
  it('states how far below zero the withdrawal takes the pot', () => {
    expect(overdraftMessage(25.85, 'Ariana', 38.25, 'GBP')).toBe('This takes Ariana below zero (−£12.40). Continue?')
  })

  it('counts a pot already below zero', () => {
    expect(overdraftMessage(-5, 'Ariana', 10, 'GBP')).toBe('This takes Ariana below zero (−£15.00). Continue?')
  })

  it('still asks when there is no figure', () => {
    expect(overdraftMessage(null, 'Ariana', 10, 'GBP')).toBe('This takes Ariana below zero. Continue?')
  })
})

describe('overdraftBalance', () => {
  it("reads the balance of the server's overdraft code", () => {
    expect(overdraftBalance(overdraftError(25.85))).toBe(25.85)
    expect(overdraftBalance(overdraftError(0))).toBe(0)
  })

  it('has no balance without the code, whatever the text says', () => {
    expect(overdraftBalance(new ApiError("exceeds Ariana's balance of 25.85. Set confirmed=true", 409))).toBeNull()
    expect(overdraftBalance(new ApiError('Conflict', 409, { code: 'other', balance: 5 }))).toBeNull()
  })
})

describe('PotWithdrawalForm from the Pots account', () => {
  it('picks the first active pot, lists every pot with the inactive one marked, and starts with no bank', async () => {
    render(<Harness />)
    await potsLoaded()

    expect(screen.getByRole('heading', { name: 'New withdrawal' })).toBeInTheDocument()
    expect(potSelect()).toHaveValue('pot-a')
    expect(within(potSelect()).getAllByRole('option').map((option) => option.textContent)).toEqual(['Ariana', 'Casa', 'Old car (inactive)'])
    expect(screen.getByRole('radio', { name: 'None' })).toBeChecked()
    expect(screen.queryByRole('combobox', { name: /^Bank/ })).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/^Category/)).not.toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-15')
    expect(screen.getByLabelText(/^Amount \(GBP\)/)).toBeInTheDocument()
    choosePaidWith('Bank')
    expect(throughBank()).toHaveValue('')
    expect(within(throughBank()).queryByText('Old bank (inactive)')).not.toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-05] without a bank sends neither bank nor category', async () => {
    const onSaved = vi.fn()
    render(<Harness onSaved={onSaved} />)
    await potsLoaded()
    fillWithdrawal()

    save()

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock).toHaveBeenCalledWith('reserva', {
      potId: 'pot-a',
      amount: 38.25,
      date: '2026-10-15',
      description: 'Boots',
      bankId: null,
      categoryId: null,
      creditCardId: null,
      statementYear: null,
      statementMonth: null,
      roundUpAmount: null,
      confirmed: false,
    })
  })

  it("defaults the category to the pot's name, excluding inactive, investment and any account's category, and follows the pot until one is chosen", async () => {
    render(<Harness />)
    await potsLoaded()

    setThroughBank('b1')

    const categorySelect = screen.getByLabelText(/^Category/)
    expect(categorySelect).toHaveValue('cat-ariana')
    expect(within(categorySelect).getAllByRole('option').map((option) => option.textContent)).toEqual(['Select a category', 'Ariana', 'Casa', 'Saude'])
    fireEvent.change(potSelect(), { target: { value: 'pot-b' } })
    expect(categorySelect).toHaveValue('cat-casa')

    setValue(/^Category/, 'cat-saude')
    fireEvent.change(potSelect(), { target: { value: 'pot-a' } })
    expect(screen.getByLabelText(/^Category/)).toHaveValue('cat-saude')
    expect(screen.getByText('dirty')).toBeInTheDocument()
  })

  it('requires a category with a bank, and clears it when the bank goes', async () => {
    getPotsMock.mockResolvedValue({ ...POTS, pots: [{ ...POTS.pots[0], name: 'Nothing like a category' }] })
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()
    setThroughBank('b1')

    save()

    expect(await screen.findByText('Category is required when a bank is selected.')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Category/)).toHaveFocus()
    expect(optionNames(screen.getByLabelText(/^Category/))).toEqual(['Select a category', 'Ariana', 'Casa', 'Saude'])
    expect(createPotWithdrawalMock).not.toHaveBeenCalled()

    setThroughBank('')
    expect(screen.queryByLabelText(/^Category/)).not.toBeInTheDocument()
  })

  it('marks the other invalid fields and sends nothing', async () => {
    getPotsMock.mockResolvedValue({ ...POTS, pots: [] })
    render(<Harness />)
    await potsLoaded()
    setValue(/^Date/, '2026-10-16')

    save()

    expect(await screen.findByText('Choose a pot.')).toBeInTheDocument()
    expect(screen.getByText('This account has no pots. Add one with Manage pots.')).toBeInTheDocument()
    expect(screen.getByText('Date must not be in the future.')).toBeInTheDocument()
    expect(screen.getByText('Enter a description.')).toBeInTheDocument()
    expect(screen.getByText('Amount must be greater than zero.')).toBeInTheDocument()

    setValue(/^Date/, '')
    save()
    expect(await screen.findByText('Enter the withdrawal date.')).toBeInTheDocument()
    expect(createPotWithdrawalMock).not.toHaveBeenCalled()
  })

  it('asks without a figure on a 409 without the code', async () => {
    createPotWithdrawalMock.mockRejectedValueOnce(new ApiError('Conflict', 409))
    render(<Harness onSaved={vi.fn()} />)
    await potsLoaded()
    fillWithdrawal()

    save()

    expect(await findDialog()).toHaveTextContent('This takes Ariana below zero. Continue?')
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-06] asks on an overdraft and resends confirmed on Continue', async () => {
    const onSaved = vi.fn()
    createPotWithdrawalMock.mockRejectedValueOnce(overdraftError())
    render(<Harness onSaved={onSaved} />)
    await potsLoaded()
    fillWithdrawal()

    save()

    const dialog = await findDialog()
    expect(dialog).toHaveTextContent('This takes Ariana below zero (−£12.40). Continue?')
    expect(onSaved).not.toHaveBeenCalled()

    fireEvent.click(within(dialog).getByText('Continue', { selector: 'button' }))

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock).toHaveBeenCalledTimes(2)
    expect(createPotWithdrawalMock.mock.calls[0][1].confirmed).toBe(false)
    expect(createPotWithdrawalMock.mock.calls[1][1].confirmed).toBe(true)
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-06] saves nothing on Cancel, keeps the values and returns focus to Save', async () => {
    createPotWithdrawalMock.mockRejectedValueOnce(overdraftError())
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()
    save()

    fireEvent.click(within(await findDialog()).getByText('Cancel', { selector: 'button' }))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    expect(createPotWithdrawalMock).toHaveBeenCalledOnce()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Boots')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add withdrawal' })).toHaveFocus())
  })

  it('returns focus to Save after Cancel when the save was submitted from a field', async () => {
    createPotWithdrawalMock.mockRejectedValueOnce(overdraftError())
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()
    const user = userEvent.setup()
    await user.click(screen.getByLabelText(/^Description/))
    await user.keyboard('{Enter}')

    fireEvent.click(within(await findDialog()).getByText('Cancel', { selector: 'button' }))

    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add withdrawal' })).toHaveFocus())
  })

  it('closes the dialog and shows the error when the confirmed save fails', async () => {
    createPotWithdrawalMock.mockRejectedValueOnce(overdraftError()).mockRejectedValueOnce(new ApiError('Barclays is inactive.', 400))
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()
    save()

    fireEvent.click(within(await findDialog()).getByText('Continue', { selector: 'button' }))

    const alert = await findErrorAlert()
    expect(alert).toHaveTextContent('Barclays is inactive.')
    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
    await waitFor(() => expect(alert).toHaveFocus())
  })

  it('shows a server refusal in an alert and moves focus to it', async () => {
    createPotWithdrawalMock.mockRejectedValue(new ApiError("Category 'Saude' cannot be used for a withdrawal expense.", 400))
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()

    save()

    const alert = await findErrorAlert()
    expect(alert).toHaveTextContent("Category 'Saude' cannot be used")
    await waitFor(() => expect(alert).toHaveFocus())
  })

  it('labels the amount without a currency until the pots arrive', () => {
    getPotsMock.mockReturnValue(new Promise(() => undefined))
    render(<Harness />)

    expect(screen.getByRole('spinbutton', { name: /^Amount\W*$/ })).toBeInTheDocument()
  })

  it('submits on Enter without the browser submitting the page', async () => {
    render(<Harness />)
    await potsLoaded()

    expect(fireEvent.submit(screen.getByRole('form', { name: 'New withdrawal' }))).toBe(false)
  })

  it('offers a retry when the pots fail, with Save disabled', async () => {
    getPotsMock.mockRejectedValueOnce(new Error('offline'))
    render(<Harness />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load the pots')
    expect(screen.getByRole('button', { name: 'Add withdrawal' })).toBeDisabled()
    expect(within(potSelect()).getByRole('option')).toHaveTextContent('Select a pot')
    expect(screen.queryByText('Loading pots…')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    await potsLoaded()
    expect(screen.getByRole('button', { name: 'Add withdrawal' })).toBeEnabled()
  })
})

describe('PotWithdrawalForm from a bank', () => {
  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-07] locks the bank, chooses the active Pots account and pot, and sends the same request as the Pots account', async () => {
    const onSaved = vi.fn()
    render(<Harness mode="bank" onSaved={onSaved} />)
    await potsLoaded()

    expect(screen.getByRole('heading', { name: 'Withdraw from pots' })).toBeInTheDocument()
    expect(screen.getByText('Barclays')).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^Bank/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument()
    const potsAccountSelect = screen.getByLabelText(/^Pots account/)
    expect(within(potsAccountSelect).getAllByRole('option').map((option) => option.textContent)).toEqual(['Holidays', 'Reserva'])

    fireEvent.change(potsAccountSelect, { target: { value: 'reserva' } })
    await waitFor(() => expect(potSelect()).toHaveValue('pot-a'))
    expect(screen.getByLabelText(/^Category/)).toHaveValue('cat-ariana')
    fillWithdrawal()
    save('Withdraw from pots')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock).toHaveBeenCalledWith('reserva', {
      potId: 'pot-a',
      amount: 38.25,
      date: '2026-10-15',
      description: 'Boots',
      bankId: 'b1',
      categoryId: 'cat-ariana',
      creditCardId: null,
      statementYear: null,
      statementMonth: null,
      roundUpAmount: null,
      confirmed: false,
    })
  })

  it('reads the pots of the Pots account it opens on', async () => {
    render(<Harness mode="bank" />)

    await waitFor(() => expect(potSelect()).toHaveValue('pot-h'))
    expect(getPotsMock).toHaveBeenCalledWith('holidays', 2026, 10)
  })

  it('says to create a Pots account when there is none', () => {
    const onCancel = vi.fn()
    render(<Harness mode="bank" accounts={[ACCOUNTS[0]]} onCancel={onCancel} />)

    expect(screen.getByRole('status')).toHaveTextContent('Create a Pots account first.')
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalledOnce()
  })
})

describe('PotWithdrawalForm editing an operation', () => {
  it('opens filled from the operation with Delete, and sends the edit to its route', async () => {
    const onSaved = vi.fn()
    const onDelete = vi.fn()
    render(<Harness edit={OPERATION} onSaved={onSaved} onDelete={onDelete} />)
    await potsLoaded()

    expect(screen.getByRole('heading', { name: 'Edit withdrawal' })).toBeInTheDocument()
    expect(potSelect()).toHaveValue('pot-b')
    expect(throughBank()).toHaveValue('b1')
    expect(screen.getByLabelText(/^Category/)).toHaveValue('cat-saude')
    expect(screen.getByText('clean')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Delete withdrawal' }))
    expect(onDelete).toHaveBeenCalledOnce()

    setValue(/^Amount/, '40')
    save('Save')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updatePotWithdrawalMock).toHaveBeenCalledWith('reserva', 'op1', {
      potId: 'pot-b',
      amount: 40,
      date: '2026-10-12',
      description: 'Boots',
      bankId: 'b1',
      categoryId: 'cat-saude',
      creditCardId: null,
      statementYear: null,
      statementMonth: null,
      roundUpAmount: null,
      confirmed: false,
    })
  })

  it('keeps Save focusable and disables Cancel and Delete while saving', async () => {
    updatePotWithdrawalMock.mockReturnValue(new Promise(() => undefined))
    render(<Harness edit={OPERATION} />)
    await potsLoaded()
    const saveButton = screen.getByRole('button', { name: 'Save' })
    saveButton.focus()

    save('Save')

    expect(await screen.findByRole('button', { name: 'Saving…' })).toHaveAttribute('aria-disabled', 'true')
    expect(saveButton).toHaveFocus()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Delete withdrawal' })).toBeDisabled()
    save('Saving…')
    expect(updatePotWithdrawalMock).toHaveBeenCalledOnce()
  })

  it('keeps a saved bank and category that are no longer offered, so the server can say why', async () => {
    render(<Harness edit={{ ...OPERATION, bankId: 'b9', categoryId: 'cat-off' }} />)
    await potsLoaded()

    expect(throughBank()).toHaveValue('b9')
    expect(within(throughBank()).getByText('Old bank (inactive)')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Category/)).toHaveValue('cat-off')
    expect(within(screen.getByLabelText(/^Category/)).getByText('Saved category (no longer available)')).toBeInTheDocument()
  })
})

const CARD_OPERATION: PotOperationDto = {
  ...OPERATION,
  bankId: null,
  creditCardId: 'amex',
  statementYear: 2026,
  statementMonth: 10,
  investmentWithdrawal: 'Pending',
}

const statementSelect = () => screen.getByRole('combobox', { name: /^Statement/ })

describe('PotWithdrawalForm paid by credit card', () => {
  it('[AC P62-F03-withdraw-forms-on-pots-account-and-card-01] shows Bank, or Card and Statement, and Category for the choice, keeping the shared fields when it changes', async () => {
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()

    choosePaidWith('Bank')
    expect(throughBank()).toBeInTheDocument()
    expect(screen.getByLabelText(/^Category/)).toBeInTheDocument()
    expect(screen.queryByRole('combobox', { name: /^Card/ })).not.toBeInTheDocument()

    choosePaidWith('Credit card')
    expect(screen.queryByRole('combobox', { name: /^Bank/ })).not.toBeInTheDocument()
    expect(screen.getByRole('combobox', { name: /^Card/ })).toBeInTheDocument()
    expect(statementSelect()).toBeInTheDocument()
    expect(screen.getByLabelText(/^Category/)).toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Boots')
    expect(screen.getByLabelText(/^Amount/)).toHaveValue(38.25)
    expect(potSelect()).toHaveValue('pot-a')

    choosePaidWith('None')
    expect(screen.queryByLabelText(/^Category/)).not.toBeInTheDocument()
  })

  it('[AC P62-F03-withdraw-forms-on-pots-account-and-card-01] sends the card, the chosen statement and the category', async () => {
    const onSaved = vi.fn()
    render(<Harness onSaved={onSaved} />)
    await potsLoaded()
    fillWithdrawal()
    choosePaidWith('Credit card')
    fireEvent.change(screen.getByRole('combobox', { name: /^Card/ }), { target: { value: 'amex' } })
    await waitFor(() => expect(optionNames(statementSelect())).toEqual(['Select a statement', 'October 2026', 'December 2026', 'January 2027']))
    expect(statementSelect()).toHaveValue('2026-10')
    fireEvent.change(statementSelect(), { target: { value: '2026-12' } })

    save()

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock).toHaveBeenCalledWith('reserva', {
      potId: 'pot-a',
      amount: 38.25,
      date: '2026-10-15',
      description: 'Boots',
      bankId: null,
      creditCardId: 'amex',
      statementYear: 2026,
      statementMonth: 12,
      categoryId: 'cat-ariana',
      roundUpAmount: null,
      confirmed: false,
    })
  })

  it('asks for a card before saving', async () => {
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()
    choosePaidWith('Credit card')

    save()

    expect(await screen.findByText('Choose a card.')).toBeInTheDocument()
    expect(createPotWithdrawalMock).not.toHaveBeenCalled()
  })

  it('[AC P62-F03-withdraw-forms-on-pots-account-and-card-03] offers only active cards, and disables Credit card with its hint when there is none', async () => {
    const { unmount } = render(<Harness />)
    await potsLoaded()
    choosePaidWith('Credit card')
    expect(optionNames(screen.getByRole('combobox', { name: /^Card/ }))).toEqual(['Select a card', 'Amex'])
    unmount()

    render(<Harness accounts={NO_CARDS} />)
    await potsLoaded()

    expect(screen.getByRole('radio', { name: 'Credit card' })).toBeDisabled()
    expect(screen.getByText('No active credit card.')).toBeInTheDocument()
  })

  it('[AC P62-F03-withdraw-forms-on-pots-account-and-card-02] [AC P62-CF-cross-feature-02] from a card locks the card and sends the same request as the Pots account', async () => {
    const onSaved = vi.fn()
    render(<Harness mode="card" onSaved={onSaved} />)
    await potsLoaded()

    expect(screen.getByRole('heading', { name: 'Withdraw from pots' })).toBeInTheDocument()
    expect(screen.getByText('Amex')).toBeInTheDocument()
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Pots account/), { target: { value: 'reserva' } })
    await waitFor(() => expect(potSelect()).toHaveValue('pot-a'))
    await waitFor(() => expect(statementSelect()).toHaveValue('2026-10'))
    fillWithdrawal()
    save('Withdraw from pots')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock).toHaveBeenCalledWith('reserva', {
      potId: 'pot-a',
      amount: 38.25,
      date: '2026-10-15',
      description: 'Boots',
      bankId: null,
      creditCardId: 'amex',
      statementYear: 2026,
      statementMonth: 10,
      categoryId: 'cat-ariana',
      roundUpAmount: null,
      confirmed: false,
    })
  })

  it('[AC P62-CF-cross-feature-03] says the investment withdrawal is pending until the statement is paid', async () => {
    render(<Harness edit={CARD_OPERATION} />)
    await potsLoaded()

    expect(screen.getByRole('radio', { name: 'Credit card' })).toBeChecked()
    await waitFor(() => expect(statementSelect()).toHaveValue('2026-10'))
    expect(screen.getByText('Investment withdrawal: Pending — taken when the October 2026 statement is paid')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete withdrawal' })).toBeEnabled()
  })

  it('[AC P62-F03-withdraw-forms-on-pots-account-and-card-04] [AC P62-CF-cross-feature-03] once the statement is paid, shows Done with bank and date, locks the paid fields and Delete, and saves description and category', async () => {
    const onSaved = vi.fn()
    const onDelete = vi.fn()
    const done = { ...CARD_OPERATION, investmentWithdrawal: 'Done', investmentWithdrawalBankId: 'b1', investmentWithdrawalDate: '2026-11-03' }
    render(<Harness edit={done} onSaved={onSaved} onDelete={onDelete} />)
    await potsLoaded()

    expect(screen.getByText('Investment withdrawal: Done — Barclays, 3 Nov 2026')).toBeInTheDocument()
    expect(screen.getByText('Its card statement is paid. Unmark the statement to change it.')).toBeInTheDocument()
    expect(screen.queryByRole('radiogroup')).not.toBeInTheDocument()
    expect(screen.queryByRole('spinbutton', { name: /^Amount/ })).not.toBeInTheDocument()
    expect(document.querySelector('input[type="date"]')).toBeNull()
    expect(screen.getByRole('textbox', { name: 'Amount' })).toHaveAttribute('readonly')
    expect(screen.queryByRole('combobox', { name: /^Statement/ })).not.toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'Statement' })).toHaveValue('October 2026')
    expect(screen.getByRole('textbox', { name: 'Amount' })).toHaveValue('£38.25')
    expect(screen.getByRole('textbox', { name: 'Card' })).toHaveValue('Amex')
    const deleteButton = screen.getByRole('button', { name: 'Delete withdrawal' })
    expect(deleteButton).toHaveAttribute('aria-disabled', 'true')
    fireEvent.click(deleteButton)
    expect(onDelete).not.toHaveBeenCalled()

    setValue(/^Description/, 'Flights to Lisbon')
    setValue(/^Category/, 'cat-casa')
    save('Save')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updatePotWithdrawalMock).toHaveBeenCalledWith('reserva', 'op1', expect.objectContaining({
      description: 'Flights to Lisbon',
      categoryId: 'cat-casa',
      amount: 38.25,
      date: '2026-10-12',
      creditCardId: 'amex',
      statementYear: 2026,
      statementMonth: 10,
    }))
  })

  it('shows the statements load error with Try again', async () => {
    getAccountStatementsMock.mockRejectedValueOnce(new Error('down')).mockResolvedValue([])
    render(<Harness mode="card" />)
    await potsLoaded()

    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(optionNames(statementSelect())).toContain('October 2026'))
  })
})

describe('PotWithdrawalForm round-up', () => {
  const CHASE = { id: 'chase', name: 'Chase', roundUpEnabled: true, isActive: true }
  const ROUND_UP_ACCOUNTS = [...ACCOUNTS, account({ id: 'chase', name: 'Chase', roundUpEnabled: true })]
  const ROUND_UP_BANKS = [...BANKS, CHASE]
  const roundUp = () => screen.queryByLabelText(/^Round-up/)
  const renderRoundUp = (props: Partial<HarnessProps> = {}) => render(<Harness accounts={ROUND_UP_ACCOUNTS} banks={ROUND_UP_BANKS} {...props} />)

  it('[AC P62-F05-round-up-on-bank-paid-withdrawal-01] suggests the round-up for a round-up bank and lets the user change it, including to 0', async () => {
    renderRoundUp()
    await potsLoaded()
    fillWithdrawal()
    setThroughBank('chase')

    expect(screen.getByLabelText('Round-up (GBP)')).toHaveValue(0.75)
    expect(screen.getByText('Charged to the bank, not taken from the pot.')).toBeInTheDocument()
    setValue(/^Round-up/, '0')
    save()

    await waitFor(() => expect(createPotWithdrawalMock).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock.mock.calls[0][1]).toMatchObject({ bankId: 'chase', amount: 38.25, roundUpAmount: 0 })
  })

  it('[AC P62-F05-round-up-on-bank-paid-withdrawal-01] shows no round-up for a bank without it, a card or no payment, and sends none', async () => {
    renderRoundUp()
    await potsLoaded()
    fillWithdrawal()

    expect(roundUp()).not.toBeInTheDocument()
    setThroughBank('b1')
    expect(roundUp()).not.toBeInTheDocument()
    choosePaidWith('Credit card')
    expect(roundUp()).not.toBeInTheDocument()
    setThroughBank('b1')
    save()

    await waitFor(() => expect(createPotWithdrawalMock).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock.mock.calls[0][1]).toMatchObject({ bankId: 'b1', roundUpAmount: null })
  })

  it('follows the amount until the user changes the round-up, then keeps it', async () => {
    renderRoundUp()
    await potsLoaded()
    setThroughBank('chase')

    setValue(/^Amount/, '10.40')
    expect(roundUp()).toHaveValue(0.6)
    setValue(/^Round-up/, '0.10')
    setValue(/^Amount/, '12.20')

    expect(roundUp()).toHaveValue(0.1)
  })

  it('sends no round-up when the field is cleared', async () => {
    renderRoundUp()
    await potsLoaded()
    fillWithdrawal()
    setThroughBank('chase')

    setValue(/^Round-up/, '')
    save()

    await waitFor(() => expect(createPotWithdrawalMock).toHaveBeenCalledOnce())
    expect(createPotWithdrawalMock.mock.calls[0][1]).toMatchObject({ roundUpAmount: null })
  })

  it('refuses a round-up outside £0.00 to £0.99 and sends nothing', async () => {
    renderRoundUp()
    await potsLoaded()
    fillWithdrawal()
    setThroughBank('chase')

    setValue(/^Round-up/, '1.50')
    save()

    expect(await screen.findByText('Round-up amount must be between £0.00 and £0.99.')).toBeInTheDocument()
    expect(createPotWithdrawalMock).not.toHaveBeenCalled()
  })

  it('[AC P62-F05-round-up-on-bank-paid-withdrawal-04] opens an edit with its round-up and saves the change', async () => {
    const onSaved = vi.fn()
    renderRoundUp({ edit: { ...OPERATION, bankId: 'chase', roundUpAmount: 0.75 }, onSaved })
    await potsLoaded()

    expect(roundUp()).toHaveValue(0.75)
    setValue(/^Round-up/, '0.50')
    save('Save')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updatePotWithdrawalMock.mock.calls[0][2]).toMatchObject({ roundUpAmount: 0.5 })
  })

  it('[AC P62-F05-round-up-on-bank-paid-withdrawal-04] adds no round-up when an older withdrawal without one is saved unchanged', async () => {
    const onSaved = vi.fn()
    renderRoundUp({ edit: { ...OPERATION, bankId: 'chase' }, onSaved })
    await potsLoaded()

    expect(roundUp()).toHaveValue(null)
    expect(screen.getByText('clean')).toBeInTheDocument()
    setValue(/^Description/, 'Boots and more')
    save('Save')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updatePotWithdrawalMock.mock.calls[0][2]).toMatchObject({ roundUpAmount: null })
  })
})
