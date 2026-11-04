import { useEffect } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDto, BankDto, CategoryDto, PotOperationDto, PotsSummaryDto } from '../../api/types'
import { overdraftMessage, usePotWithdrawalForm } from '../../hooks/usePotWithdrawalForm'
import { account, potsAccount } from '../../test/account'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import PotWithdrawalForm from '../PotWithdrawalForm'

const { createPotWithdrawalMock, updatePotWithdrawalMock, getPotsMock } = vi.hoisted(() => ({
  createPotWithdrawalMock: vi.fn<FinancialApiClient['createPotWithdrawal']>(),
  updatePotWithdrawalMock: vi.fn<FinancialApiClient['updatePotWithdrawal']>(),
  getPotsMock: vi.fn<FinancialApiClient['getPots']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createPotWithdrawal: createPotWithdrawalMock,
    updatePotWithdrawal: updatePotWithdrawalMock,
    getPots: getPotsMock,
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
]

const BANKS: BankDto[] = [
  { id: 'b1', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: true },
  { id: 'b9', name: 'Old bank', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false, isActive: false },
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
  potId: 'pot-b',
  bankId: 'b1',
  categoryId: 'cat-saude',
  fundingLines: [],
}

interface HarnessProps {
  mode?: 'pots' | 'bank'
  edit?: PotOperationDto
  accounts?: AccountDto[]
  onSaved?: () => void
  onDelete?: () => void
  onCancel?: () => void
}

function Harness({ mode = 'pots', edit, accounts = ACCOUNTS, onSaved = vi.fn(), onDelete = vi.fn(), onCancel = vi.fn() }: HarnessProps) {
  const form = usePotWithdrawalForm(accounts, CATEGORIES, { year: 2026, month: 10 }, onSaved)
  const { openEdit, openFromBank, openFromPotsAccount } = form

  useEffect(() => {
    if (edit) openEdit(edit)
    else if (mode === 'bank') openFromBank('b1')
    else openFromPotsAccount('reserva')
    // eslint-disable-next-line react-hooks/exhaustive-deps -- opens once, as the action button does
  }, [])

  return (
    <>
      {form.isOpen && (
        <PotWithdrawalForm
          form={form}
          banks={BANKS}
          lockedBank={mode === 'bank' ? { id: 'b1', name: 'Barclays' } : undefined}
          onDelete={onDelete}
          onCancel={onCancel}
        />
      )}
      <p>{form.isDirty ? 'dirty' : 'clean'}</p>
    </>
  )
}

const setValue = (label: RegExp, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })
const throughBank = () => screen.getByRole('combobox', { name: /Through bank/ })
const setThroughBank = (value: string) => fireEvent.change(throughBank(), { target: { value } })
const potSelect = () => screen.getByLabelText(/^Pot\b/)
const potsLoaded = () => waitFor(() => expect(screen.getByRole('button', { name: /^(Add withdrawal|Withdraw from pots|Save)$/ })).toBeEnabled())
const save = (name = 'Add withdrawal') => fireEvent.click(screen.getByRole('button', { name }))

function fillWithdrawal() {
  setValue(/^Description/, 'Boots')
  setValue(/^Amount/, '38.25')
}

function overdraftError(balance = '25.85') {
  return new ApiError(`This withdrawal exceeds Ariana's balance of ${balance}. Set confirmed=true to proceed.`, 409)
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
  sessionStorage.clear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('overdraftMessage', () => {
  it('states how far below zero the withdrawal takes the pot', () => {
    expect(overdraftMessage(overdraftError().message, 'Ariana', 38.25, 'GBP')).toBe('This takes Ariana below zero (−£12.40). Continue?')
  })

  it('counts a pot already below zero', () => {
    expect(overdraftMessage(overdraftError('-5.00').message, 'Ariana', 10, 'GBP')).toBe('This takes Ariana below zero (−£15.00). Continue?')
  })

  it('still asks when the figure cannot be read', () => {
    expect(overdraftMessage('Not enough money.', 'Ariana', 10, 'GBP')).toBe('This takes Ariana below zero. Continue?')
  })
})

describe('PotWithdrawalForm from the Pots account', () => {
  it('picks the first active pot, lists every pot with the inactive one marked, and starts with no bank', async () => {
    render(<Harness />)
    await potsLoaded()

    expect(screen.getByRole('heading', { name: 'New withdrawal' })).toBeInTheDocument()
    expect(potSelect()).toHaveValue('pot-a')
    expect(within(potSelect()).getAllByRole('option').map((option) => option.textContent)).toEqual(['Ariana', 'Casa', 'Old car (inactive)'])
    expect(throughBank()).toHaveValue('')
    expect(screen.queryByLabelText(/^Category/)).not.toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-15')
    expect(screen.getByLabelText(/^Amount \(GBP\)/)).toBeInTheDocument()
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

  it('closes the dialog and shows the error when the confirmed save fails', async () => {
    createPotWithdrawalMock.mockRejectedValueOnce(overdraftError()).mockRejectedValueOnce(new ApiError('Barclays is inactive.', 400))
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()
    save()

    fireEvent.click(within(await findDialog()).getByText('Continue', { selector: 'button' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Barclays is inactive.')
    await waitFor(() => expect(screen.queryByRole('dialog', { hidden: true })).not.toBeInTheDocument())
  })

  it('shows a server refusal in an alert and returns focus to Save', async () => {
    createPotWithdrawalMock.mockRejectedValue(new ApiError("Category 'Saude' cannot be used for a withdrawal expense.", 400))
    render(<Harness />)
    await potsLoaded()
    fillWithdrawal()

    save()

    expect(await screen.findByRole('alert')).toHaveTextContent("Category 'Saude' cannot be used")
    await waitFor(() => expect(screen.getByRole('button', { name: 'Add withdrawal' })).toHaveFocus())
  })

  it('offers a retry when the pots fail, with Save disabled', async () => {
    getPotsMock.mockRejectedValueOnce(new Error('offline'))
    render(<Harness />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load the pots')
    expect(screen.getByRole('button', { name: 'Add withdrawal' })).toBeDisabled()

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
    expect(screen.queryByRole('combobox', { name: /Through bank/ })).not.toBeInTheDocument()
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
      confirmed: false,
    })
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
