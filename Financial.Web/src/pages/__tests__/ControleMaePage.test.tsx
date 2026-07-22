import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ControleMaePage from '../ControleMaePage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { MaeLedgerEntryDto } from '../../api/types'

const getMaeLedgerEntriesByMonthMock = vi.fn<FinancialApiClient['getMaeLedgerEntriesByMonth']>()
const createMaeLedgerEntryMock = vi.fn<FinancialApiClient['createMaeLedgerEntry']>()
const updateMaeLedgerEntryValuesMock = vi.fn<FinancialApiClient['updateMaeLedgerEntryValues']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getMaeLedgerEntriesByMonth: getMaeLedgerEntriesByMonthMock,
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

describe('ControleMaePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getMaeLedgerEntriesByMonthMock.mockResolvedValue(ENTRIES)
  })

  it('shows a loading state before data arrives', () => {
    render(<ControleMaePage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getMaeLedgerEntriesByMonthMock.mockRejectedValue(new Error('Network down'))

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

  it('renders the create-entry form', async () => {
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('New Entry')).toBeInTheDocument())
    expect(screen.getByLabelText('Date')).toBeInTheDocument()
    expect(screen.getByLabelText('Description')).toBeInTheDocument()
    expect(screen.getByLabelText('Currency')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Add Entry' })).toBeInTheDocument()
  })

  it('edits an entry values and saves, updating the displayed row', async () => {
    updateMaeLedgerEntryValuesMock.mockResolvedValue({ ...ENTRIES[0], brlValue: 355, gbpValue: 51.6 })
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit' }))
    const brlInput = screen.getByDisplayValue('350')
    fireEvent.change(brlInput, { target: { value: '355' } })

    getMaeLedgerEntriesByMonthMock.mockResolvedValue([{ ...ENTRIES[0], brlValue: 355, gbpValue: 51.6 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateMaeLedgerEntryValuesMock).toHaveBeenCalledWith('e1', { brlValue: 355, gbpValue: 51.1 }),
    )
    await waitFor(() => expect(screen.getByText('355.00')).toBeInTheDocument())
  })
})
