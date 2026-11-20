import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CategoryDto, PotOperationDto, PotsSummaryDto } from '../../api/types'
import { account, potsAccount } from '../../test/account'
import { pinDate } from '../../test-utils/pinDate'
import { getStoredDefault } from '../../utils/createFormDefaults'
import { usePotWithdrawalForm } from '../usePotWithdrawalForm'
import { never } from '../../test-utils/deferred'

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
  ],
}

const ACCOUNTS = [
  account({ id: 'b1', name: 'Barclays', categoryId: 'cat-bank' }),
  potsAccount({ id: 'reserva', name: 'Reserva', categoryId: 'cat-reserva' }),
  potsAccount({ id: 'holidays', name: 'Holidays', categoryId: 'cat-holidays' }),
]

const category = (id: string, name: string): CategoryDto => ({
  id,
  name,
  active: true,
  isInvestment: false,
  isTithe: false,
  hasReferences: false,
})

const CATEGORIES = [category('cat-casa', 'Casa'), category('cat-saude', 'Saude'), category('cat-reserva', 'Reserva')]

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

function renderForm(onSaved = vi.fn()) {
  return renderHook(() => usePotWithdrawalForm(ACCOUNTS, CATEGORIES, { year: 2026, month: 10 }, onSaved))
}

async function openWithPotsLoaded() {
  const rendered = renderForm()
  act(() => rendered.result.current.openFromPotsAccount('reserva'))
  await waitFor(() => expect(rendered.result.current.pots).not.toBeNull())
  return rendered
}

function fillValidWithdrawal(result: { current: ReturnType<typeof usePotWithdrawalForm> }) {
  act(() => result.current.setField('description', 'Boots'))
  act(() => result.current.setField('amount', '10'))
}

describe('usePotWithdrawalForm', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    createPotWithdrawalMock.mockReset()
    updatePotWithdrawalMock.mockReset()
    getPotsMock.mockReset()
    getPotsMock.mockResolvedValue(POTS)
    sessionStorage.clear()
  })

  it('clears the chosen pot when the pots account changes', async () => {
    const { result } = await openWithPotsLoaded()
    act(() => result.current.setField('potId', 'pot-b'))

    act(() => result.current.setField('potsAccountId', 'holidays'))

    expect(result.current.values?.potId).not.toBe('pot-b')
  })

  it('clears the category when the bank is cleared, and only then', async () => {
    const { result } = await openWithPotsLoaded()
    act(() => result.current.setField('bankId', 'b1'))
    act(() => result.current.setField('categoryId', 'cat-saude'))

    act(() => result.current.setField('description', ''))
    const categoryAfterOtherFieldCleared = result.current.values?.categoryId
    act(() => result.current.setField('bankId', ''))

    expect(categoryAfterOtherFieldCleared).toBe('cat-saude')
    expect(result.current.values?.categoryId).toBe('')
  })

  it('keeps the category when the bank changes to another bank', async () => {
    const { result } = await openWithPotsLoaded()
    act(() => result.current.setField('bankId', 'b1'))
    act(() => result.current.setField('categoryId', 'cat-saude'))

    act(() => result.current.setField('bankId', 'b2'))

    expect(result.current.values?.categoryId).toBe('cat-saude')
  })

  it('asks for a pot when the account has no active pot', async () => {
    getPotsMock.mockResolvedValue({ ...POTS, pots: POTS.pots.map((pot) => ({ ...pot, isActive: false })) })
    const { result } = await openWithPotsLoaded()
    fillValidWithdrawal(result)

    act(() => result.current.submit())

    expect(result.current.errorFields.potId).toBe('Choose a pot.')
  })

  it('does not submit before the pots have loaded', () => {
    getPotsMock.mockReturnValue(never())
    const { result } = renderForm()
    act(() => result.current.openFromPotsAccount('reserva'))
    fillValidWithdrawal(result)

    act(() => result.current.submit())

    expect(result.current.errorFields).toEqual({})
    expect(createPotWithdrawalMock).not.toHaveBeenCalled()
  })

  it('rejects a whitespace-only date and description', async () => {
    const { result } = await openWithPotsLoaded()
    fillValidWithdrawal(result)
    act(() => result.current.setField('date', '  '))
    act(() => result.current.setField('description', '  '))

    act(() => result.current.submit())

    expect(result.current.errorFields).toMatchObject({ date: 'Enter the withdrawal date.', description: 'Enter a description.' })
    expect(createPotWithdrawalMock).not.toHaveBeenCalled()
  })

  it('saves a trimmed description and remembers the date for the next withdrawal', async () => {
    createPotWithdrawalMock.mockResolvedValue({} as never)
    const { result } = await openWithPotsLoaded()
    fillValidWithdrawal(result)
    act(() => result.current.setField('description', '  Boots  '))
    act(() => result.current.setField('date', '2026-10-03'))

    await act(async () => result.current.submit())

    expect(createPotWithdrawalMock).toHaveBeenCalledWith('reserva', expect.objectContaining({ description: 'Boots' }))
    expect(getStoredDefault('potWithdrawal.date')).toBe('2026-10-03')
  })

  it('sends a second submit only once while the first is saving', async () => {
    createPotWithdrawalMock.mockReturnValue(never())
    const { result } = await openWithPotsLoaded()
    fillValidWithdrawal(result)

    act(() => result.current.submit())
    act(() => result.current.submit())

    expect(result.current.isSaving).toBe(true)
    expect(createPotWithdrawalMock).toHaveBeenCalledTimes(1)
  })

  it('stops saving and shows the save error when the request fails', async () => {
    createPotWithdrawalMock.mockRejectedValue('unexpected')
    const { result } = await openWithPotsLoaded()
    fillValidWithdrawal(result)

    await act(async () => result.current.submit())

    expect(result.current.isSaving).toBe(false)
    expect(result.current.error).toBe('Failed to save the withdrawal.')
  })

  it('opens an edit unlocked, remembering the bank and category it opened with', async () => {
    const { result } = renderForm()

    act(() => result.current.openEdit(OPERATION))
    await waitFor(() => expect(result.current.pots).not.toBeNull())

    expect(result.current).toMatchObject({ isBankLocked: false, openedBankId: 'b1', openedCategoryId: 'cat-saude', currency: 'GBP' })
  })

  it('opens from a bank with the bank locked', () => {
    const { result } = renderForm()

    act(() => result.current.openFromBank('b1'))

    expect(result.current).toMatchObject({ isBankLocked: true, openedBankId: 'b1' })
    expect(result.current.values).toMatchObject({ potsAccountId: 'holidays', bankId: 'b1', amount: '' })
  })

  it('opens an edit with no pot, bank or category as blanks, defaulting the pot to the first active one', async () => {
    const { result } = renderForm()

    act(() => result.current.openEdit({ ...OPERATION, potId: null, bankId: null, categoryId: null }))
    await waitFor(() => expect(result.current.pots).not.toBeNull())

    expect(result.current.values).toMatchObject({ potId: 'pot-a', bankId: '', categoryId: '' })
  })

  it('reports a pots load failure', async () => {
    getPotsMock.mockRejectedValue(new Error('down'))
    const { result } = renderForm()

    act(() => result.current.openFromPotsAccount('reserva'))

    await waitFor(() => expect(result.current.hasPotsError).toBe(true))
  })

  it('reports no pots error and no saving state before anything is opened', () => {
    const { result } = renderForm()

    expect(result.current).toMatchObject({ hasPotsError: false, isSaving: false, isBankLocked: false, openedBankId: '', openedCategoryId: '', currency: '' })
  })
})
