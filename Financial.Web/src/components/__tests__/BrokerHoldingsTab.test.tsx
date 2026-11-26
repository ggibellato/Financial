import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerHoldingsDto, HoldingRowDto, HoldingsGroupDto, HoldingsTotalsDto, PortfolioAssetSummaryItemDto, SelectedNode } from '../../api/types'
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
  segment: null,
  pickSource: null,
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
  lastMonthStartValue: 3000,
  thisMonthStartValue: 3300,
  monthChangePercent: 10,
  monthStartExcludedCount: 0,
}

const NO_MONTH_START = { value: null, unitPrice: null, priceDate: null, currency: null }

function row(asset: Partial<PortfolioAssetSummaryItemDto>, shares: Partial<HoldingRowDto> = {}): HoldingRowDto {
  return {
    asset: { ...ASSET, ...asset },
    investedShareOfGroup: 25,
    marketShareOfGroup: 30,
    investedShareOfBase: 10,
    marketShareOfBase: 12,
    lastMonthStart: { value: 1000, unitPrice: 10, priceDate: '2026-10-30', currency: 'BRL' },
    thisMonthStart: { value: 1100, unitPrice: 11, priceDate: '2026-11-01', currency: 'BRL' },
    monthChangePercent: 10,
    ...shares,
  }
}

const HOLDINGS: BrokerHoldingsDto = {
  currencies: ['BRL'],
  lastMonthStart: { date: '2026-10-01', earliestPriceDate: '2026-09-24', latestPriceDate: '2026-10-08' },
  thisMonthStart: { date: '2026-11-01', earliestPriceDate: '2026-10-25', latestPriceDate: '2026-11-08' },
  groups: [
    {
      name: 'Acoes',
      segments: [],
      arcaBucket: null,
      arcaSection: null,
      holdings: [row({ assetName: 'CCCC3', marketValue: null, unrealisedGain: null, resultPercent: null }, { investedShareOfGroup: null, marketShareOfGroup: null })],
      subtotal: { ...TOTALS, holdingCount: 1, unvaluedHoldingCount: 1, marketValue: null, profitAmount: null },
    },
    {
      name: 'FII',
      segments: [],
      arcaBucket: null,
      arcaSection: null,
      holdings: [row({ assetName: 'BBBB11', totalInvested: 1000 }), row({ assetName: 'AAAA11', totalInvested: 3000 })],
      subtotal: TOTALS,
    },
  ],
  total: { ...TOTALS, holdingCount: 3, unvaluedHoldingCount: 1, invested: 10000 },
  arca: null,
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

  it('says_amounts_are_in_each_holdings_own_currency_when_the_broker_holds_several', async () => {
    await renderLoaded({ ...HOLDINGS, currencies: ['GBP', 'USD'] })
    expect(screen.getByRole('table')).toHaveAccessibleName(
      "Holdings, amounts in each holding's own currency (GBP, USD); subtotals and totals add them without conversion",
    )
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

  it('shows_the_pick_source_of_each_holding', async () => {
    await renderLoaded({
      ...HOLDINGS,
      groups: [{ ...HOLDINGS.groups[1], holdings: [row({ assetName: 'PICK11', pickSource: 'MyChoice' }), row({ assetName: 'NOPK11' })] }],
    })
    expect(cellText(assetRow('PICK11'), 'Pick')).toBe('My choice')
    expect(cellText(assetRow('NOPK11'), 'Pick')).toBe('—')
  })

  it('groups_a_segmented_portfolio_by_segment_with_each_segments_subtotal_and_share_of_the_portfolio', async () => {
    const logistics = row({ assetName: 'LOGA11', segment: 'Logistics' })
    const unsegmented = row({ assetName: 'NONE11' })
    await renderLoaded({
      ...HOLDINGS,
      groups: [
        {
          ...HOLDINGS.groups[1],
          holdings: [logistics, unsegmented],
          segments: [
            { name: 'Logistics', holdings: [logistics], subtotal: { ...TOTALS, invested: 1000 }, investedShareOfGroup: 25, marketShareOfGroup: 30 },
            { name: null, holdings: [unsegmented], subtotal: { ...TOTALS, invested: 3000 }, investedShareOfGroup: 75, marketShareOfGroup: 70 },
          ],
        },
      ],
    })

    const headers = screen.getAllByRole('rowheader').map(cell => cell.textContent)
    expect(headers).toEqual([
      'FII',
      'Segment: Logistics',
      'Segment subtotal: Logistics',
      'Segment: No segment',
      'Segment subtotal: No segment',
      'Subtotal FII',
      expect.stringContaining('Broker total'),
    ])
    expect(within(assetRow('LOGA11')).getByText('Segment Logistics:', { exact: false })).toBeInTheDocument()
    const segmentSubtotal = screen.getByText('Segment subtotal: Logistics').closest('tr')!
    expect(cellText(segmentSubtotal, 'Invested')).toBe('1,000.00')
    expect(cellText(segmentSubtotal, 'Invested % of group')).toBe('25.00%')
    expect(cellText(segmentSubtotal, 'Market % of group')).toBe('30.00%')
    expect(cellText(screen.getByText('Subtotal FII').closest('tr')!, 'Invested % of group')).toBe('')
  })

  it('sorts_within_each_segment', async () => {
    const small = row({ assetName: 'SMAL11', segment: 'Logistics', totalInvested: 100 })
    const large = row({ assetName: 'LARG11', segment: 'Logistics', totalInvested: 900 })
    const other = row({ assetName: 'OTHR11', segment: 'Paper', totalInvested: 500 })
    await renderLoaded({
      ...HOLDINGS,
      groups: [
        {
          ...HOLDINGS.groups[1],
          holdings: [small, large, other],
          segments: [
            { name: 'Logistics', holdings: [small, large], subtotal: TOTALS, investedShareOfGroup: 66, marketShareOfGroup: 66 },
            { name: 'Paper', holdings: [other], subtotal: TOTALS, investedShareOfGroup: 34, marketShareOfGroup: 34 },
          ],
        },
      ],
    })

    fireEvent.click(screen.getByRole('button', { name: 'Invested' }))

    const names = screen.getAllByRole('row').slice(2).map(r => (r as HTMLTableRowElement).cells[0].textContent?.replace(/^Asset name:(Segment [^:]+: )?/, ''))
    expect(names.slice(0, 7)).toEqual(['FII', 'Segment: Logistics', 'SMAL11', 'LARG11', 'Segment subtotal: Logistics', 'Segment: Paper', 'OTHR11'])
  })

  it('heads_the_month_columns_with_the_first_of_last_month_and_of_this_month', async () => {
    await renderLoaded()
    expect(screen.getByRole('button', { name: '1 Oct, start of last month' })).toHaveTextContent('1 Oct')
    expect(screen.getByRole('button', { name: '1 Nov, start of this month' })).toHaveTextContent('1 Nov')
  })

  it('shows_the_month_start_values_with_the_price_used_and_a_signed_change', async () => {
    await renderLoaded()
    const holding = assetRow('BBBB11')
    expect(cellText(holding, 'Start of last month (1 Oct)')).toMatch(/^1,000\.00, R\$\s?10\.00 saved 30 Oct$/)
    expect(cellText(holding, 'Start of this month (1 Nov)')).toMatch(/^1,100\.00, R\$\s?11\.00 saved 1 Nov$/)
    expect(within(holding).getByTitle(/R\$\s?11\.00 saved 1 Nov/)).toBeInTheDocument()
    expect(cellText(holding, 'Month change %')).toBe('+10.00%')
  })

  it('marks_a_missing_month_start_value_with_the_price_window_it_searched', async () => {
    await renderLoaded({
      ...HOLDINGS,
      groups: [{ ...HOLDINGS.groups[1], holdings: [row({ assetName: 'NOPR11' }, { thisMonthStart: NO_MONTH_START, monthChangePercent: null })] }],
    })
    const holding = assetRow('NOPR11')
    expect(within(holding).getByRole('img', { name: 'No saved price between 25 Oct and 8 Nov' })).toBeInTheDocument()
    expect(within(holding).getByRole('img', { name: 'No change: a month-start price is missing' })).toBeInTheDocument()
    expect(within(holding).getByTitle('No saved price between 25 Oct and 8 Nov')).toBeInTheDocument()
  })

  it('stars_a_month_start_subtotal_that_excludes_holdings_without_a_price', async () => {
    await renderLoaded({
      ...HOLDINGS,
      groups: [{ ...HOLDINGS.groups[1], subtotal: { ...TOTALS, monthStartExcludedCount: 1 } }],
    })
    const subtotal = screen.getByText('Subtotal FII').closest('tr')!
    expect(within(subtotal).getByText('* Excludes 1 holdings without both month-start prices')).toBeInTheDocument()
    expect(cellText(subtotal, 'Start of last month (1 Oct)')).toBe('3,000.00*')
    expect(cellText(subtotal, 'Start of this month (1 Nov)')).toBe('3,300.00*')
    expect(cellText(subtotal, 'Month change %')).toBe('+10.00%*')
  })

  it('arranges_a_broker_using_arca_into_arca_reserve_fund_and_not_in_arca_sections_with_a_grand_total', async () => {
    await renderLoaded(ARCA_HOLDINGS)

    expect(screen.getAllByRole('heading', { level: 3 }).map(h => h.textContent)).toEqual(['ARCA', 'Reserve fund', 'Not in ARCA'])
    expect(screen.getAllByRole('rowheader').map(cell => cell.textContent?.replace(/\d+ of \d+ holdings.*$/, ''))).toEqual([
      'Acoes · , A – Shares and businesses',
      'Subtotal Acoes',
      'FII · , R – Real estate',
      'Subtotal FII',
      'ARCA total',
      'Reserva',
      'Subtotal Reserva',
      'Subtotal Reserve fund',
      'Outros',
      'Subtotal Outros',
      'Subtotal Not in ARCA',
      'Grand total',
    ])
  })

  it('reads_the_share_columns_as_percent_of_arca_and_leaves_rows_outside_arca_blank', async () => {
    await renderLoaded(ARCA_HOLDINGS)

    expect(screen.getByRole('button', { name: 'Market % of ARCA' })).toBeInTheDocument()
    expect(cellText(assetRow('FIIA11'), 'Invested % of ARCA')).toBe('30.00%')
    expect(cellText(assetRow('RESV11'), 'Invested % of ARCA')).toBe('—')
    expect(within(assetRow('RESV11')).getAllByRole('img', { name: 'Not part of ARCA' })).toHaveLength(2)
    expect(cellText(assetRow('RESV11'), 'Invested % of group')).toBe('—')
  })

  it('leaves_out_the_not_in_arca_section_when_every_portfolio_has_a_bucket', async () => {
    await renderLoaded({
      ...ARCA_HOLDINGS,
      groups: ARCA_HOLDINGS.groups.filter(g => g.arcaBucket !== null),
      arca: { ...ARCA_HOLDINGS.arca!, notInArca: null },
    })

    expect(screen.getAllByRole('heading', { level: 3 }).map(h => h.textContent)).toEqual(['ARCA', 'Reserve fund'])
  })

  it('keeps_the_broker_layout_and_base_when_no_portfolio_has_a_bucket', async () => {
    await renderLoaded()

    expect(screen.queryByRole('heading', { level: 3 })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Market % of broker' })).toBeInTheDocument()
    expect(screen.getByText('Broker total')).toBeInTheDocument()
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

function arcaGroup(name: string, arcaBucket: HoldingsGroupDto['arcaBucket'], assetName: string, shares: Partial<HoldingRowDto> = {}): HoldingsGroupDto {
  const arcaSection = arcaBucket === null ? 'NotInArca' : arcaBucket === 'ReserveFund' ? 'ReserveFund' : 'Arca'
  return { name, arcaBucket, arcaSection, segments: [], holdings: [row({ assetName }, shares)], subtotal: TOTALS }
}

const ARCA_HOLDINGS: BrokerHoldingsDto = {
  ...HOLDINGS,
  groups: [
    arcaGroup('Acoes', 'Shares', 'ACAO3'),
    arcaGroup('FII', 'RealEstate', 'FIIA11', { investedShareOfBase: 30 }),
    arcaGroup('Reserva', 'ReserveFund', 'RESV11', { investedShareOfBase: null, marketShareOfBase: null, investedShareOfGroup: null, marketShareOfGroup: null }),
    arcaGroup('Outros', null, 'OUTR11', { investedShareOfBase: null, marketShareOfBase: null }),
  ],
  arca: {
    buckets: [
      { bucket: 'Shares', totals: TOTALS },
      { bucket: 'RealEstate', totals: TOTALS },
    ],
    arcaTotal: TOTALS,
    reserveFund: TOTALS,
    notInArca: TOTALS,
    arcaAndReserveFundTotal: TOTALS,
  },
}
