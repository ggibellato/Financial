import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ControleMaePage from '../ControleMaePage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { MaeLedgerEntryDto, MaeLedgerTotalsDto } from '../../api/types'

const getMaeLedgerEntriesFromDateMock = vi.fn<FinancialApiClient['getMaeLedgerEntriesFromDate']>()
const getMaeLedgerTotalsMock = vi.fn<FinancialApiClient['getMaeLedgerTotals']>()
const createMaeLedgerEntryMock = vi.fn<FinancialApiClient['createMaeLedgerEntry']>()
const updateMaeLedgerEntryValuesMock = vi.fn<FinancialApiClient['updateMaeLedgerEntryValues']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMaeLedgerEntriesFromDate: getMaeLedgerEntriesFromDateMock,
    getMaeLedgerTotals: getMaeLedgerTotalsMock,
    createMaeLedgerEntry: createMaeLedgerEntryMock,
    updateMaeLedgerEntryValues: updateMaeLedgerEntryValuesMock,
  }),
}))

const ENTRIES: MaeLedgerEntryDto[] = [
  {
    id: 'e1',
    date: '2026-07-15',
    description: 'School supplies',
    note: 'Term start',
    sourceCurrency: 'BRL',
    brlValue: 350,
    gbpValue: 51.1,
  },
]

const TOTALS: MaeLedgerTotalsDto = { totalBrlValue: 5000, totalGbpValue: 720.45 }

describe('ControleMaePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getMaeLedgerEntriesFromDateMock.mockResolvedValue(ENTRIES)
    getMaeLedgerTotalsMock.mockResolvedValue(TOTALS)
  })

  it('shows a loading state before data arrives', () => {
    render(<ControleMaePage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getMaeLedgerEntriesFromDateMock.mockRejectedValue(new Error('Network down'))

    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('renders both BRL and GBP values for every ledger entry once loaded', async () => {
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())
    expect(screen.getByText('350.00')).toBeInTheDocument()
    expect(screen.getByText('51.10')).toBeInTheDocument()
  })

  it('renders the full BRL and GBP totals across all entries, not just the filtered ones', async () => {
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())
    expect(screen.getByText('5,000.00')).toBeInTheDocument()
    expect(screen.getByText('720.45')).toBeInTheDocument()
  })

  it('shows the create-entry form only after New Entry is clicked', async () => {
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Entry' })).toBeInTheDocument())
    expect(screen.queryByLabelText('Date')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Entry' }))

    expect(screen.getByLabelText('Date')).toBeInTheDocument()
    expect(screen.getByLabelText('Description')).toBeInTheDocument()
    expect(screen.getByLabelText('Currency')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add Entry' })).toBeInTheDocument()
  })

  it('edits an entry values via the toggled panel and saves, updating the displayed row', async () => {
    updateMaeLedgerEntryValuesMock.mockResolvedValue({ ...ENTRIES[0], brlValue: 355, gbpValue: 51.6 })
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit' }))
    expect(screen.getByText('Edit Entry')).toBeInTheDocument()
    const brlInput = screen.getByDisplayValue('350')
    fireEvent.change(brlInput, { target: { value: '355' } })

    getMaeLedgerEntriesFromDateMock.mockResolvedValue([{ ...ENTRIES[0], brlValue: 355, gbpValue: 51.6 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateMaeLedgerEntryValuesMock).toHaveBeenCalledWith('e1', { brlValue: 355, gbpValue: 51.1 }),
    )
    await waitFor(() => expect(screen.getByText('355.00')).toBeInTheDocument())
  })
})
