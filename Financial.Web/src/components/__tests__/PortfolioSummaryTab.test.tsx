import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { AggregatedSummaryData } from '../../hooks/useAggregatedSummary'
import type { PortfolioAssetSummaryData, RowPriceState } from '../../hooks/usePortfolioAssetSummary'
import type { AggregatedSummaryDto, PortfolioAssetSummaryItemDto } from '../../api/types'
import PortfolioSummaryTab from '../PortfolioSummaryTab'

const mockAggregatedRetry = vi.fn()
const mockPortfolioRetry = vi.fn()

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

const SUMMARY: AggregatedSummaryDto = {
  totalBought: 15420.5,
  totalSold: 3200.0,
  totalCredits: 842.3,
}

const ITEM_1: PortfolioAssetSummaryItemDto = {
  assetName: 'ALZR11',
  ticker: 'ALZR11',
  exchange: 'BVMF',
  firstInvestmentDate: '2021-03-01T00:00:00',
  currentQuantity: 25,
  totalBought: 2500,
  totalSold: 0,
  totalInvested: 2500,
  portfolioWeight: 23.4,
  totalCredits: 0,
  cashFlows: [],
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
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_in_totals_section_on_aggregated_summary_failure', () => {
    setAggregatedMock({ error: 'Unable to load summary' })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('Unable to load summary')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders_loading_state_in_table_section_while_items_load', () => {
    setPortfolioMock({ isLoading: true })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_error_state_in_table_section_on_items_fetch_failure', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ error: 'Unable to load portfolio assets' })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('Unable to load portfolio assets')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
  })

  it('renders_table_with_correct_column_headers', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('Asset Name')).toBeInTheDocument()
    expect(screen.getByText('First Investment')).toBeInTheDocument()
    expect(screen.getByText('Quantity')).toBeInTheDocument()
    expect(screen.getByText('Total Invested')).toBeInTheDocument()
    expect(screen.getByText('% Portfolio')).toBeInTheDocument()
    expect(screen.getByText('Current Value')).toBeInTheDocument()
    expect(screen.getByText('% Profit')).toBeInTheDocument()
  })

  it('renders_asset_row_with_correctly_formatted_values', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [IDLE_ROW_PRICE] })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('ALZR11')).toBeInTheDocument()
    expect(screen.getByText('01/03/2021')).toBeInTheDocument()
    expect(screen.getByText(/23\.4%/)).toBeInTheDocument()
  })

  it('renders_per_cell_loading_indicator_while_price_loads', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [LOADING_ROW_PRICE] })
    render(<PortfolioSummaryTab />)
    const loadingCells = screen.getAllByText('...')
    expect(loadingCells).toHaveLength(4)
  })

  it('renders_current_value_when_price_resolves', () => {
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [rowPrice] })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText(/262[.,]50/)).toBeInTheDocument()
  })

  it('renders_correct_profit_percent', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 25, totalInvested: 250 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    render(<PortfolioSummaryTab />)
    // Both % Profit and % Profit w/ Credits show 5.00% when totalCredits is 0
    const profitElements = screen.getAllByText(/5[.,]00%/)
    expect(profitElements.length).toBeGreaterThanOrEqual(1)
    expect(profitElements[0]).toHaveClass('portfolio-summary__profit--green')
  })

  it('renders_dash_in_current_value_and_profit_on_price_failure', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [ITEM_1], rowPrices: [FAILED_ROW_PRICE] })
    render(<PortfolioSummaryTab />)
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(2)
  })

  it('renders_dash_in_profit_when_total_invested_is_zero', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, totalInvested: 0 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    render(<PortfolioSummaryTab />)
    const dashes = screen.getAllByText('—')
    expect(dashes.length).toBeGreaterThanOrEqual(2) // % Profit and % Profit w/ Credits
    expect(screen.getByText(/262[.,]50/)).toBeInTheDocument()
  })

  it('applies_green_class_to_positive_profit', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 25, totalInvested: 200 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    render(<PortfolioSummaryTab />)
    const profitEl = document.querySelector('.portfolio-summary__profit--green')
    expect(profitEl).toBeInTheDocument()
  })

  it('applies_red_class_to_negative_profit', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, currentQuantity: 25, totalInvested: 300 }
    const rowPrice: RowPriceState = { isLoading: false, currentPrice: 10.5, fetchFailed: false }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [rowPrice] })
    render(<PortfolioSummaryTab />)
    const profitEl = document.querySelector('.portfolio-summary__profit--red')
    expect(profitEl).toBeInTheDocument()
  })

  it('renders_empty_string_for_null_first_investment_date', () => {
    const item: PortfolioAssetSummaryItemDto = { ...ITEM_1, firstInvestmentDate: null }
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ items: [item], rowPrices: [IDLE_ROW_PRICE] })
    render(<PortfolioSummaryTab />)
    expect(screen.queryByText('01/03/2021')).not.toBeInTheDocument()
  })

  it('totals_section_is_unaffected_when_table_section_errors', () => {
    setAggregatedMock({ summary: SUMMARY })
    setPortfolioMock({ error: 'F01 fetch failed' })
    render(<PortfolioSummaryTab />)
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
    expect(screen.getByText('Total Sold')).toBeInTheDocument()
    expect(screen.getByText('Total Credits')).toBeInTheDocument()
    expect(screen.getByText('F01 fetch failed')).toBeInTheDocument()
  })
})
