import { fireEvent, render, screen, waitFor } from '../../test/renderWithFluent'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TaxWorkbookDto, TaxWorkbookOptionDto } from '../../api/types'
import { never } from '../../test-utils/deferred'
import TaxPage from '../TaxPage'

const { getTaxWorkbookOptionsMock, getTaxWorkbookMock, downloadCsvMock } = vi.hoisted(() => ({
  getTaxWorkbookOptionsMock: vi.fn<FinancialApiClient['getTaxWorkbookOptions']>(),
  getTaxWorkbookMock: vi.fn<FinancialApiClient['getTaxWorkbook']>(),
  downloadCsvMock: vi.fn(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getTaxWorkbookOptions: getTaxWorkbookOptionsMock,
    getTaxWorkbook: getTaxWorkbookMock,
  } as Partial<FinancialApiClient>,
}))

vi.mock('../../utils/taxWorkbookCsv', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../../utils/taxWorkbookCsv')>()),
  downloadCsv: downloadCsvMock,
}))

const OPTIONS: TaxWorkbookOptionDto[] = [
  { jurisdiction: 'BR', taxYear: '2025' },
  { jurisdiction: 'BR', taxYear: '2026' },
  { jurisdiction: 'UK', taxYear: '2025/26' },
]

const WORKBOOK: TaxWorkbookDto = {
  jurisdiction: 'BR',
  taxYear: '2025',
  entries: [
    {
      id: 'e1',
      date: '2025-06-01',
      eventCategory: 'Dividend',
      proceeds: null,
      costBasis: null,
      gainLoss: null,
      grossAmount: 100,
      withheldAmount: 10,
      netAmount: 90,
      calculationStatus: 'Final',
      evidenceReference: 'ev-1',
      taxRuleLabel: 'BR dividend rule',
    },
    {
      id: 'e2',
      date: '2025-07-01',
      eventCategory: 'CapitalGain',
      proceeds: 500,
      costBasis: 400,
      gainLoss: 100,
      grossAmount: null,
      withheldAmount: null,
      netAmount: null,
      calculationStatus: 'Incomplete',
      evidenceReference: 'ev-2',
      taxRuleLabel: null,
    },
    {
      id: 'e3',
      date: '2025-08-01',
      eventCategory: 'Interest',
      proceeds: null,
      costBasis: null,
      gainLoss: null,
      grossAmount: 20,
      withheldAmount: 2,
      netAmount: 18,
      calculationStatus: 'RequiresReview',
      evidenceReference: 'ev-3',
      taxRuleLabel: null,
    },
  ],
  categoryTotals: [
    {
      eventCategory: 'Dividend',
      totalProceeds: null,
      totalCostBasis: null,
      totalGainLoss: null,
      totalGrossAmount: 100,
      totalWithheldAmount: 10,
      totalNetAmount: 90,
    },
  ],
  calculationStatus: 'RequiresReview',
}

const EMPTY_WORKBOOK: TaxWorkbookDto = { ...WORKBOOK, entries: [], categoryTotals: [] }

async function renderLoaded() {
  render(<TaxPage />)
  await screen.findByText('ev-1')
}

describe('TaxPage', () => {
  beforeEach(() => {
    getTaxWorkbookOptionsMock.mockReset().mockResolvedValue(OPTIONS)
    getTaxWorkbookMock.mockReset().mockResolvedValue(WORKBOOK)
    downloadCsvMock.mockReset()
  })

  it('renders_loading_indicator_while_options_load', () => {
    getTaxWorkbookOptionsMock.mockReturnValue(never())
    render(<TaxPage />)
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('renders_error_state_and_retry_loads_the_tax_years', async () => {
    getTaxWorkbookOptionsMock.mockRejectedValueOnce(new Error('Unable to load tax years.'))
    render(<TaxPage />)

    expect(await screen.findByText('Unable to load tax years.')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('ev-1')).toBeInTheDocument()
    expect(getTaxWorkbookOptionsMock).toHaveBeenCalledTimes(2)
  })

  it('renders_explanatory_empty_state_when_no_jurisdictions_exist', async () => {
    getTaxWorkbookOptionsMock.mockResolvedValue([])
    render(<TaxPage />)
    expect(await screen.findByText(/No classified disposals or income events yet/)).toBeInTheDocument()
  })

  it('selecting_a_jurisdiction_loads_its_first_tax_year', async () => {
    await renderLoaded()
    fireEvent.change(screen.getByLabelText('Jurisdiction'), { target: { value: 'UK' } })
    await waitFor(() => expect(getTaxWorkbookMock).toHaveBeenLastCalledWith('UK', '2025/26'))
  })

  it('selecting_a_tax_year_loads_that_year', async () => {
    await renderLoaded()
    fireEvent.change(screen.getByLabelText('Tax year'), { target: { value: '2026' } })
    await waitFor(() => expect(getTaxWorkbookMock).toHaveBeenLastCalledWith('BR', '2026'))
  })

  it('renders_every_entry_with_its_amounts_and_evidence_reference', async () => {
    await renderLoaded()
    expect(screen.getAllByText('90.00').length).toBeGreaterThan(0)
    expect(screen.getAllByText('100.00').length).toBeGreaterThan(0)
    expect(screen.getByText('ev-2')).toBeInTheDocument()
    expect(screen.getByText('ev-3')).toBeInTheDocument()
  })

  it('shows_no_calculation_status_on_entries_or_the_workbook', async () => {
    await renderLoaded()
    expect(screen.queryByRole('columnheader', { name: 'Status' })).not.toBeInTheDocument()
    expect(screen.queryByText('Incomplete')).not.toBeInTheDocument()
    expect(screen.queryByText(/Requires review/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Overall status/)).not.toBeInTheDocument()
  })

  it('export_csv_is_disabled_and_a_specific_empty_state_shows_when_the_selection_has_no_entries', async () => {
    getTaxWorkbookMock.mockResolvedValue(EMPTY_WORKBOOK)
    render(<TaxPage />)
    expect(await screen.findByText(/No classified events for BR 2025 yet/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Export CSV' })).toBeDisabled()
  })

  it('export_csv_downloads_the_selected_workbook', async () => {
    await renderLoaded()
    fireEvent.click(screen.getByRole('button', { name: 'Export CSV' }))
    expect(downloadCsvMock).toHaveBeenCalledWith('tax-workbook-BR-2025.csv', expect.stringContaining('2025-06-01,BR,2025,Dividend'))
  })

  it('renders_loading_indicator_while_the_workbook_loads', async () => {
    getTaxWorkbookMock.mockReturnValue(never())
    render(<TaxPage />)
    await waitFor(() => expect(getTaxWorkbookMock).toHaveBeenCalled())
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('renders_error_state_and_retry_loads_the_workbook', async () => {
    getTaxWorkbookMock.mockRejectedValueOnce(new Error('Unable to load the tax workbook.'))
    render(<TaxPage />)

    expect(await screen.findByText('Unable to load the tax workbook.')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('ev-1')).toBeInTheDocument()
  })

  it('renders_category_totals', async () => {
    await renderLoaded()
    expect(screen.getByText('Category totals')).toBeInTheDocument()
    expect(screen.getAllByText('Dividend').length).toBeGreaterThan(0)
  })
})
