import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ControleMaePage from '../ControleMaePage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { MaeLedgerEntryDto, MaeLedgerTotalsDto } from '../../api/types'

const {
  getMaeLedgerEntriesFromDateMock,
  getMaeLedgerTotalsMock,
  createMaeLedgerEntryMock,
  updateMaeLedgerEntryValuesMock,
  deleteMaeLedgerEntryMock,
} = vi.hoisted(() => ({
  getMaeLedgerEntriesFromDateMock: vi.fn<FinancialApiClient['getMaeLedgerEntriesFromDate']>(),
  getMaeLedgerTotalsMock: vi.fn<FinancialApiClient['getMaeLedgerTotals']>(),
  createMaeLedgerEntryMock: vi.fn<FinancialApiClient['createMaeLedgerEntry']>(),
  updateMaeLedgerEntryValuesMock: vi.fn<FinancialApiClient['updateMaeLedgerEntryValues']>(),
  deleteMaeLedgerEntryMock: vi.fn<FinancialApiClient['deleteMaeLedgerEntry']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMaeLedgerEntriesFromDate: getMaeLedgerEntriesFromDateMock,
    getMaeLedgerTotals: getMaeLedgerTotalsMock,
    createMaeLedgerEntry: createMaeLedgerEntryMock,
    updateMaeLedgerEntryValues: updateMaeLedgerEntryValuesMock,
    deleteMaeLedgerEntry: deleteMaeLedgerEntryMock,
  } as Partial<FinancialApiClient>,
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
    getMaeLedgerEntriesFromDateMock.mockReset()
    getMaeLedgerTotalsMock.mockReset()
    createMaeLedgerEntryMock.mockReset()
    updateMaeLedgerEntryValuesMock.mockReset()
    deleteMaeLedgerEntryMock.mockReset()
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

  it('lists the Create Entry fields in Date, Currency, Description, Note, Value order', async () => {
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Entry' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Entry' }))

    const fieldLabels = screen
      .getAllByText(/^(Date|Currency|Description|Note|Value)$/, { selector: 'label' })
      .map((label) => label.textContent)
    expect(fieldLabels).toEqual(['Date', 'Currency', 'Description', 'Note', 'Value'])
  })

  it('edits an entry values via the toggled panel and saves, updating the displayed row', async () => {
    updateMaeLedgerEntryValuesMock.mockResolvedValue({ ...ENTRIES[0], brlValue: 355, gbpValue: 51.6 })
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit entry' }))
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

  it('deletes an entry after confirming the prompt', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    deleteMaeLedgerEntryMock.mockResolvedValue(undefined)
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())

    getMaeLedgerEntriesFromDateMock.mockResolvedValue([])
    fireEvent.click(screen.getByRole('button', { name: 'Delete entry' }))

    await waitFor(() => expect(deleteMaeLedgerEntryMock).toHaveBeenCalledWith('e1'))
    await waitFor(() => expect(screen.queryByText('School supplies')).not.toBeInTheDocument())
  })

  it('does not delete when the confirmation prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('School supplies')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete entry' }))

    expect(deleteMaeLedgerEntryMock).not.toHaveBeenCalled()
  })

  it('sorts entries by clicking the Description column header, keeping the totals row fixed', async () => {
    const entries: MaeLedgerEntryDto[] = [
      { id: 'e1', date: '2026-07-15', description: 'Zebra item', note: '', sourceCurrency: 'BRL', brlValue: 350, gbpValue: 51.1 },
      { id: 'e2', date: '2026-07-20', description: 'Apple item', note: '', sourceCurrency: 'BRL', brlValue: 100, gbpValue: 20 },
    ]
    getMaeLedgerEntriesFromDateMock.mockResolvedValue(entries)

    const { container } = render(<ControleMaePage />)

    await waitFor(() => expect(screen.getByText('Zebra item')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Description' }))

    const dataRows = container.querySelectorAll('.controle-mae-page__section tbody tr')
    expect(dataRows).toHaveLength(2)
    expect(dataRows[0]).toHaveTextContent('Apple item')
    expect(dataRows[1]).toHaveTextContent('Zebra item')

    const totalsRow = container.querySelector('.controle-mae-page__totals-row')
    expect(totalsRow).toHaveTextContent('5,000.00')
    expect(totalsRow).toHaveTextContent('720.45')
  })
})
