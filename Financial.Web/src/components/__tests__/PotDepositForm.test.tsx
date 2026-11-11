import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest'
import { useEffect } from 'react'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankOption, IncomeSourceDto, PotOperationDto, PotsSummaryDto } from '../../api/types'
import { usePotDepositForm } from '../../hooks/usePotDepositForm'
import { fireEvent, render, screen, waitFor, within } from '../../test/renderWithFluent'
import { pinDate } from '../../test-utils/pinDate'
import PotDepositForm from '../PotDepositForm'

const { createPotDepositMock, updatePotDepositMock, getPotsMock } = vi.hoisted(() => ({
  createPotDepositMock: vi.fn<FinancialApiClient['createPotDeposit']>(),
  updatePotDepositMock: vi.fn<FinancialApiClient['updatePotDeposit']>(),
  getPotsMock: vi.fn<FinancialApiClient['getPots']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createPotDeposit: createPotDepositMock,
    updatePotDeposit: updatePotDepositMock,
    getPots: getPotsMock,
  } as Partial<FinancialApiClient>,
}))

const RESERVA = { id: 'reserva', name: 'Reserva', currency: 'GBP' }

const POTS: PotsSummaryDto = {
  potsAccountId: 'reserva',
  currency: 'GBP',
  total: 100,
  pots: [
    { id: 'pot-a', name: 'Ariana', splitPercentage: 33.33, isActive: true, balance: 10 },
    { id: 'pot-b', name: 'Casa', splitPercentage: 33.33, isActive: true, balance: 10 },
    { id: 'pot-c', name: 'Saude', splitPercentage: 33.34, isActive: true, balance: 10 },
    { id: 'pot-old', name: 'Old car', splitPercentage: 0, isActive: false, balance: 0 },
  ],
}

const BANKS: BankOption[] = [
  { id: 'b1', name: 'Barclays', roundUpEnabled: false, isActive: true },
  { id: 'b2', name: 'Trading 212', roundUpEnabled: false, isActive: true },
]

const SOURCES: IncomeSourceDto[] = [{ id: 's1', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: 'reserva', hasReferences: true }]

const SAVED_OPERATION: PotOperationDto = {
  operationId: 'op1',
  kind: 'Deposit',
  potsAccountId: 'reserva',
  date: '2026-10-10',
  description: 'Ramsay bonus',
  amount: 90,
  into: 'ActivePots',
  potId: null,
  bankId: null,
  categoryId: null,
  incomeId: null,
  fundingLines: [
    { bankId: 'b1', amount: 85 },
    { bankId: 'b2', amount: 5 },
  ],
}

interface HarnessProps {
  edit?: PotOperationDto
  incomeSources?: IncomeSourceDto[]
  onSaved?: () => void
  onFromIncome?: () => void
  onDelete?: () => void
  onCancel?: () => void
}

function Harness({ edit, incomeSources = SOURCES, onSaved = vi.fn(), onFromIncome = vi.fn(), onDelete = vi.fn(), onCancel = vi.fn() }: HarnessProps) {
  const form = usePotDepositForm(RESERVA, { year: 2026, month: 10 }, onSaved)
  const { open, openEdit } = form

  useEffect(() => {
    if (edit) openEdit(edit)
    else open()
    // eslint-disable-next-line react-hooks/exhaustive-deps -- opens once, as the action button does
  }, [])

  return (
    <>
      {form.isOpen && <PotDepositForm form={form} banks={BANKS} incomeSources={incomeSources} onFromIncome={onFromIncome} onDelete={onDelete} onCancel={onCancel} />}
      <p>{form.isDirty ? 'dirty' : 'clean'}</p>
      <button type="button" onClick={() => openEdit(SAVED_OPERATION)}>
        Open the saved deposit
      </button>
    </>
  )
}

const setValue = (label: RegExp, value: string) => fireEvent.change(screen.getByLabelText(label), { target: { value } })

function fillManualDeposit() {
  setDepositAmount('90')
  setValue(/^Description/, 'Ramsay bonus')
}

const depositAmount = () => screen.getAllByLabelText(/^Amount \(GBP\)/)[0]
const setDepositAmount = (value: string) => fireEvent.change(depositAmount(), { target: { value } })
const firstLine = () => within(screen.getByRole('group', { name: 'Funding line 1' }))
const save = (name = 'Add deposit') => fireEvent.click(screen.getByRole('button', { name }))

beforeEach(() => {
  pinDate('2026-10-15T12:00:00+01:00')
  createPotDepositMock.mockReset().mockResolvedValue(SAVED_OPERATION)
  updatePotDepositMock.mockReset().mockResolvedValue(SAVED_OPERATION)
  getPotsMock.mockReset().mockResolvedValue(POTS)
  sessionStorage.clear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('PotDepositForm', () => {
  it('opens as a manual deposit into the active pots, dated today, with one blank line', async () => {
    render(<Harness />)

    await screen.findByRole('table', { name: 'Split preview' })
    expect(screen.getByRole('radio', { name: 'Manual' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'Active pots by split' })).toBeChecked()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-15')
    expect(firstLine().getByLabelText(/^Bank/)).toHaveValue('')
    expect(screen.getByRole('button', { name: 'Add deposit' })).toBeEnabled()
    expect(screen.queryByRole('button', { name: 'Delete deposit' })).not.toBeInTheDocument()
    expect(screen.getByText('clean')).toBeInTheDocument()
  })

  it('previews the split once with the residual on the largest percentage', async () => {
    render(<Harness />)
    const table = await screen.findByRole('table', { name: 'Split preview' })

    expect(within(within(table).getByText('Saude').closest('tr') as HTMLElement).getByText('—')).toBeInTheDocument()
    setDepositAmount('10')

    expect(within(within(table).getByText('Ariana').closest('tr') as HTMLElement).getByText('3.33')).toBeInTheDocument()
    expect(within(within(table).getByText('Saude').closest('tr') as HTMLElement).getByText('3.34')).toBeInTheDocument()
    expect(within(table).queryByText('Old car')).not.toBeInTheDocument()
  })

  it('explains the preview when the active pots do not total 100%', async () => {
    getPotsMock.mockResolvedValue({ ...POTS, pots: [{ ...POTS.pots[0], splitPercentage: 95 }] })
    render(<Harness />)

    expect(await screen.findByText(/They must total exactly 100% to deposit by split/)).toHaveTextContent('Active pots total 95.00%')
    expect(screen.queryByRole('table', { name: 'Split preview' })).not.toBeInTheDocument()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] the only line follows the amount, and a mismatch disables Save until the lines add up', async () => {
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })

    setDepositAmount('90')
    expect(firstLine().getByLabelText(/^Amount/)).toHaveValue(90)

    fireEvent.change(firstLine().getByLabelText(/^Amount/), { target: { value: '85' } })
    expect(screen.getAllByRole('status').filter((region) => region.textContent?.includes('Lines add up to £85.00; the deposit is £90.00.'))).toHaveLength(1)
    expect(screen.getByRole('button', { name: 'Add deposit' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Add line' }))
    expect(within(screen.getByRole('group', { name: 'Funding line 2' })).getByLabelText(/^Amount/)).toHaveValue(5)
    expect(screen.getByRole('button', { name: 'Add deposit' })).toBeEnabled()
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] posts one funding line per bank', async () => {
    const onSaved = vi.fn()
    render(<Harness onSaved={onSaved} />)
    await screen.findByRole('table', { name: 'Split preview' })
    fillManualDeposit()
    fireEvent.change(firstLine().getByLabelText(/^Bank/), { target: { value: 'b1' } })
    fireEvent.change(firstLine().getByLabelText(/^Amount/), { target: { value: '85' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add line' }))
    fireEvent.change(within(screen.getByRole('group', { name: 'Funding line 2' })).getByLabelText(/^Bank/), { target: { value: 'b2' } })

    save()

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(createPotDepositMock).toHaveBeenCalledWith('reserva', {
      amount: 90,
      date: '2026-10-15',
      description: 'Ramsay bonus',
      potId: null,
      fundingLines: [
        { bankId: 'b1', amount: 85 },
        { bankId: 'b2', amount: 5 },
      ],
    })
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-03] sends no lines for money from outside CashFlow', async () => {
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })
    fillManualDeposit()

    fireEvent.click(screen.getByRole('checkbox', { name: 'No bank — money from outside CashFlow' }))
    expect(screen.queryByRole('group', { name: 'Funding line 1' })).not.toBeInTheDocument()
    save()

    await waitFor(() => expect(createPotDepositMock).toHaveBeenCalled())
    expect(createPotDepositMock.mock.calls[0][1].fundingLines).toEqual([])
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-09] lists every pot for one pot, marks the inactive one, and sends the chosen pot', async () => {
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })
    fillManualDeposit()
    fireEvent.click(screen.getByRole('checkbox', { name: 'No bank — money from outside CashFlow' }))

    fireEvent.click(screen.getByRole('radio', { name: 'One pot' }))

    expect(screen.queryByRole('table', { name: 'Split preview' })).not.toBeInTheDocument()
    const pot = screen.getByLabelText(/^Pot/)
    expect(within(pot).getAllByRole('option').map((option) => option.textContent)).toEqual(['Select a pot', 'Ariana', 'Casa', 'Saude', 'Old car (inactive)'])

    save()
    expect(await screen.findByText('Choose a pot.')).toBeInTheDocument()
    expect(createPotDepositMock).not.toHaveBeenCalled()

    setValue(/^Pot/, 'pot-old')
    save()
    await waitFor(() => expect(createPotDepositMock).toHaveBeenCalled())
    expect(createPotDepositMock.mock.calls[0][1].potId).toBe('pot-old')
  })

  it('marks every invalid field, focuses the first and sends nothing', async () => {
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })
    setValue(/^Date/, '2026-10-16')

    save()

    expect(await screen.findByText('Date must not be in the future.')).toBeInTheDocument()
    expect(screen.getByText('Enter a description.')).toBeInTheDocument()
    expect(screen.getByText('Choose a bank.')).toBeInTheDocument()
    expect(screen.getAllByText('Amount must be greater than zero.')).toHaveLength(2)
    expect(depositAmount()).toHaveFocus()
    expect(createPotDepositMock).not.toHaveBeenCalled()

    setValue(/^Date/, '')
    save()
    expect(await screen.findByText('Enter the deposit date.')).toBeInTheDocument()
  })

  it('shows a server refusal in an alert, keeps the values and moves focus to the alert', async () => {
    createPotDepositMock.mockRejectedValue(new ApiError('Active pots must total 100% (now 95%).', 400))
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })
    fillManualDeposit()
    fireEvent.click(screen.getByRole('checkbox', { name: 'No bank — money from outside CashFlow' }))

    save()

    const alert = await screen.findByRole('alert')
    expect(alert).toHaveTextContent('Active pots must total 100% (now 95%).')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Ramsay bonus')
    await waitFor(() => expect(alert).toHaveFocus())
  })

  it('disables the fields and Cancel while saving, and Save says so and stays focusable', async () => {
    createPotDepositMock.mockReturnValue(new Promise(() => undefined))
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })
    fillManualDeposit()
    fireEvent.click(screen.getByRole('checkbox', { name: 'No bank — money from outside CashFlow' }))

    save()

    expect(await screen.findByRole('button', { name: 'Saving...' })).toHaveAttribute('aria-disabled', 'true')
    expect(screen.getByLabelText(/^Description/)).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    save('Saving...')
    expect(createPotDepositMock).toHaveBeenCalledOnce()
  })

  it('waits for the pots before saving, and offers a retry when they fail', async () => {
    getPotsMock.mockRejectedValueOnce(new Error('offline'))
    render(<Harness />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load the pots')
    expect(screen.getByRole('button', { name: 'Add deposit' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await screen.findByRole('table', { name: 'Split preview' })
    expect(screen.getByRole('button', { name: 'Add deposit' })).toBeEnabled()
  })

  it('choosing From an income only selects it; the income form opens from an explicit Continue', async () => {
    const onFromIncome = vi.fn()
    render(<Harness onFromIncome={onFromIncome} />)
    await screen.findByRole('table', { name: 'Split preview' })
    setDepositAmount('90')

    const fromIncome = screen.getByRole('radio', { name: 'From an income' })
    fireEvent.click(fromIncome)

    expect(onFromIncome).not.toHaveBeenCalled()
    expect(fromIncome).toBeChecked()
    expect(screen.queryByRole('radio', { name: 'One pot' })).not.toBeInTheDocument()
    expect(screen.queryByRole('group', { name: 'Funding line 1' })).not.toBeInTheDocument()
    expect(screen.queryByRole('table', { name: 'Split preview' })).not.toBeInTheDocument()

    save('Continue to the income form')
    expect(onFromIncome).toHaveBeenCalledOnce()
    expect(createPotDepositMock).not.toHaveBeenCalled()

    fireEvent.click(screen.getByRole('radio', { name: 'Manual' }))
    expect(depositAmount()).toHaveValue(90)
    expect(screen.getByRole('button', { name: 'Add deposit' })).toBeEnabled()
  })

  it('shows the edit fields when an operation opens over a form on From an income', async () => {
    render(<Harness />)
    await screen.findByRole('table', { name: 'Split preview' })
    fireEvent.click(screen.getByRole('radio', { name: 'From an income' }))

    fireEvent.click(screen.getByRole('button', { name: 'Open the saved deposit' }))

    expect(screen.getByRole('heading', { name: 'Edit deposit' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Description/)).toHaveValue('Ramsay bonus')
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Continue to the income form' })).not.toBeInTheDocument()
  })

  it('offers no income source when none names a Pots account', async () => {
    render(<Harness incomeSources={[{ ...SOURCES[0], potsAccountId: null }]} />)

    expect(screen.getByRole('radio', { name: 'From an income' })).toBeDisabled()
    expect(screen.getByText(/so a deposit can only be manual/)).toBeInTheDocument()
    await screen.findByRole('table', { name: 'Split preview' })
  })

  it('opens filled from the operation, with Delete and no Source, and puts back the rebuilt deposit', async () => {
    const onSaved = vi.fn()
    const onDelete = vi.fn()
    render(<Harness edit={SAVED_OPERATION} onSaved={onSaved} onDelete={onDelete} />)
    await screen.findByRole('table', { name: 'Split preview' })

    expect(screen.getByRole('heading', { name: 'Edit deposit' })).toBeInTheDocument()
    expect(screen.queryByRole('radio', { name: 'Manual' })).not.toBeInTheDocument()
    expect(firstLine().getByLabelText(/^Bank/)).toHaveValue('b1')
    expect(screen.getByLabelText(/^Date/)).toHaveValue('2026-10-10')
    expect(screen.getByText('clean')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Delete deposit' }))
    expect(onDelete).toHaveBeenCalledOnce()

    setValue(/^Description/, 'Bonus')
    expect(screen.getByText('dirty')).toBeInTheDocument()
    save('Save')

    await waitFor(() => expect(onSaved).toHaveBeenCalledOnce())
    expect(updatePotDepositMock).toHaveBeenCalledWith('reserva', 'op1', {
      amount: 90,
      date: '2026-10-10',
      description: 'Bonus',
      potId: null,
      fundingLines: [
        { bankId: 'b1', amount: 85 },
        { bankId: 'b2', amount: 5 },
      ],
    })
  })

  it('opens a one-pot deposit with no lines as it was saved', async () => {
    render(<Harness edit={{ ...SAVED_OPERATION, into: 'OnePot', potId: 'pot-old', fundingLines: [] }} />)

    await waitFor(() => expect(screen.getByLabelText(/^Pot/)).toHaveValue('pot-old'))
    expect(screen.getByRole('radio', { name: 'One pot' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'No bank — money from outside CashFlow' })).toBeChecked()
  })
})
