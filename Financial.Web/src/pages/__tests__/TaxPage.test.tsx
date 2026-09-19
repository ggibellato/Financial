import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { TaxWorkbookData } from '../../hooks/useTaxWorkbook'
import type { TaxWorkbookDto } from '../../api/types'
import TaxPage from '../TaxPage'

const mockSelectJurisdiction = vi.fn()
const mockSelectTaxYear = vi.fn()
const mockRetryOptions = vi.fn()
const mockRetryWorkbook = vi.fn()
const mockExportCsv = vi.fn()

const WORKBOOK: TaxWorkbookDto = {
  jurisdiction: 'BR',
  taxYear: '2026',
  entries: [
    {
      id: 'e1',
      date: '2026-06-01',
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
      date: '2026-07-01',
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
      date: '2026-08-01',
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

const mockHookValue: TaxWorkbookData = {
  options: [
    { jurisdiction: 'BR', taxYear: '2026' },
    { jurisdiction: 'UK', taxYear: '2025/26' },
  ],
  isLoadingOptions: false,
  optionsError: null,
  retryOptions: mockRetryOptions,
  jurisdictions: ['BR', 'UK'],
  taxYearsForJurisdiction: ['2026'],
  jurisdiction: 'BR',
  taxYear: '2026',
  selectJurisdiction: mockSelectJurisdiction,
  selectTaxYear: mockSelectTaxYear,
  workbook: WORKBOOK,
  isLoadingWorkbook: false,
  workbookError: null,
  retryWorkbook: mockRetryWorkbook,
  canExportCsv: true,
  exportCsv: mockExportCsv,
}

vi.mock('../../hooks/useTaxWorkbook', () => ({
  useTaxWorkbook: () => mockHookValue,
}))

function setMock(overrides: Partial<TaxWorkbookData>) {
  Object.assign(mockHookValue, overrides)
}

describe('TaxPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setMock({
      options: [
        { jurisdiction: 'BR', taxYear: '2026' },
        { jurisdiction: 'UK', taxYear: '2025/26' },
      ],
      isLoadingOptions: false,
      optionsError: null,
      jurisdictions: ['BR', 'UK'],
      taxYearsForJurisdiction: ['2026'],
      jurisdiction: 'BR',
      taxYear: '2026',
      workbook: WORKBOOK,
      isLoadingWorkbook: false,
      workbookError: null,
      canExportCsv: true,
    })
  })

  it('renders_loading_indicator_while_options_load', () => {
    setMock({ isLoadingOptions: true })
    render(<TaxPage />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_options_failure', () => {
    setMock({ optionsError: 'Unable to load tax years' })
    render(<TaxPage />)
    expect(screen.getByText('Unable to load tax years')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(mockRetryOptions).toHaveBeenCalled()
  })

  it('renders_explanatory_empty_state_when_no_jurisdictions_exist', () => {
    setMock({ jurisdictions: [] })
    render(<TaxPage />)
    expect(screen.getByText(/No classified disposals or income events yet/)).toBeInTheDocument()
  })

  it('selecting_a_jurisdiction_calls_the_hook', () => {
    render(<TaxPage />)
    fireEvent.change(screen.getByLabelText('Jurisdiction'), { target: { value: 'UK' } })
    expect(mockSelectJurisdiction).toHaveBeenCalledWith('UK')
  })

  it('selecting_a_tax_year_calls_the_hook', () => {
    setMock({ taxYearsForJurisdiction: ['2025', '2026'] })
    render(<TaxPage />)
    fireEvent.change(screen.getByLabelText('Tax Year'), { target: { value: '2025' } })
    expect(mockSelectTaxYear).toHaveBeenCalledWith('2025')
  })

  it('renders_every_entry_with_its_amounts_and_evidence_reference', () => {
    render(<TaxPage />)
    expect(screen.getAllByText('90.00').length).toBeGreaterThan(0)
    expect(screen.getAllByText('100.00').length).toBeGreaterThan(0)
    expect(screen.getByText('ev-1')).toBeInTheDocument()
    expect(screen.getByText('ev-2')).toBeInTheDocument()
  })

  it('renders_each_of_the_3_calculation_statuses_with_a_distinct_indicator', () => {
    render(<TaxPage />)
    expect(screen.getByText('Final')).toBeInTheDocument()
    expect(screen.getByText('Incomplete')).toBeInTheDocument()
    expect(screen.getAllByText('Requires review').length).toBeGreaterThan(0)
  })

  it('renders_the_aggregate_status_indicator', () => {
    render(<TaxPage />)
    expect(screen.getByText('Overall status:')).toBeInTheDocument()
  })

  it('export_csv_is_disabled_when_the_selected_workbook_has_zero_entries', () => {
    setMock({ workbook: { ...WORKBOOK, entries: [] }, canExportCsv: false })
    render(<TaxPage />)
    expect(screen.getByRole('button', { name: 'Export CSV' })).toBeDisabled()
  })

  it('export_csv_triggers_the_hooks_export_when_enabled', () => {
    render(<TaxPage />)
    fireEvent.click(screen.getByRole('button', { name: 'Export CSV' }))
    expect(mockExportCsv).toHaveBeenCalled()
  })

  it('renders_a_workbook_specific_empty_state_when_the_selection_has_no_entries', () => {
    setMock({ workbook: { ...WORKBOOK, entries: [] }, canExportCsv: false })
    render(<TaxPage />)
    expect(screen.getByText(/No classified events for BR 2026 yet/)).toBeInTheDocument()
  })

  it('renders_loading_indicator_while_the_workbook_loads', () => {
    setMock({ isLoadingWorkbook: true, workbook: null })
    render(<TaxPage />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_with_retry_on_workbook_failure', () => {
    setMock({ workbookError: 'Unable to load the tax workbook', workbook: null })
    render(<TaxPage />)
    expect(screen.getByText('Unable to load the tax workbook')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(mockRetryWorkbook).toHaveBeenCalled()
  })

  it('renders_category_totals', () => {
    render(<TaxPage />)
    expect(screen.getByText('Category Totals')).toBeInTheDocument()
    expect(screen.getAllByText('Dividend').length).toBeGreaterThan(0)
  })
})
