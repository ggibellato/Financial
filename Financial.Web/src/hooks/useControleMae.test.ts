import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { MaeLedgerEntryDto, MaeLedgerTotalsDto } from '../api/types'
import { useControleMae } from './useControleMae'

const CURRENT_YEAR = new Date().getFullYear()
const DEFAULT_FROM_DATE = `${CURRENT_YEAR - 1}-01-01`
const OTHER_FROM_DATE = `${CURRENT_YEAR - 2}-06-01`

const getMaeLedgerEntriesFromDateMock = vi.fn<FinancialApiClient['getMaeLedgerEntriesFromDate']>()
const getMaeLedgerTotalsMock = vi.fn<FinancialApiClient['getMaeLedgerTotals']>()
const createMaeLedgerEntryMock = vi.fn<FinancialApiClient['createMaeLedgerEntry']>()
const updateMaeLedgerEntryValuesMock = vi.fn<FinancialApiClient['updateMaeLedgerEntryValues']>()
const deleteMaeLedgerEntryMock = vi.fn<FinancialApiClient['deleteMaeLedgerEntry']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMaeLedgerEntriesFromDate: getMaeLedgerEntriesFromDateMock,
    getMaeLedgerTotals: getMaeLedgerTotalsMock,
    createMaeLedgerEntry: createMaeLedgerEntryMock,
    updateMaeLedgerEntryValues: updateMaeLedgerEntryValuesMock,
    deleteMaeLedgerEntry: deleteMaeLedgerEntryMock,
  }),
}))

const ENTRIES: MaeLedgerEntryDto[] = [
  {
    id: 'e1',
    date: `${CURRENT_YEAR}-07-15`,
    description: 'School supplies',
    note: 'Term start',
    sourceCurrency: 'BRL',
    brlValue: 350,
    gbpValue: 51.1,
  },
]

const TOTALS: MaeLedgerTotalsDto = { totalBrlValue: 1000, totalGbpValue: 145.3 }

describe('useControleMae', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getMaeLedgerEntriesFromDateMock.mockResolvedValue(ENTRIES)
    getMaeLedgerTotalsMock.mockResolvedValue(TOTALS)
  })

  it('fetches entries from January of the previous year by default', async () => {
    const { result } = renderHook(() => useControleMae())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMaeLedgerEntriesFromDateMock).toHaveBeenCalledWith(DEFAULT_FROM_DATE)
    expect(result.current.fromDateInputValue).toBe(DEFAULT_FROM_DATE)
    expect(result.current.entries).toEqual(ENTRIES)
  })

  it('fetches the all-time totals independently of the selected from-date', async () => {
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMaeLedgerTotalsMock).toHaveBeenCalled()
    expect(result.current.totals).toEqual(TOTALS)
  })

  it('re-fetches entries from the new date when the from-date input changes', async () => {
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setFromDateInputValue(OTHER_FROM_DATE))

    await waitFor(() => expect(getMaeLedgerEntriesFromDateMock).toHaveBeenCalledWith(OTHER_FROM_DATE))
  })

  it('creates an entry and re-fetches entries and totals on success', async () => {
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
    await waitFor(() => expect(getMaeLedgerEntriesFromDateMock).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(getMaeLedgerTotalsMock).toHaveBeenCalledTimes(2))
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

  it('saves an edit and re-fetches entries and totals on success', async () => {
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
    await waitFor(() => expect(getMaeLedgerEntriesFromDateMock).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(getMaeLedgerTotalsMock).toHaveBeenCalledTimes(2))
  })

  it('deletes an entry and re-fetches entries and totals on success', async () => {
    deleteMaeLedgerEntryMock.mockResolvedValue(undefined)
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteEntry('e1'))

    await waitFor(() => expect(deleteMaeLedgerEntryMock).toHaveBeenCalledWith('e1'))
    await waitFor(() => expect(getMaeLedgerEntriesFromDateMock).toHaveBeenCalledTimes(2))
    await waitFor(() => expect(getMaeLedgerTotalsMock).toHaveBeenCalledTimes(2))
  })

  it('surfaces a delete error without crashing', async () => {
    deleteMaeLedgerEntryMock.mockRejectedValue(new Error('Mae ledger entry not found.'))
    const { result } = renderHook(() => useControleMae())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.deleteEntry('unknown'))

    await waitFor(() => expect(result.current.deleteError).toBe('Mae ledger entry not found.'))
  })
})
