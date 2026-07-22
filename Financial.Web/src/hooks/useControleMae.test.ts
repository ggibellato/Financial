import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { MaeLedgerEntryDto } from '../api/types'
import { useControleMae } from './useControleMae'

const NOW = new Date()
const CURRENT_YEAR = NOW.getFullYear()
const CURRENT_MONTH = NOW.getMonth() + 1
const CURRENT_MONTH_INPUT = `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}`
const NEXT_MONTH = CURRENT_MONTH === 12 ? 1 : CURRENT_MONTH + 1
const NEXT_MONTH_YEAR = CURRENT_MONTH === 12 ? CURRENT_YEAR + 1 : CURRENT_YEAR
const NEXT_MONTH_INPUT = `${NEXT_MONTH_YEAR}-${String(NEXT_MONTH).padStart(2, '0')}`

const getMaeLedgerEntriesByMonthMock = vi.fn<FinancialApiClient['getMaeLedgerEntriesByMonth']>()
const createMaeLedgerEntryMock = vi.fn<FinancialApiClient['createMaeLedgerEntry']>()
const updateMaeLedgerEntryValuesMock = vi.fn<FinancialApiClient['updateMaeLedgerEntryValues']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMaeLedgerEntriesByMonth: getMaeLedgerEntriesByMonthMock,
    createMaeLedgerEntry: createMaeLedgerEntryMock,
    updateMaeLedgerEntryValues: updateMaeLedgerEntryValuesMock,
  }),
}))

const ENTRIES: MaeLedgerEntryDto[] = [
  {
    id: 'e1',
    date: `${CURRENT_YEAR}-${String(CURRENT_MONTH).padStart(2, '0')}-15`,
    description: 'School supplies',
    note: 'Term start',
    sourceCurrency: 'BRL',
    brlValue: 350,
    gbpValue: 51.1,
  },
]

describe('useControleMae', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getMaeLedgerEntriesByMonthMock.mockResolvedValue(ENTRIES)
  })

  it('fetches entries for the current month on mount', async () => {
    const { result } = renderHook(() => useControleMae())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMaeLedgerEntriesByMonthMock).toHaveBeenCalledWith(CURRENT_YEAR, CURRENT_MONTH)
    expect(result.current.monthInputValue).toBe(CURRENT_MONTH_INPUT)
    expect(result.current.entries).toEqual(ENTRIES)
  })

  it('re-fetches for a new month when the month input changes', async () => {
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setMonthInputValue(NEXT_MONTH_INPUT))

    await waitFor(() => expect(getMaeLedgerEntriesByMonthMock).toHaveBeenCalledWith(NEXT_MONTH_YEAR, NEXT_MONTH))
  })

  it('creates an entry and re-fetches on success', async () => {
    createMaeLedgerEntryMock.mockResolvedValue({
      id: 'e2',
      date: `${CURRENT_YEAR}-07-16`,
      description: 'Medical appointment',
      note: '',
      sourceCurrency: 'GBP',
      brlValue: null,
      gbpValue: 40,
    })
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', `${CURRENT_YEAR}-07-16`))
    act(() => result.current.setCreateField('createDescription', 'Medical appointment'))
    act(() => result.current.setCreateField('createSourceCurrency', 'GBP'))
    act(() => result.current.setCreateField('createSourceValue', '40'))
    act(() => result.current.submitCreate())

    await waitFor(() =>
      expect(createMaeLedgerEntryMock).toHaveBeenCalledWith(
        expect.objectContaining({ description: 'Medical appointment', sourceCurrency: 'GBP', sourceValue: 40 }),
      ),
    )
    await waitFor(() => expect(getMaeLedgerEntriesByMonthMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a backend validation error on create failure without crashing', async () => {
    createMaeLedgerEntryMock.mockRejectedValue(new Error('Date must not be in the future.'))
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setCreateField('createDate', '2099-01-01'))
    act(() => result.current.setCreateField('createDescription', 'Future'))
    act(() => result.current.setCreateField('createSourceValue', '10'))
    act(() => result.current.submitCreate())

    await waitFor(() => expect(result.current.createError).toBe('Date must not be in the future.'))
  })

  it('saves an edit and re-fetches on success', async () => {
    updateMaeLedgerEntryValuesMock.mockResolvedValue({ ...ENTRIES[0], brlValue: 355, gbpValue: 51.6 })
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.showEditForm(ENTRIES[0]))
    act(() => result.current.setEditField('editBrlValue', '355'))
    act(() => result.current.setEditField('editGbpValue', '51.6'))
    act(() => result.current.saveEdit())

    await waitFor(() =>
      expect(updateMaeLedgerEntryValuesMock).toHaveBeenCalledWith('e1', { brlValue: 355, gbpValue: 51.6 }),
    )
    await waitFor(() => expect(getMaeLedgerEntriesByMonthMock).toHaveBeenCalledTimes(2))
  })
})
