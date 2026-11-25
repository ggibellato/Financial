import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerHoldingsDto, HoldingRowDto, HoldingsTotalsDto, PortfolioAssetSummaryItemDto, SelectedNode } from '../../api/types'
import { renderWithSelectedNode } from '../../test-utils/renderWithSelectedNode'
import BrokerHoldingsTab from '../BrokerHoldingsTab'

const { getBrokerHoldingsMock } = vi.hoisted(() => ({
  getBrokerHoldingsMock: vi.fn<FinancialApiClient['getBrokerHoldings']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getBrokerHoldings: getBrokerHoldingsMock } as Partial<FinancialApiClient>,
}))

const BROKER_NODE: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' }

const ASSET: PortfolioAssetSummaryItemDto = {
  assetName: 'BBBB11',
  ticker: 'BBBB11',
  exchange: 'BVMF',
  class: 'RealEstate',
  firstInvestmentDate: '2021-06-14T00:00:00',
  currentQuantity: 20,
  averagePrice: 100,
  averageSellPrice: null,
  totalBought: 2000,
  totalSold: 0,
  totalInvested: 2000,
  realisedGainLoss: 0,
  realisedGainLossSharesOnly: 0,
  portfolioWeight: 50,
  investedWeight: 50,
  marketValue: 1800,
  costOfUnitsHeld: 2000,
  unrealisedGain: -200,
  resultPercent: -10,
  resultWithCreditsPercent: 40,
  priceAsOfDate: '2026-11-24',
  marketStatus: 'Current',
  priceOnlyReturn: null,
  totalReturn: null,
  totalReturnNetOfTax: null,
  totalCredits: 1000,
  lastMonthCredits: 18,
  lastCreditMonth: '2026-10',
  lastMonthCreditsPercent: 0.9,
  creditFrequencyPerYear: 12,
  estimatedAnnualCredits: 216,
  estimatedAnnualPercent: 10.8,
  currentMonthCredits: 0,
  creditsPercentOfInvested: 50,
  monthsHeld: 65,
  monthlyYieldPercent: 50 / 65,
  aprPercent: (50 / 65) * 12,
  lastCreditPercentOfMarketValue: 1,
}

const TOTALS: HoldingsTotalsDto = {
  holdingCount: 2,
  unvaluedHoldingCount: 0,
  invested: 4000,
  marketValue: 3900,
  investedShareOfBase: 40,
  marketShareOfBase: 39,
  profitAmount: -100,
  profitPercent: -2.5,
  profitWithCreditsPercent: 22.5,
  totalCredits: 1000,
  creditsPercentOfInvested: 25,
  lastCreditAmount: 18,
  lastCreditPercentOfMarketValue: 0.46,
}

function row(asset: Partial<PortfolioAssetSummaryItemDto>, shares: Partial<HoldingRowDto> = {}): HoldingRowDto {
  return {
    asset: { ...ASSET, ...asset },
    investedShareOfGroup: 25,
    marketShareOfGroup: 30,
    investedShareOfBase: 10,
    marketShareOfBase: 12,
    ...shares,
  }
}

const HOLDINGS: BrokerHoldingsDto = {
  currency: 'BRL',
  groups: [
    {
      name: 'Acoes',
      holdings: [row({ assetName: 'CCCC3', marketValue: null, unrealisedGain: null, resultPercent: null }, { investedShareOfGroup: null, marketShareOfGroup: null })],
      subtotal: { ...TOTALS, holdingCount: 1, unvaluedHoldingCount: 1, marketValue: null, profitAmount: null },
    },
    {
      name: 'FII',
      holdings: [row({ assetName: 'BBBB11', totalInvested: 1000 }), row({ assetName: 'AAAA11', totalInvested: 3000 })],
      subtotal: TOTALS,
    },
  ],
  total: { ...TOTALS, holdingCount: 3, unvaluedHoldingCount: 1, invested: 10000 },
}

const cellText = (rowElement: HTMLElement, label: string) =>
  within(rowElement)
    .getAllByRole('cell')
    .find(cell => cell.textContent?.startsWith(`${label}:`))
    ?.textContent?.replace(`${label}:`, '')

const assetRow = (name: string) => screen.getByText(name).closest('tr')!

async function renderLoaded(holdings: BrokerHoldingsDto = HOLDINGS) {
  getBrokerHoldingsMock.mockResolvedValue(holdings)
  renderWithSelectedNode(<BrokerHoldingsTab />, BROKER_NODE)
  await screen.findByRole('table')
}

describe('BrokerHoldingsTab', () => {
  beforeEach(() => {
    getBrokerHoldingsMock.mockReset()
  })

  it('loads_the_selected_brokers_holdings', async () => {
    await renderLoaded()
    expect(getBrokerHoldingsMock).toHaveBeenCalledWith('XPI')
    expect(screen.getByRole('table')).toHaveAccessibleName('Holdings, amounts in BRL')
  })

  it('shows_each_portfolio_as_a_row_group_header_followed_by_its_holdings_and_subtotal', async () => {
    await renderLoaded()
    const groupHeaders = screen.getAllByRole('rowheader').map(cell => cell.textContent)
    expect(groupHeaders).toEqual([
      'Acoes',
      expect.stringContaining('Subtotal Acoes'),
      'FII',
      'Subtotal FII',
      expect.stringContaining('Broker total'),
    ])
  })

  it('shows_shares_profit_amount_and_credit_yields_for_a_holding', async () => {
    await renderLoaded()
    const holding = assetRow('BBBB11')
    expect(cellText(holding, 'Invested % of group')).toBe('25.00%')
    expect(cellText(holding, 'Invested % of broker')).toBe('10.00%')
    expect(cellText(holding, 'Profit amount')).toBe('-200.00')
    expect(cellText(holding, 'Profit %')).toBe('-10.00%')
    expect(cellText(holding, 'Credits % of invested')).toBe('50.00%')
    expect(cellText(holding, 'Monthly yield')).toBe('0.77%')
    expect(cellText(holding, 'APR')).toBe('9.23%')
    expect(cellText(holding, 'Last credit % of market value')).toBe('1.00%')
    expect(cellText(holding, 'Current price')).toBe('90.00')
  })

  it('shows_a_dash_for_the_group_shares_of_a_single_holding_and_for_an_unvalued_holding', async () => {
    await renderLoaded()
    const holding = assetRow('CCCC3')
    expect(within(holding).getAllByRole('img', { name: 'Only holding in the portfolio' })).toHaveLength(2)
    expect(cellText(holding, 'Invested % of group')).toBe('—')
    expect(cellText(holding, 'Current value')).toBe('—')
  })

  it('names_the_unvalued_holdings_on_the_subtotal_and_the_broker_total', async () => {
    await renderLoaded()
    expect(screen.getByText('1 of 1 holdings could not be valued; the total is incomplete.')).toBeInTheDocument()
    expect(screen.getByText('1 of 3 holdings could not be valued; the total is incomplete.')).toBeInTheDocument()
  })

  it('leaves_the_per_asset_only_columns_blank_on_a_subtotal', async () => {
    await renderLoaded()
    const subtotal = screen.getByText('Subtotal FII').closest('tr')!
    expect(cellText(subtotal, 'Invested')).toBe('4,000.00')
    expect(cellText(subtotal, 'Profit % with credits')).toBe('22.50%')
    expect(cellText(subtotal, 'Current price')).toBe('')
    expect(cellText(subtotal, 'Monthly yield')).toBe('')
    expect(cellText(subtotal, 'Last credit month')).toBe('')
  })

  it('sorts_within_each_group_and_keeps_group_headers_and_subtotals_in_place', async () => {
    await renderLoaded()
    const order = () =>
      screen.getAllByRole('row').slice(2).map(r =>
        (r as HTMLTableRowElement).cells[0].textContent?.replace('Asset name:', '').replace(/\d+ of \d+ holdings.*$/, ''),
      )
    const sortByInvested = () => fireEvent.click(screen.getByRole('button', { name: 'Invested' }))

    sortByInvested()

    expect(order()).toEqual(['Acoes', 'CCCC3', 'Subtotal Acoes', 'FII', 'BBBB11', 'AAAA11', 'Subtotal FII', 'Broker total'])

    sortByInvested()

    expect(order()).toEqual(['Acoes', 'CCCC3', 'Subtotal Acoes', 'FII', 'AAAA11', 'BBBB11', 'Subtotal FII', 'Broker total'])
  })

  it('sorts_each_group_by_first_purchase_newest_first_by_default', async () => {
    const newer = row({ assetName: 'NEWR11', firstInvestmentDate: '2025-01-01T00:00:00' })
    await renderLoaded({ ...HOLDINGS, groups: [{ ...HOLDINGS.groups[1], holdings: [...HOLDINGS.groups[1].holdings, newer] }] })
    expect(screen.getByRole('columnheader', { name: 'First purchase' })).toHaveAttribute('aria-sort', 'descending')
    const names = screen.getAllByRole('row').map(r => (r as HTMLTableRowElement).cells[0].textContent?.replace('Asset name:', ''))
    expect(names.indexOf('NEWR11')).toBeLessThan(names.indexOf('BBBB11'))
  })

  it('names_each_grouped_sort_button_with_its_group', async () => {
    await renderLoaded()
    expect(screen.getByRole('button', { name: 'Profit amount' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Last credit amount' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Market % of broker' })).toBeInTheDocument()
  })

  it('marks_a_stale_price_and_hides_columns_without_a_total_on_the_stacked_totals_card', async () => {
    await renderLoaded({ ...HOLDINGS, groups: [{ ...HOLDINGS.groups[1], holdings: [row({ assetName: 'OLDP11', marketStatus: 'Stale' })] }] })
    expect(within(assetRow('OLDP11')).getByText('This price is older than the most recent weekday.')).toBeInTheDocument()
    const subtotalCells = within(screen.getByText('Subtotal FII').closest('tr')!).getAllByRole('cell')
    const priceCell = subtotalCells.find(cell => cell.textContent?.startsWith('Current price:'))!
    expect(priceCell).toHaveClass('data-table__cell--no-total')
  })

  it('shows_the_empty_message_when_the_broker_has_no_active_holdings', async () => {
    getBrokerHoldingsMock.mockResolvedValue({ ...HOLDINGS, groups: [] })
    renderWithSelectedNode(<BrokerHoldingsTab />, BROKER_NODE)
    expect(await screen.findByText('This broker has no active holdings.')).toBeInTheDocument()
  })

  it('shows_the_error_panel_and_reloads_on_retry', async () => {
    getBrokerHoldingsMock.mockRejectedValueOnce(new Error('boom')).mockResolvedValue(HOLDINGS)
    renderWithSelectedNode(<BrokerHoldingsTab />, BROKER_NODE)

    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(screen.getByRole('table')).toBeInTheDocument())
    expect(getBrokerHoldingsMock).toHaveBeenCalledTimes(2)
  })
})
