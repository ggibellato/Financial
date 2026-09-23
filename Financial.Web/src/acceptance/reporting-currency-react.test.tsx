import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '../test/renderWithFluent'
import { createSelectedNodeWrapper } from '../test-utils/selectedNodeTestWrapper'
import ReportingCurrencyPage from '../pages/ReportingCurrencyPage'
import AggregatedSummaryTab from '../components/AggregatedSummaryTab'
import TransactionsTab from '../components/TransactionsTab'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { AggregatedSummaryDto, AssetDetailsDto } from '../api/types'

const {
  getReportingCurrencyMock,
  setReportingCurrencyMock,
  getSummaryByBrokerMock,
  getBrokerBreakdownMock,
  getAssetDetailsMock,
  getTransactionTypeEffectsMock,
} = vi.hoisted(() => ({
  getReportingCurrencyMock: vi.fn<FinancialApiClient['getReportingCurrency']>(),
  setReportingCurrencyMock: vi.fn<FinancialApiClient['setReportingCurrency']>(),
  getSummaryByBrokerMock: vi.fn<FinancialApiClient['getSummaryByBroker']>(),
  getBrokerBreakdownMock: vi.fn<FinancialApiClient['getBrokerBreakdown']>(),
  getAssetDetailsMock: vi.fn<FinancialApiClient['getAssetDetails']>(),
  getTransactionTypeEffectsMock: vi.fn<FinancialApiClient['getTransactionTypeEffects']>(),
}))

vi.mock('../api/financialApiClient', () => ({
  apiClient: {
    getReportingCurrency: getReportingCurrencyMock,
    setReportingCurrency: setReportingCurrencyMock,
    getSummaryByBroker: getSummaryByBrokerMock,
    getBrokerBreakdown: getBrokerBreakdownMock,
    getAssetDetails: getAssetDetailsMock,
    getTransactionTypeEffects: getTransactionTypeEffectsMock,
  } as Partial<FinancialApiClient>,
}))

const SUMMARY: AggregatedSummaryDto = {
  totalBought: 1000,
  totalSold: 0,
  totalCredits: 0,
  totalInvested: 1000,
  marketValue: 1150,
  holdingCount: 1,
  unvaluedHoldingCount: 0,
  priceOnlyReturn: 0.12,
  totalReturn: 0.14,
  totalReturnNetOfTax: 0.13,
  reportingCurrency: 'GBP',
  isReportingCurrencyEnabled: true,
  convertedMarketValue: 168.55,
  convertedInvested: 146.6,
  convertedUnrealisedGainLoss: 21.95,
  convertedTotalReturn: 0.145,
  convertedTotalReturnNetOfTax: 0.135,
  isPartial: false,
  isReportingCurrencyUnavailable: false,
}

const ASSET: AssetDetailsDto = {
  name: 'KLBN4',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  ticker: 'KLBN4',
  isin: 'BRKLBN',
  exchange: 'BVMF',
  country: 'BR',
  localTypeCode: 'ON',
  class: 'Equity',
  valuationMethod: 'Unspecified',
  incomePolicy: 'Unknown',
  quantity: 100,
  averagePrice: 20,
  averageSellPrice: null,
  positionType: 'Long',
  totalBought: 2000,
  totalSold: 0,
  totalCredits: 0,
  realizedGainLoss: 0,
  marketValue: null,
  costOfUnitsHeld: 2000,
  unrealisedGain: null,
  priceAsOfDate: null,
  marketStatus: 'Current',
  priceOnlyReturn: null,
  totalReturn: null,
  transactions: [
    {
      id: 't1',
      date: '2026-07-01T00:00:00',
      type: 'Buy',
      quantity: 100,
      unitPrice: 20,
      fees: 0,
      withheld: 0,
      netCash: -2000,
      currency: 'BRL',
      fxRateSnapshot: { toCurrency: 'GBP', rate: 0.146, source: 'Frankfurter', retrievedAt: '2026-07-01T08:00:00Z' },
    },
  ],
  credits: [],
  priceSnapshots: [],
  cashFlowsWithCredits: [],
  cashFlowsWithoutCredits: [],
  disposalRecords: [],
  corporateActions: [],
  costBasisMethod: 'AverageCost',
  taxJurisdictions: [],
}

function renderBrokerSummary(summary: AggregatedSummaryDto) {
  const { wrapper, setNode } = createSelectedNodeWrapper()
  getSummaryByBrokerMock.mockResolvedValue(summary)
  getBrokerBreakdownMock.mockReturnValue(new Promise(() => {}))
  const result = render(<AggregatedSummaryTab />, { wrapper })
  setNode({ nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' })
  return result
}

describe('P49 F04 — React Reporting Currency acceptance', () => {
  beforeEach(() => {
    getReportingCurrencyMock.mockReset()
    setReportingCurrencyMock.mockReset()
    getSummaryByBrokerMock.mockReset()
    getBrokerBreakdownMock.mockReset()
    getAssetDetailsMock.mockReset()
    getTransactionTypeEffectsMock.mockReset()
    getTransactionTypeEffectsMock.mockReturnValue(new Promise(() => {}))
  })

  it('[AC P49-F04-react-reporting-currency-01] a Reporting Currency control appears under Settings, offering GBP/BRL/USD, and persists the chosen value via the setting endpoint', async () => {
    getReportingCurrencyMock.mockResolvedValue({ currency: 'GBP', enabled: true })
    setReportingCurrencyMock.mockResolvedValue({ currency: 'BRL', enabled: true })
    render(<ReportingCurrencyPage />)

    expect(await screen.findByRole('radio', { name: 'GBP' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'BRL' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'USD' })).toBeInTheDocument()

    screen.getByRole('radio', { name: 'BRL' }).click()

    await waitFor(() => expect(setReportingCurrencyMock).toHaveBeenCalledWith({ currency: 'BRL', enabled: true }))
  })

  it('[AC P49-F04-react-reporting-currency-02] the broker summary view shows the converted total clearly labelled with its currency, next to the unchanged native-currency figures', async () => {
    renderBrokerSummary(SUMMARY)

    expect(await screen.findByText('Market Value (converted to GBP)')).toBeInTheDocument()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
    const nativeMarketValue = screen.getByText('Market Value').nextElementSibling
    expect(nativeMarketValue?.textContent).toMatch(/1[.,]150[.,]00/)
  })

  it('[AC P49-F04-react-reporting-currency-03] a provenance affordance on a Transaction row displays the underlying rate, source and retrieved-at date', async () => {
    getAssetDetailsMock.mockResolvedValue(ASSET)
    const { wrapper, setNode } = createSelectedNodeWrapper()
    render(<TransactionsTab />, { wrapper })
    setNode({ nodeType: 'Asset', brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4', ticker: 'KLBN4', exchange: 'BVMF' })

    const provenanceButton = await screen.findByRole('button', { name: 'FX conversion details' })
    provenanceButton.focus()

    expect(await screen.findByText(/1 BRL = 0.146 GBP/)).toBeInTheDocument()
    expect(screen.getByText(/Source: Frankfurter/)).toBeInTheDocument()
  })

  it('[AC P49-F04-react-reporting-currency-04] a Partial-flagged converted total shows a visible inline warning rather than presenting an incomplete figure as complete', async () => {
    renderBrokerSummary({ ...SUMMARY, isPartial: true })

    expect(await screen.findByText(/could not be converted/)).toBeInTheDocument()
    expect(screen.getByText('Market Value (converted to GBP)')).toBeInTheDocument()
  })

  it('[AC P49-F04-react-reporting-currency-05] a ReportingCurrencyUnavailable response hides the converted figures with a retry affordance while native-currency figures stay visible', async () => {
    renderBrokerSummary({ ...SUMMARY, isReportingCurrencyUnavailable: true })

    expect(await screen.findByRole('alert')).toHaveTextContent(/Converted totals unavailable/)
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
    expect(screen.queryByText(/Market Value \(converted to/)).not.toBeInTheDocument()
    expect(screen.getByText('Total Bought')).toBeInTheDocument()
    expect(screen.getByText('Market Value')).toBeInTheDocument()
  })
})
