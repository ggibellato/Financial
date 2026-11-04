import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { BankDto, IncomeDto, IncomeSourceDto } from '../../api/types'
import { setStoredDefault } from '../../utils/createFormDefaults'
import { potsAccount } from '../../test/account'
import { SPLIT_CONFIRMATION_DELAY_MS, selectActiveIncomeSources, useIncomeForm } from '../useIncomeForm'

const { createIncomeMock, updateIncomeMock, getAccountsMock } = vi.hoisted(() => ({
  createIncomeMock: vi.fn<FinancialApiClient['createIncome']>(),
  updateIncomeMock: vi.fn<FinancialApiClient['updateIncome']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    createIncome: createIncomeMock,
    updateIncome: updateIncomeMock,
    getAccounts: getAccountsMock,
  } as Partial<FinancialApiClient>,
}))

const ACCOUNTS = [
  potsAccount({ id: 'p1', name: 'Reserva' }),
  potsAccount({ id: 'p2', name: 'Holidays' }),
  potsAccount({ id: 'p3', name: 'Old pots', isActive: false }),
]

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true, isActive: true },
]

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: '1', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: false },
  { id: '2', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: 'p1', hasReferences: false },
  { id: '3', name: 'Lottery', isActive: true, group: 'NonReportable', potsAccountId: null, hasReferences: false },
  { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', potsAccountId: null, hasReferences: false },
  { id: '5', name: 'Bonus', isActive: true, group: 'NonReportable', potsAccountId: 'p3', hasReferences: false },
]

async function renderIncomeForm(onSaved: () => void) {
  const rendered = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
  await waitFor(() => expect(rendered.result.current.potsAccountsStatus).toBe('ready'))
  return rendered
}

describe('useIncomeForm', () => {
  let onSaved: () => void

  beforeEach(() => {
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    onSaved = vi.fn<() => void>()
    sessionStorage.clear()
    // shouldAdvanceTime keeps real timers ticking so RTL's waitFor polling still resolves
    // while the split-confirmation dismiss timeout is driven by fake time.
    vi.useFakeTimers({ shouldAdvanceTime: true })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('selectActiveIncomeSources filters out inactive sources and orders the rest', () => {
    const mixed: IncomeSourceDto[] = [
      { id: '4', name: 'DividendoJuros', isActive: true, group: 'DividendoJuros', potsAccountId: null, hasReferences: false },
      { id: '3', name: 'Lottery', isActive: false, group: 'NonReportable', potsAccountId: null, hasReferences: false },
      { id: '2', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: 'p1', hasReferences: false },
      { id: '1', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: false },
    ]

    const result = selectActiveIncomeSources(mixed)

    expect(result.map((s) => s.name)).toEqual(['Gleison', 'Ariana', 'DividendoJuros'])
  })

  it('defaults the new-income source to the first active option and leaves bank unselected once the form opens', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeSource).toBe('1')
    expect(result.current.incomeBank).toBe('')
  })

  it('leaves the default source unset when there are no income sources', () => {
    const { result } = renderHook(() => useIncomeForm([], BANKS, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeSource).toBe('')
  })

  it('submits a create request with a null bankId and null description when both are left blank', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '42.50'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(
      expect.objectContaining({ bankId: null, description: null }),
    )
  })

  it('submits a create request with the provided description', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '42.50'))
    act(() => result.current.setIncomeField('description', 'Chip ISA dividend'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(
      expect.objectContaining({ description: 'Chip ISA dividend' }),
    )
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] starts ticked into the source\'s Pots account on create', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())

    act(() => result.current.setIncomeField('incomeSource', '2'))

    expect(result.current.incomeSplitToReserve).toBe('true')
    expect(result.current.incomePotsAccountId).toBe('p1')
  })

  it('offers the active Pots accounts by name', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())

    expect(result.current.potsAccounts.map((account) => account.name)).toEqual(['Holidays', 'Reserva'])
  })

  it('also offers the stored Pots account on edit when it is now inactive', async () => {
    const { result } = await renderIncomeForm(onSaved)
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: 'p3',
      hasLinkedDeposit: false,
      fundingLines: [],
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.potsAccounts.map((account) => account.name)).toEqual(['Holidays', 'Old pots', 'Reserva'])
    expect(result.current.incomePotsAccountId).toBe('p3')
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] sends another Pots account when one is chosen', async () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))

    act(() => result.current.setIncomeField('potsAccountId', 'p2'))
    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ potsAccountId: 'p2' }))
  })

  it('marks an otherwise unchanged edit dirty when another Pots account is chosen', async () => {
    const { result } = await renderIncomeForm(onSaved)
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: 'p1',
      hasLinkedDeposit: false,
      fundingLines: [],
    }
    act(() => result.current.showEditIncomeForm(income))
    expect(result.current.isIncomeDirty).toBe(false)

    act(() => result.current.setIncomeField('potsAccountId', 'p2'))

    expect(result.current.isIncomeDirty).toBe(true)
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] resets the split and the Pots account when the source changes', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))
    act(() => result.current.setIncomeField('potsAccountId', 'p2'))
    act(() => result.current.setIncomeField('splitToReserve', 'false'))

    act(() => result.current.setIncomeField('incomeSource', '1'))
    act(() => result.current.setIncomeField('incomeSource', '2'))

    expect(result.current.incomeSplitToReserve).toBe('true')
    expect(result.current.incomePotsAccountId).toBe('p1')
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] leaves no Pots account chosen when the source\'s is inactive, and asks for one on save', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '5'))
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    act(() => result.current.submitIncome())

    expect(result.current.incomeSplitToReserve).toBe('true')
    expect(result.current.incomePotsAccountId).toBe('')
    expect(result.current.saveIncomeErrorFields).toEqual({ potsAccountId: 'Choose a Pots account.' })
    expect(createIncomeMock).not.toHaveBeenCalled()
  })

  it('sends no Pots account when an income\'s source no longer splits, so the hidden split can\'t block the save', async () => {
    updateIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['updateIncome']>>)
    const { result } = await renderIncomeForm(onSaved)
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '1', incomeSourceName: 'Gleison',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: 'p1',
      hasLinkedDeposit: false,
      fundingLines: [],
    }
    act(() => result.current.showEditIncomeForm(income))

    act(() => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ potsAccountId: null }))
  })

  it('clears an inactive default Pots account once the accounts arrive, without marking the form dirty', async () => {
    let resolveAccounts: (accounts: typeof ACCOUNTS) => void = () => {}
    getAccountsMock.mockReset().mockReturnValue(new Promise((resolve) => (resolveAccounts = resolve)))
    setStoredDefault('income.incomeSource', '5')
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    expect(result.current.incomePotsAccountId).toBe('p3')

    await act(async () => resolveAccounts(ACCOUNTS))

    await waitFor(() => expect(result.current.incomePotsAccountId).toBe(''))
    expect(result.current.incomeSplitToReserve).toBe('true')
    expect(result.current.isIncomeDirty).toBe(false)
  })

  it('exposes the accounts load failure and retries it', async () => {
    getAccountsMock.mockReset().mockRejectedValueOnce(new Error('down')).mockResolvedValue(ACCOUNTS)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    await waitFor(() => expect(result.current.potsAccountsStatus).toBe('failed'))

    act(() => result.current.retryPotsAccounts())

    await waitFor(() => expect(result.current.potsAccountsStatus).toBe('ready'))
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] defaults splitToReserve to false when the default source is ineligible', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeSource).toBe('1')
    expect(result.current.incomeSplitToReserve).toBe('false')
  })

  it('switching to an eligible source sets splitToReserve to true', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())

    act(() => result.current.setIncomeField('incomeSource', '2'))

    expect(result.current.incomeSplitToReserve).toBe('true')
  })

  it('switching back to an ineligible source sets splitToReserve to false', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))

    act(() => result.current.setIncomeField('incomeSource', '1'))

    expect(result.current.incomeSplitToReserve).toBe('false')
  })

  it('populates incomeSplitToReserve as true for a split income', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: 'p1',
      hasLinkedDeposit: false,
      fundingLines: [],
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.incomeSplitToReserve).toBe('true')
  })

  it('populates incomeSplitToReserve as false for an unsplit income', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: null,
      hasLinkedDeposit: false,
      fundingLines: [],
    }

    act(() => result.current.showEditIncomeForm(income))

    expect(result.current.incomeSplitToReserve).toBe('false')
  })

  it("submits a create request with the source's Pots account when checked", () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ potsAccountId: 'p1' }))
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] submits null as the Pots account when the split is unticked', () => {
    createIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('incomeSource', '2'))
    act(() => result.current.setIncomeField('splitToReserve', 'false'))
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ potsAccountId: null }))
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-05] keeps the Pots account a split income names when it is edited, even if its source now names another', () => {
    updateIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['updateIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: 'p9',
      hasLinkedDeposit: false,
      fundingLines: [],
    }
    act(() => result.current.showEditIncomeForm(income))
    act(() => result.current.setIncomeField('netValue', '1000'))

    act(() => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ potsAccountId: 'p9' }))
  })

  it('submits null when the split of a split income is unticked on edit', () => {
    updateIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['updateIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: null, bankName: null, description: null,
      potsAccountId: 'p9',
      hasLinkedDeposit: false,
      fundingLines: [],
    }
    act(() => result.current.showEditIncomeForm(income))
    act(() => result.current.setIncomeField('splitToReserve', 'false'))

    act(() => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ potsAccountId: null }))
  })

  it("splits an unsplit income into its source's Pots account when the split is ticked on edit", () => {
    updateIncomeMock.mockResolvedValue({} as Awaited<ReturnType<FinancialApiClient['updateIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    const income: IncomeDto = {
      id: 'i1', date: '2026-07-25', incomeSourceId: '2', incomeSourceName: 'Ariana',
      grossValue: null, netValue: 2450, bankId: 'bank-barclays', bankName: 'Barclays', description: null,
      potsAccountId: null,
      hasLinkedDeposit: false,
      fundingLines: [],
    }
    act(() => result.current.showEditIncomeForm(income))
    act(() => result.current.setIncomeField('splitToReserve', 'true'))

    act(() => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ potsAccountId: 'p1' }))
  })

  it('names the Pots account in the confirmation and tells onSaved when the create response comes back split', async () => {
    createIncomeMock.mockResolvedValue({ potsAccountId: 'p2' } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    await act(async () => {
      result.current.submitIncome()
    })

    expect(result.current.splitConfirmationMessage).toBe('Income saved and split to Holidays')
    expect(onSaved).toHaveBeenCalledWith(expect.objectContaining({ potsAccountId: 'p2' }), false, 'Holidays')
  })

  it('leaves splitConfirmationMessage null when the create response comes back unsplit', async () => {
    createIncomeMock.mockResolvedValue({ potsAccountId: null } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))

    await act(async () => {
      result.current.submitIncome()
    })

    expect(result.current.splitConfirmationMessage).toBeNull()
  })

  it('clears splitConfirmationMessage automatically after the delay', async () => {
    createIncomeMock.mockResolvedValue({ potsAccountId: 'p1' } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))
    await act(async () => {
      result.current.submitIncome()
    })
    expect(result.current.splitConfirmationMessage).toBe('Income saved and split to Reserva')

    act(() => {
      vi.advanceTimersByTime(SPLIT_CONFIRMATION_DELAY_MS)
    })

    await waitFor(() => expect(result.current.splitConfirmationMessage).toBeNull())
  })

  it('persists date, bank, and income source after a successful create, for the next create form', async () => {
    createIncomeMock.mockResolvedValue({ potsAccountId: null } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.setIncomeField('incomeSource', INCOME_SOURCES[1].id))
    act(() => result.current.setIncomeField('netValue', '2450'))
    await act(async () => {
      result.current.submitIncome()
    })

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeDate).toBe('2026-07-25')
    expect(result.current.incomeBank).toBe('bank-barclays')
    expect(result.current.incomeSource).toBe(INCOME_SOURCES[1].id)
  })

  it('[AC P60-F06-admin-accounts-page-05] leaves the bank empty when the remembered bank is inactive', () => {
    setStoredDefault('income.bank', 'bank-barclays')
    const banks = [{ ...BANKS[0], isActive: false }, BANKS[1]]
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, banks, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeBank).toBe('')
  })

  it('leaves the bank empty when the remembered bank no longer exists', () => {
    setStoredDefault('income.bank', 'bank-deleted')
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeBank).toBe('')
  })

  it('always starts gross/net value and description blank on a new create form, even after a persisted save', async () => {
    createIncomeMock.mockResolvedValue({ potsAccountId: null } as Awaited<ReturnType<FinancialApiClient['createIncome']>>)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '2450'))
    act(() => result.current.setIncomeField('description', 'Salary'))
    await act(async () => {
      result.current.submitIncome()
    })

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeNetValue).toBe('')
    expect(result.current.incomeGrossValue).toBe('')
    expect(result.current.incomeDescription).toBe('')
  })

  it('rejects a submit with a blank date, blank source, and a gross value below the net value, without calling the API', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', ''))
    act(() => result.current.setIncomeField('incomeSource', ''))
    act(() => result.current.setIncomeField('netValue', '100'))
    act(() => result.current.setIncomeField('grossValue', '50'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.saveIncomeErrorFields.date).toBe('Date is required')
    expect(result.current.saveIncomeErrorFields.incomeSource).toBe('Income source is required')
    expect(result.current.saveIncomeErrorFields.grossValue).toBe('Gross value must be at least the net value')
  })

  it('rejects a submit with a non-numeric net value, without calling the API', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', 'not-a-number'))

    act(() => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.saveIncomeErrorFields.netValue).toBe('Net value must be a non-negative number')
  })

  it('cancelIncomeForm closes the form and resets its fields', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('description', 'Draft'))

    act(() => result.current.cancelIncomeForm())

    expect(result.current.isIncomeFormOpen).toBe(false)
    expect(result.current.incomeDescription).toBe('')
  })

  it('surfaces a create error returned by the backend', async () => {
    createIncomeMock.mockRejectedValue(new Error('Income source is inactive'))
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-07-25'))
    act(() => result.current.setIncomeField('netValue', '42.50'))

    await act(async () => {
      result.current.submitIncome()
    })

    expect(result.current.saveIncomeError).toBe('Income source is inactive')
  })
})

describe('useIncomeForm funding lines', () => {
  const LINKED_INCOME: IncomeDto = {
    id: 'i1', date: '2026-10-14', incomeSourceId: '2', incomeSourceName: 'Ariana',
    grossValue: null, netValue: 100, bankId: 'bank-barclays', bankName: 'Barclays', description: 'Salary',
    potsAccountId: 'p1',
    hasLinkedDeposit: true,
    fundingLines: [{ bankId: 'bank-barclays', amount: 90 }],
  }
  const HAND_SPLIT_INCOME: IncomeDto = {
    ...LINKED_INCOME,
    fundingLines: [{ bankId: 'bank-barclays', amount: 60 }, { bankId: 'bank-trading212', amount: 30 }],
  }
  const OLDER_INCOME: IncomeDto = { ...LINKED_INCOME, hasLinkedDeposit: false, fundingLines: [] }
  let onSaved: () => void

  beforeEach(() => {
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset().mockResolvedValue(LINKED_INCOME)
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    onSaved = vi.fn<() => void>()
    sessionStorage.clear()
  })

  type Form = { current: ReturnType<typeof useIncomeForm> }

  function openNewSplitIncome(result: Form) {
    act(() => result.current.showDepositIncomeForm('p1'))
    act(() => result.current.setIncomeField('date', '2026-10-14'))
    act(() => result.current.setIncomeField('netValue', '100'))
  }

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-01] shows the default line from the bank for the whole deposit, and leaves it to the server', async () => {
    createIncomeMock.mockResolvedValue(LINKED_INCOME)
    const { result } = await renderIncomeForm(onSaved)
    openNewSplitIncome(result)
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))

    expect(result.current.incomeFunding.isShown).toBe(true)
    expect(result.current.incomeFunding.isHandSplit).toBe(false)
    expect(result.current.incomeFunding.bankName).toBe('Barclays')
    expect(result.current.incomeFunding.depositAmount).toBe(90)

    await act(async () => result.current.submitIncome())

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ bankId: 'bank-barclays', potsAccountId: 'p1', fundingLines: null }))
  })

  it('needs a bank before it can save a split, and says so on the Bank field', async () => {
    const { result } = await renderIncomeForm(onSaved)
    openNewSplitIncome(result)

    await act(async () => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.saveIncomeErrorFields.bank).toBe('Choose the bank this income landed in.')
  })

  it('asks for a net value above zero before saving a split', async () => {
    const { result } = await renderIncomeForm(onSaved)
    openNewSplitIncome(result)
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.setIncomeField('netValue', '0'))

    await act(async () => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.saveIncomeErrorFields.netValue).toBe('Net value must be greater than zero to split to pots.')
  })

  it('sends no lines when the split is not ticked', async () => {
    updateIncomeMock.mockResolvedValue(LINKED_INCOME)
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm(LINKED_INCOME))
    act(() => result.current.setIncomeField('splitToReserve', 'false'))

    expect(result.current.incomeFunding.isShown).toBe(false)

    await act(async () => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ potsAccountId: null, fundingLines: null }))
  })

  it('hides the section for an older split income and sends no lines', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm(OLDER_INCOME))

    expect(result.current.incomeFunding.isShown).toBe(false)

    await act(async () => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ potsAccountId: 'p1', fundingLines: null }))
  })

  it('keeps the section hidden when an older split income is unticked and ticked again in the same edit', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm(OLDER_INCOME))
    act(() => result.current.setIncomeField('splitToReserve', 'false'))
    act(() => result.current.setIncomeField('splitToReserve', 'true'))

    expect(result.current.incomeFunding.isShown).toBe(false)
  })

  it('shows the default line when an income that never split is ticked', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm({ ...OLDER_INCOME, potsAccountId: null }))
    act(() => result.current.setIncomeField('splitToReserve', 'true'))

    expect(result.current.incomeFunding.isShown).toBe(true)
    expect(result.current.incomeFunding.isHandSplit).toBe(false)
  })

  it('reopens a single line on the income bank as the default line and sends no lines for it', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm(LINKED_INCOME))

    expect(result.current.incomeFunding.isHandSplit).toBe(false)
    expect(result.current.isIncomeDirty).toBe(false)

    await act(async () => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ fundingLines: null }))
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] reopens hand-split lines and sends them on every save, so a save does not reset them', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm(HAND_SPLIT_INCOME))

    expect(result.current.incomeFunding.isHandSplit).toBe(true)
    expect(result.current.incomeFunding.lines.map(({ bankId, amount }) => [bankId, amount])).toEqual([['bank-barclays', '60'], ['bank-trading212', '30']])
    expect(result.current.isIncomeDirty).toBe(false)

    act(() => result.current.setIncomeField('description', 'Changed'))
    await act(async () => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({
      fundingLines: [{ bankId: 'bank-barclays', amount: 60 }, { bankId: 'bank-trading212', amount: 30 }],
    }))
  })

  it('reopens one line on another bank as a hand split', async () => {
    const { result } = await renderIncomeForm(onSaved)
    act(() => result.current.showEditIncomeForm({ ...LINKED_INCOME, fundingLines: [{ bankId: 'bank-trading212', amount: 90 }] }))

    expect(result.current.incomeFunding.isHandSplit).toBe(true)
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] blocks the save with the mismatch text until the lines add up', async () => {
    createIncomeMock.mockResolvedValue(LINKED_INCOME)
    const { result } = await renderIncomeForm(onSaved)
    openNewSplitIncome(result)
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.incomeFunding.splitAcrossBanks())

    const [first] = result.current.incomeFunding.lines
    expect(first).toMatchObject({ bankId: 'bank-barclays', amount: '90.00' })
    expect(result.current.incomeFunding.mismatch).toBeNull()
    expect(result.current.isIncomeDirty).toBe(true)

    act(() => result.current.incomeFunding.changeLine(first.key, { amount: '60' }))
    expect(result.current.incomeFunding.mismatch).toBe('Lines add up to £60.00; the deposit is £90.00.')

    await act(async () => result.current.submitIncome())
    expect(createIncomeMock).not.toHaveBeenCalled()

    act(() => result.current.incomeFunding.addLine())
    const second = result.current.incomeFunding.lines[1]
    expect(second.amount).toBe('30.00')
    act(() => result.current.incomeFunding.changeLine(second.key, { bankId: 'bank-trading212' }))
    expect(result.current.incomeFunding.mismatch).toBeNull()

    await act(async () => result.current.submitIncome())
    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({
      fundingLines: [{ bankId: 'bank-barclays', amount: 60 }, { bankId: 'bank-trading212', amount: 30 }],
    }))
  })

  it('marks a line with no bank and keeps the values', async () => {
    const { result } = await renderIncomeForm(onSaved)
    openNewSplitIncome(result)
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.incomeFunding.splitAcrossBanks())
    const [first] = result.current.incomeFunding.lines
    act(() => result.current.incomeFunding.changeLine(first.key, { bankId: '' }))

    await act(async () => result.current.submitIncome())

    expect(createIncomeMock).not.toHaveBeenCalled()
    expect(result.current.incomeFunding.errors).toEqual({ [`line:${first.key}:bankId`]: 'Choose a bank.' })
    expect(result.current.incomeNetValue).toBe('100')
  })

  it('stops adding lines at five and removes a line', async () => {
    const { result } = await renderIncomeForm(onSaved)
    openNewSplitIncome(result)
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.incomeFunding.splitAcrossBanks())
    for (let added = 0; added < 5; added += 1) act(() => result.current.incomeFunding.addLine())

    expect(result.current.incomeFunding.lines).toHaveLength(5)

    act(() => result.current.incomeFunding.removeLine(result.current.incomeFunding.lines[4].key))
    expect(result.current.incomeFunding.lines).toHaveLength(4)
  })

  it('returns to the default line for the next entry after a kept-open save', async () => {
    createIncomeMock.mockResolvedValue(LINKED_INCOME)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { keepOpenAfterSave: true }))
    await waitFor(() => expect(result.current.potsAccountsStatus).toBe('ready'))
    openNewSplitIncome(result)
    act(() => result.current.setIncomeField('bank', 'bank-barclays'))
    act(() => result.current.incomeFunding.splitAcrossBanks())

    await act(async () => result.current.submitIncome())

    expect(result.current.isIncomeFormOpen).toBe(true)
    expect(result.current.incomeFunding.isHandSplit).toBe(false)
    expect(result.current.incomeFunding.lines).toEqual([])
    expect(result.current.isIncomeDirty).toBe(false)
  })

  it('does not move an income opened on another bank screen to that bank', async () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank: { id: 'bank-trading212', name: 'Trading212' } }))
    await waitFor(() => expect(result.current.potsAccountsStatus).toBe('ready'))
    act(() => result.current.showEditIncomeForm(HAND_SPLIT_INCOME))

    expect(result.current.incomeLockedBank).toBeUndefined()

    await act(async () => result.current.submitIncome())

    expect(updateIncomeMock).toHaveBeenCalledWith('i1', expect.objectContaining({ bankId: 'bank-barclays' }))
  })

  it('keeps the bank locked on an income of the screen bank', async () => {
    const lockedBank = { id: 'bank-barclays', name: 'Barclays' }
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank }))
    await waitFor(() => expect(result.current.potsAccountsStatus).toBe('ready'))
    act(() => result.current.showEditIncomeForm(LINKED_INCOME))

    expect(result.current.incomeLockedBank).toEqual(lockedBank)
    expect(result.current.incomeFunding.bankName).toBe('Barclays')
  })
})

describe('useIncomeForm account settings', () => {
  const LOCKED_CHASE = { id: 'bank-chase', name: 'Chase' }
  const SAVED: IncomeDto = {
    id: 'i9', date: '2026-10-14', incomeSourceId: '1', incomeSourceName: 'Gleison',
    grossValue: null, netValue: 100, bankId: 'bank-chase', bankName: 'Chase', description: 'Bonus',
    potsAccountId: null,
    hasLinkedDeposit: false,
    fundingLines: [],
  }
  let onSaved: () => void

  beforeEach(() => {
    createIncomeMock.mockReset()
    updateIncomeMock.mockReset()
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    onSaved = vi.fn<() => void>()
    sessionStorage.clear()
  })

  async function addIncome(result: { current: ReturnType<typeof useIncomeForm> }) {
    act(() => result.current.showCreateIncomeForm())
    act(() => result.current.setIncomeField('date', '2026-10-14'))
    act(() => result.current.setIncomeField('grossValue', '120'))
    act(() => result.current.setIncomeField('netValue', '100'))
    act(() => result.current.setIncomeField('description', 'Bonus'))
    await act(async () => result.current.submitIncome())
  }

  it('opens with the locked bank and sends it in the request', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank: LOCKED_CHASE }))

    await addIncome(result)

    expect(createIncomeMock).toHaveBeenCalledWith(expect.objectContaining({ bankId: 'bank-chase' }))
  })

  it('does not remember the locked bank as the default bank', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const locked = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank: LOCKED_CHASE }))
    await addIncome(locked.result)

    const monthly = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))
    act(() => monthly.result.current.showCreateIncomeForm())

    expect(monthly.result.current.incomeBank).toBe('')
  })

  it('keeps the form open after a create with the same date and source, clearing values and description', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() =>
      useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank: LOCKED_CHASE, keepOpenAfterSave: true }),
    )

    await addIncome(result)

    expect(onSaved).toHaveBeenCalledOnce()
    expect(result.current.isIncomeFormOpen).toBe(true)
    expect(result.current.incomeDate).toBe('2026-10-14')
    expect(result.current.incomeSource).toBe('1')
    expect(result.current.incomeBank).toBe('bank-chase')
    expect(result.current.incomeGrossValue).toBe('')
    expect(result.current.incomeNetValue).toBe('')
    expect(result.current.incomeDescription).toBe('')
    expect(result.current.isIncomeDirty).toBe(false)
  })

  it('closes the form after a create when keep-open is not set', async () => {
    createIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))

    await addIncome(result)

    expect(result.current.isIncomeFormOpen).toBe(false)
  })

  it('opens on today when viewing the current month and the last-used date is in another month', () => {
    vi.setSystemTime(new Date('2026-10-15T12:00:00+01:00'))
    setStoredDefault('income.date', '2026-08-30')
    const { result } = renderHook(() =>
      useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank: LOCKED_CHASE, viewedMonth: { year: 2026, month: 10 } }),
    )

    act(() => result.current.showCreateIncomeForm())

    expect(result.current.incomeDate).toBe('2026-10-15')
  })

  it('closes the form after an edit even when keep-open is set', async () => {
    updateIncomeMock.mockResolvedValue(SAVED)
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { keepOpenAfterSave: true }))

    act(() => result.current.showEditIncomeForm(SAVED))
    await act(async () => result.current.submitIncome())

    expect(result.current.isIncomeFormOpen).toBe(false)
  })

  it('keeps the values and shows the connection message when the save cannot reach the server', async () => {
    createIncomeMock.mockRejectedValue(new ApiError('Failed to fetch', 0))
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved, { lockedBank: LOCKED_CHASE }))

    await addIncome(result)

    expect(result.current.saveIncomeError).toBe('Could not save. Check the connection and try again.')
    expect(result.current.incomeDescription).toBe('Bonus')
    expect(result.current.isIncomeFormOpen).toBe(true)
  })

  it('reports dirty only while the values differ from those the form opened with', () => {
    const { result } = renderHook(() => useIncomeForm(INCOME_SOURCES, BANKS, onSaved))

    act(() => result.current.showCreateIncomeForm())
    expect(result.current.isIncomeDirty).toBe(false)

    act(() => result.current.setIncomeField('netValue', '10'))
    expect(result.current.isIncomeDirty).toBe(true)
  })
})
