import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AggregatedSummaryData } from '../../hooks/useAggregatedSummary'
import type { PortfolioAssetSummaryData, RowPriceState } from '../../hooks/usePortfolioAssetSummary'
import type { AggregatedSummaryDto, PortfolioAssetSummaryItemDto } from '../../api/types'
import { SelectedNodeProvider } from '../../context/SelectedNodeContext'
import PortfolioSummaryTab from '../PortfolioSummaryTab'

function renderComponent() {
  return render(
    <SelectedNodeProvider>
      <PortfolioSummaryTab />
    </SelectedNodeProvider>,
  )
}

const mockAggregatedRetry = vi.fn()
const mockPortfolioRetry = vi.fn()
const xirrMock = vi.hoisted(() => vi.fn<(cashFlows: { date: Date; amount: number }[]) => number | null>())

const mockAggregatedHookValue: AggregatedSummaryData = {
  summary: null,
  isLoading: false,
  error: null,
  retry: mockAggregatedRetry,
}

const mockPortfolioHookValue: PortfolioAssetSummaryData = {
  items: null,
  rowPrices: [],
  isLoading: false,
  error: null,
  retry: mockPortfolioRetry,
}

vi.mock('../../hooks/useAggregatedSummary', () => ({
  useAggregatedSummary: () => mockAggregatedHookValue,
}))

vi.mock('../../hooks/usePortfolioAssetSummary', () => ({
  usePortfolioAssetSummary: () => mockPortfolioHookValue,
}))

vi.mock('../../utils/xirr', () => ({
  xirr: xirrMock,
}))

const SUMMARY: AggregatedSummaryDto = {
  totalBought: 15420.5,
  totalSold: 3200.0,
  totalCredits: 842.3,
  totalInvested: 12220.5,
}

const ITEM_1: PortfolioAssetSummaryItemDto = {
  assetName: 'ALZR11',
  ticker: 'ALZR11',
  exchange: 'BVMF',
  class: 'RealEstate',
  firstInvestmentDate: '2021-03-01T00:00:00',
  currentQuantity: 25,
  averagePrice: 100,
  totalBought: 2500,
  totalSold: 0,
  totalInvested: 2500,
  portfolioWeight: 23.4,
  totalCredits: 0,
  cashFlows: [],
  lastMonthCredits: 0,
  lastCreditMonth: null,
  lastMonthCreditsPercent: null,
  creditFrequencyPerYear: null,
  estimatedAnnualCredits: null,
  estimatedAnnualPercent: null,
  currentMonthCredits: 0,
}

const LOADING_ROW_PRICE: RowPriceState = { isLoading: true, currentPrice: null, fetchFailed: false }
const FAILED_ROW_PRICE: RowPriceState = { isLoading: false, currentPrice: null, fetchFailed: true }
const IDLE_ROW_PRICE: RowPriceState = { isLoading: false, currentPrice: null, fetchFailed: false }

function setAggregatedMock(overrides: Partial<AggregatedSummaryData>) {
  Object.assign(mockAggregatedHookValue, overrides)
}

function setPortfolioMock(overrides: Partial<PortfolioAssetSummaryData>) {
  Object.assign(mockPortfolioHookValue, overrides)
}

describe('PortfolioSummaryTab', () => {
  beforeEach(() => {
    mockAggregatedRetry.mockReset()
    mockPortfolioRetry.mockReset()
    xirrMock.mockReset()
    xirrMock.mockReturnValue(null)
    Object.assign(mockAggregatedHookValue, {
      summary: null,
      isLoading: false,
      error: null,
    })
    Object.assign(mockPortfolioHookValue, {
      items: null,
      rowPrices: [],
      isLoading: false,
      error: null,
    })
  })

  it('renders_loading_state_in_totals_section_while_aggregated_summary_loads', () => {
    setAggregatedMock({ isLoading: true })
    renderComponent()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_in_totals_section_on_aggregated_summary_failure', () => {
    setAggregatedMock({ error: 'Unable to load summary' })
    renderComponent()
    expect(screen.getByText('Unable to load summary')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_total_invested_for_portfolio_node_selection', () => {
    setAggregatedMock({ summary: SUMMARY })
    renderComponent()
    const labels = screen.getAllByText(/^Total (Bought|Sold|Credits|Invested)$/, { selector: 'span.aggregated-summary__label' })
    expect(labels.map((el) => el.textContent)).toEqual(['Total Bought', 'Total Sold', 'Total Credits', 'Total Invested'])
  })

  it('renders_loading_state_in_table_section_while_items_load', () => {
    setPortfolioMock({ isLoading: true })
    renderComponent()
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_in_table_section_on_items_fetch_failure', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ error: 'Unable to load portfolio assets' })
    renderComponent()
    expect(screen.getByText('Unable to load portfolio assets')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
  })

  it('renders_table_with_correct_column_headers', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('Asset Name')).toBeInTheDocument()
    expect(screen.getByText('First Investment')).toBeInTheDocument()
    expect(screen.getByText('Quantity')).toBeInTheDocument()
    expect(screen.getAllByText('Total Invested').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('% Portfolio')).toBeInTheDocument()
    expect(screen.getAllByText('Total Credits').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('Current Value')).toBeInTheDocument()
    expect(screen.getByText('Average Price')).toBeInTheDocument()
    expect(screen.getByText('Current Price')).toBeInTheDocument()
    expect(screen.getByText('Profit')).toBeInTheDocument()
    expect(screen.getAllByText('%').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('w/ Credits')).toBeInTheDocument()
    expect(screen.getByText('XIRR')).toBeInTheDocument()
  })

  it('renders_asset_row_with_correctly_formatted_values', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalCredits: 75.5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('ALZR11')).toBeInTheDocument()
    expect(screen.getByText('01/03/2021')).toBeInTheDocument()
    expect(screen.getByText(/23\.4%/)).toBeInTheDocument()
    expect(screen.getByText(/75[.,]50/)).toBeInTheDocument()
  })

  it('renders_average_price_with_formatted_value', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, averagePrice: 123.456 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/123[.,]46/)).toBeInTheDocument()
  })

  it('renders_total_credits_immediately_before_price_resolves', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalCredits: 75.5 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/75[.,]50/)).toBeInTheDocument()
    const loadingCells = screen.getAllByText('...')
    expect(loadingCells.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_per_cell_loading_indicator_while_price_loads', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const loadingCells = screen.getAllByText('...')
    expect(loadingCells).toHaveLength(5)
  })

  it('renders_current_value_when_price_resolves', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/262[.,]50/)).toBeInTheDocument()
  })

  it('renders_current_price_when_price_resolves', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/10[.,]50/)).toBeInTheDocument()
  })

  it('renders_dash_in_current_price_when_price_fetch_fails', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [FAILED_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(5)
  })

  it('renders_correct_profit_percent', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 25, totalInvested: 250 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    // Both % Profit and % Profit w/ Credits show 5.00% when totalCredits is 0
    const profitElements = screen.getAllByText(/5[.,]00%/)
    expect(profitElements.length).toBeGreaterThanOrEqual(1)
    expect(profitElements[0]).toHaveClass('portfolio-summary__profit--green')
  })

  it('renders_correct_profit_with_credits_percent', () => {
    // currentValue = 10.5 * 25 = 262.50
    // profitWithCreditsPercent = (262.50 + 12.50 - 250) / 250 * 100 = 10.00%
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      currentQuantity: 25,
      totalInvested: 250,
      totalCredits: 12.5,
    }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/10[.,]00%/)).toBeInTheDocument()
  })

  it('renders_xirr_when_price_resolves', () => {
    xirrMock.mockReturnValue(0.1234)
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    expect(screen.getByText(/12[.,]34%/)).toBeInTheDocument()
    expect(xirrMock).toHaveBeenCalledWith(
      expect.arrayContaining([expect.objectContaining({ amount: 262.5 })]),
    )
  })

  it('renders_dash_in_current_value_and_price_dependent_columns_on_price_failure', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [FAILED_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(5)
  })

  it('renders_dash_in_profit_when_total_invested_is_zero', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalInvested: 0 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(2) // % Profit and % Profit w/ Credits
    expect(screen.getByText(/262[.,]50/)).toBeInTheDocument()
  })

  it('renders_dash_in_xirr_when_cash_flows_fewer_than_two_entries', () => {
    // cashFlows: [] + terminal entry = 1 entry → xirr returns null → XIRR shows "—"
    xirrMock.mockReturnValue(null)
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('applies_green_class_to_positive_profit', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 25, totalInvested: 200 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    const profitEl = document.querySelector('.portfolio-summary__profit--green')
    expect(profitEl).toBeInTheDocument()
  })

  it('applies_red_class_to_negative_profit', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 25, totalInvested: 300 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    const profitEl = document.querySelector('.portfolio-summary__profit--red')
    expect(profitEl).toBeInTheDocument()
  })

  it('applies_green_class_to_positive_profit_with_credits', () => {
    // currentValue = 10.5 * 25 = 262.50; totalInvested = 300; totalCredits = 50
    // profitWithCreditsPercent = (262.50 + 50 - 300) / 300 * 100 = 4.17% (positive)
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      currentQuantity: 25,
      totalInvested: 300,
      totalCredits: 50,
    }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    const greenEls = document.querySelectorAll('.portfolio-summary__profit--green')
    expect(greenEls.length).toBeGreaterThanOrEqual(1)
  })

  it('applies_red_class_to_negative_profit_with_credits', () => {
    // currentValue = 10.5 * 25 = 262.50; totalInvested = 400; totalCredits = 10
    // profitWithCreditsPercent = (262.50 + 10 - 400) / 400 * 100 = -31.875% (negative)
    const item: PortfolioAssetSummaryItemDto = {
      ...ITEM_1,
      currentQuantity: 25,
      totalInvested: 400,
      totalCredits: 10,
    }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    renderComponent()
    const redEls = document.querySelectorAll('.portfolio-summary__profit--red')
    expect(redEls.length).toBeGreaterThanOrEqual(1)
  })

  it('applies_green_class_to_positive_xirr', () => {
    xirrMock.mockReturnValue(0.1234)
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    const greenEls = document.querySelectorAll('.portfolio-summary__profit--green')
    expect(greenEls.length).toBeGreaterThanOrEqual(1)
  })

  it('applies_red_class_to_negative_xirr', () => {
    xirrMock.mockReturnValue(-0.05)
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    renderComponent()
    const redEls = document.querySelectorAll('.portfolio-summary__profit--red')
    expect(redEls.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_empty_string_for_null_first_investment_date', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, firstInvestmentDate: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    renderComponent()
    expect(screen.queryByText('01/03/2021')).not.toBeInTheDocument()
  })

  it('totals_section_is_unaffected_when_table_section_errors', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ error: 'F01 fetch failed' })
    renderComponent()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
    expect(screen.getByText('Total Sold')).toBeInTheDocument()
    expect(screen.getByText('Total Credits')).toBeInTheDocument()
    expect(screen.getByText('F01 fetch failed')).toBeInTheDocument()
  })

  // ── P03-F02: Credits Analysis Columns ─────────────────────────────────────

  it('renders_grouped_credits_analysis_column_headers_after_xirr', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('Last Month')).toBeInTheDocument()
    expect(screen.getByText('Month')).toBeInTheDocument()
    expect(screen.getByText('Est. Annual')).toBeInTheDocument()
    // "Credits" sub-header appears under both the Last Month and Est. Annual groups
    expect(screen.getAllByText('Credits').length).toBeGreaterThanOrEqual(2)
    // "%" sub-header appears under Profit, Last Month, and Est. Annual groups
    expect(screen.getAllByText('%').length).toBeGreaterThanOrEqual(3)
  })

  it('renders_last_month_credits_with_formatted_value', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06' }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/12[.,]50/)).toBeInTheDocument()
  })

  it('renders_last_month_credits_as_dash_when_no_credits', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 0, lastCreditMonth: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_last_credit_month_in_mmm_yyyy_format', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06' }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText('Jun 2026')).toBeInTheDocument()
  })

  it('renders_last_credit_month_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastCreditMonth: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_last_month_percent_with_percent_suffix', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06', lastMonthCreditsPercent: 1.25 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/1[.,]25%/)).toBeInTheDocument()
  })

  it('renders_last_month_percent_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCreditsPercent: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_estimated_annual_credits_with_formatted_value', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06', estimatedAnnualCredits: 150.00 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/150[.,]00/)).toBeInTheDocument()
  })

  it('renders_estimated_annual_credits_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualCredits: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_estimated_annual_percent_with_percent_suffix', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, lastMonthCredits: 12.50, lastCreditMonth: '2026-06', estimatedAnnualCredits: 150.00, estimatedAnnualPercent: 6.00 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByText(/6[.,]00%/)).toBeInTheDocument()
  })

  it('renders_estimated_annual_percent_as_dash_when_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualPercent: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_credits_separator_class_on_last_month_group_header', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    const header = screen.getByText('Last Month')
    expect(header).toHaveClass('portfolio-summary__credits-separator')
  })

  // ── P03-F02: Footer Panel ──────────────────────────────────────────────────

  it('renders_footer_with_total_invested_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', totalInvested: 2000, totalCredits: 0 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/3[,.]000[.,]00/)).toBeInTheDocument()
  })

  it('renders_footer_with_total_credits_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalCredits: 50, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', totalCredits: 75, totalInvested: 2000 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/125[.,]00/)).toBeInTheDocument()
  })

  it('renders_footer_credits_label_with_current_month_and_year', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-07-15'))
    try {
      setAggregatedMock({ summary: SUMMARY })
      setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
      renderComponent()
      expect(screen.getByText('Credits Jul 2026')).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })

  it('renders_footer_current_month_credits_sum', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentMonthCredits: 10, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', currentMonthCredits: 20, totalInvested: 2000 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/30[.,]00/)).toBeInTheDocument()
  })

  it('renders_footer_estimated_annual_credits_sum_of_non_null', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualCredits: 600, totalInvested: 1000 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', estimatedAnnualCredits: null, totalInvested: 2000 }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [LOADING_ROW_PRICE, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/600[.,]00/)).toBeInTheDocument()
  })

  it('renders_footer_estimated_annual_credits_as_dash_when_all_null', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, estimatedAnnualCredits: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue('—')).toBeInTheDocument()
  })

  it('renders_footer_current_value_as_calculating_when_all_prices_pending', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue('Calculating…')).toBeInTheDocument()
  })

  it('renders_footer_current_value_as_partial_sum_with_asterisk_while_prices_loading', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 5 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11' }
    const resolvedPrice: RowPriceState = { isLoading: false, currentPrice: 10, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [resolvedPrice, LOADING_ROW_PRICE] })
    renderComponent()
    expect(screen.getByDisplayValue(/50[.,]00 \*/)).toBeInTheDocument()
    expect(screen.getByText('excludes assets with pending prices')).toBeInTheDocument()
  })

  it('renders_footer_current_value_as_clean_sum_when_all_prices_resolved', () => {
    const item1: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 5 }
    const item2: PortfolioAssetSummaryItemDto = { ...ITEM_1, assetName: 'MXRF11', currentQuantity: 10 }
    const price1: RowPriceState = { isLoading: false, currentPrice: 10, fetchFailed: false }
    const price2: RowPriceState = { isLoading: false, currentPrice: 5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item1, item2], rowPrices: [price1, price2] })
    renderComponent()
    expect(screen.getByDisplayValue(/100[.,]00/)).toBeInTheDocument()
    expect(screen.queryByText('excludes assets with pending prices')).not.toBeInTheDocument()
  })

  it('footer_panel_is_not_inside_table_element', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    renderComponent()
    expect(document.querySelector('.portfolio-summary__footer')).not.toBeNull()
    expect(document.querySelector('table tfoot')).toBeNull()
  })
})
