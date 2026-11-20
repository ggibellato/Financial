import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PotOperationDto, PotsSummaryDto, TransferAccountOptionDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault } from '../../utils/createFormDefaults'
import { usePotDepositForm } from '../usePotDepositForm'

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

const RESERVA: TransferAccountOptionDto = { id: 'reserva', name: 'Reserva', currency: 'GBP' }

const POTS: PotsSummaryDto = {
  potsAccountId: 'reserva',
  currency: 'GBP',
  total: 100,
  pots: [{ id: 'pot-a', name: 'Ariana', splitPercentage: 100, isActive: true, balance: 100 }],
}

const DEPOSIT: PotOperationDto = {
  operationId: 'op1',
  kind: 'Deposit',
  potsAccountId: 'reserva',
  date: '2026-10-12',
  description: 'Bonus',
  amount: 30,
  into: 'OnePot',
  incomeId: null,
  potId: 'pot-a',
  bankId: null,
  categoryId: null,
  fundingLines: [
    { bankId: 'b1', amount: 20 },
    { bankId: 'b2', amount: 10 },
  ],
}

function renderForm(potsAccount: TransferAccountOptionDto | undefined = RESERVA) {
  return renderHook(() => usePotDepositForm(potsAccount, { year: 2026, month: 10 }, vi.fn()))
}

function fillValidDeposit(result: { current: ReturnType<typeof usePotDepositForm> }) {
  act(() => result.current.setField('description', 'Bonus'))
  act(() => result.current.setHasNoBank(true))
  act(() => result.current.setAmount('10'))
}

describe('usePotDepositForm', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    createPotDepositMock.mockReset()
    updatePotDepositMock.mockReset()
    getPotsMock.mockReset()
    getPotsMock.mockResolvedValue(POTS)
    sessionStorage.clear()
  })

  it('keeps a single funding line in step with the amount until it is edited', () => {
    const { result } = renderForm()
    act(() => result.current.open())

    act(() => result.current.setAmount('10'))
    const followed = result.current.values?.lines[0].amount
    act(() => result.current.changeLine(result.current.values!.lines[0].key, { amount: '7' }))
    act(() => result.current.setAmount('12'))

    expect(followed).toBe('10')
    expect(result.current.values?.lines[0].amount).toBe('7')
  })

  it('leaves several funding lines alone when the amount changes', () => {
    const { result } = renderForm()
    act(() => result.current.open())
    act(() => result.current.addLine())

    act(() => result.current.setAmount('10'))

    expect(result.current.values?.lines.map((line) => line.amount)).toEqual(['', ''])
  })

  it('opens an edit with its funding lines, into the one pot it named', () => {
    const { result } = renderForm()

    act(() => result.current.openEdit(DEPOSIT))

    expect(result.current.values).toMatchObject({ into: 'OnePot', potId: 'pot-a', hasNoBank: false })
    expect(result.current.values?.lines.map(({ bankId, amount }) => ({ bankId, amount }))).toEqual([
      { bankId: 'b1', amount: '20' },
      { bankId: 'b2', amount: '10' },
    ])
    expect(result.current.openedBankIds).toEqual(['b1', 'b2'])
  })

  it('opens an edit with no funding lines as a deposit with no bank and one blank line', () => {
    const { result } = renderForm()

    act(() => result.current.openEdit({ ...DEPOSIT, potId: null, fundingLines: [] }))

    expect(result.current.values).toMatchObject({ into: 'ActivePots', potId: '', hasNoBank: true })
    expect(result.current.values?.lines).toHaveLength(1)
  })

  it('removes a funding line', () => {
    const { result } = renderForm()
    act(() => result.current.openEdit(DEPOSIT))

    act(() => result.current.removeLine(result.current.values!.lines[0].key))

    expect(result.current.values?.lines.map((line) => line.bankId)).toEqual(['b2'])
  })

  it('rejects a whitespace-only date and description', () => {
    const { result } = renderForm()
    act(() => result.current.open())
    fillValidDeposit(result)
    act(() => result.current.setField('date', '  '))
    act(() => result.current.setField('description', '  '))

    act(() => result.current.submit())

    expect(result.current.errorFields).toMatchObject({ date: 'Enter the deposit date.', description: 'Enter a description.' })
    expect(createPotDepositMock).not.toHaveBeenCalled()
  })

  it('saves a trimmed description and remembers the date for the next deposit', async () => {
    createPotDepositMock.mockResolvedValue({} as never)
    const { result } = renderForm()
    act(() => result.current.open())
    fillValidDeposit(result)
    act(() => result.current.setField('description', '  Bonus  '))
    act(() => result.current.setField('date', '2026-10-03'))

    await act(async () => result.current.submit())

    expect(createPotDepositMock).toHaveBeenCalledWith('reserva', expect.objectContaining({ description: 'Bonus' }))
    expect(getStoredDefault('potDeposit.date')).toBe('2026-10-03')
  })

  it('stops saving and shows the save error when the request fails', async () => {
    createPotDepositMock.mockRejectedValue('unexpected')
    const { result } = renderForm()
    act(() => result.current.open())
    fillValidDeposit(result)

    await act(async () => result.current.submit())

    expect(result.current.isSaving).toBe(false)
    expect(result.current.error).toBe('Failed to save the deposit.')
  })

  it('reports a pots load failure', async () => {
    getPotsMock.mockRejectedValue(new Error('down'))
    const { result } = renderForm()

    act(() => result.current.open())

    await waitFor(() => expect(result.current.hasPotsError).toBe(true))
  })

  it('reports no pots error, no saving state and no currency before anything is opened', () => {
    const { result } = renderHook(() => usePotDepositForm(undefined, { year: 2026, month: 10 }, vi.fn()))

    expect(result.current).toMatchObject({ hasPotsError: false, isSaving: false, currency: '', openedBankIds: [] })
  })
})
