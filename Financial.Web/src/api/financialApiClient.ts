import { API_BASE_URL } from './config'
import type {
  AggregatedSummaryDto,
  AssetCashFlowDto,
  AssetDetailsDto,
  AssetPriceDto,
  BrokerNodeDto,
  CalculateXirrRequestDto,
  CreditCreateDto,
  CreditDeleteDto,
  CreditDto,
  CreditUpdateDto,
  DividendHistoryItemDto,
  DividendSummaryDto,
  InvestmentScope,
  PortfolioAssetSummaryItemDto,
  PortfolioBreakdownItemDto,
  PortfolioReferenceDto,
  TransactionCreateDto,
  TransactionDeleteDto,
  TransactionSummaryItemDto,
  TransactionUpdateDto,
  TreeNodeDto,
  WatchlistItemDto,
  XirrResultDto,
} from './types'

export interface FinancialApiClient {
  getNavigationTree: (scope?: InvestmentScope) => Promise<TreeNodeDto>
  getBrokers: () => Promise<BrokerNodeDto[]>
  getAssetDetails: (brokerName: string, portfolioName: string, assetName: string, scope?: InvestmentScope) => Promise<AssetDetailsDto>
  getCreditsByBroker: (brokerName: string) => Promise<CreditDto[]>
  getCreditsByPortfolio: (brokerName: string, portfolioName: string) => Promise<CreditDto[]>
  getSummaryByBroker: (brokerName: string, scope?: InvestmentScope) => Promise<AggregatedSummaryDto>
  getSummaryByPortfolio: (brokerName: string, portfolioName: string, scope?: InvestmentScope) => Promise<AggregatedSummaryDto>
  getBrokerBreakdown: (brokerName: string, scope?: InvestmentScope) => Promise<PortfolioBreakdownItemDto[]>
  getTransactionsByBroker: (brokerName: string) => Promise<TransactionSummaryItemDto[]>
  getTransactionsByPortfolio: (brokerName: string, portfolioName: string) => Promise<TransactionSummaryItemDto[]>
  addTransaction: (request: TransactionCreateDto) => Promise<AssetDetailsDto>
  updateTransaction: (request: TransactionUpdateDto) => Promise<AssetDetailsDto>
  deleteTransaction: (request: TransactionDeleteDto) => Promise<AssetDetailsDto>
  addCredit: (request: CreditCreateDto) => Promise<AssetDetailsDto>
  updateCredit: (request: CreditUpdateDto) => Promise<AssetDetailsDto>
  deleteCredit: (request: CreditDeleteDto) => Promise<AssetDetailsDto>
  getDividendHistory: (ticker: string, exchange?: string) => Promise<DividendHistoryItemDto[]>
  getDividendSummary: (ticker: string, exchange?: string) => Promise<DividendSummaryDto>
  getCurrentPrice: (
    exchange: string,
    ticker: string,
    assetClass?: string,
    brokerName?: string,
    name?: string,
  ) => Promise<AssetPriceDto>
  getWatchlist: () => Promise<WatchlistItemDto[]>
  getAssetPriceFetchScope: () => Promise<PortfolioReferenceDto[]>
  getPortfolioAssetsSummary: (brokerName: string, portfolioName: string, scope?: InvestmentScope) => Promise<PortfolioAssetSummaryItemDto[]>
  calculateXirr: (cashFlows: AssetCashFlowDto[], terminalValue: number) => Promise<XirrResultDto>
}

export interface FinancialApiClientOptions {
  baseUrl?: string
  fetch?: typeof fetch
}

interface ProblemDetailsBody {
  detail?: string
  title?: string
}

async function buildErrorMessage(response: Response, method: string, url: string): Promise<string> {
  let body = ''
  try {
    body = await response.text()
  } catch {
    // Ignore response body failures; fall back to the generic message below.
  }

  if (body) {
    try {
      const problem = JSON.parse(body) as ProblemDetailsBody
      if (problem.detail) return problem.detail
      if (problem.title) return problem.title
    } catch {
      // Not a JSON problem-details body; fall through to the generic message.
    }
  }

  return `API request failed: ${method} ${url} (${response.status} ${body || response.statusText})`
}

export function createFinancialApiClient(options: FinancialApiClientOptions = {}): FinancialApiClient {
  const baseUrl = options.baseUrl !== undefined
    ? options.baseUrl.replace(/\/$/, '')
    : API_BASE_URL
  const fetcher = options.fetch ?? fetch

  const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
    const url = `${baseUrl}${path}`
    const headers = new Headers(init?.headers)
    if (!headers.has('Accept')) {
      headers.set('Accept', 'application/json')
    }
    if (init?.body !== undefined && init?.body !== null && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    const response = await fetcher(url, {
      ...init,
      headers,
    })

    if (!response.ok) {
      const method = init?.method ?? 'GET'
      throw new Error(await buildErrorMessage(response, method, url))
    }

    return (await response.json()) as T
  }

  const buildExchangeQuery = (exchange?: string) => {
    const trimmed = exchange?.trim()
    return trimmed && trimmed.length > 0 ? `?exchange=${encodeURIComponent(trimmed)}` : ''
  }

  const buildScopeQuery = (scope: InvestmentScope) => `?scope=${scope}`

  return {
    getNavigationTree: (scope = 'active') => request<TreeNodeDto>(`/navigation/tree${buildScopeQuery(scope)}`),
    getBrokers: () => request<BrokerNodeDto[]>('/navigation/brokers'),
    getAssetDetails: (brokerName, portfolioName, assetName, scope = 'active') =>
      request<AssetDetailsDto>(
        `/assets/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}/${encodeURIComponent(assetName)}${buildScopeQuery(scope)}`,
      ),
    getCreditsByBroker: (brokerName) =>
      request<CreditDto[]>(`/credits/broker/${encodeURIComponent(brokerName)}`),
    getCreditsByPortfolio: (brokerName, portfolioName) =>
      request<CreditDto[]>(
        `/credits/portfolio/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}`,
      ),
    getSummaryByBroker: (brokerName, scope = 'active') =>
      request<AggregatedSummaryDto>(`/summary/broker/${encodeURIComponent(brokerName)}${buildScopeQuery(scope)}`),
    getSummaryByPortfolio: (brokerName, portfolioName, scope = 'active') =>
      request<AggregatedSummaryDto>(
        `/summary/portfolio/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}${buildScopeQuery(scope)}`,
      ),
    getBrokerBreakdown: (brokerName, scope = 'active') =>
      request<PortfolioBreakdownItemDto[]>(`/summary/broker/${encodeURIComponent(brokerName)}/breakdown${buildScopeQuery(scope)}`),
    getTransactionsByBroker: (brokerName) =>
      request<TransactionSummaryItemDto[]>(`/transactions/broker/${encodeURIComponent(brokerName)}`),
    getTransactionsByPortfolio: (brokerName, portfolioName) =>
      request<TransactionSummaryItemDto[]>(
        `/transactions/portfolio/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}`,
      ),
    addTransaction: (requestBody) =>
      request<AssetDetailsDto>('/transactions', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    updateTransaction: (requestBody) =>
      request<AssetDetailsDto>('/transactions', {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    deleteTransaction: (requestBody) =>
      request<AssetDetailsDto>('/transactions', {
        method: 'DELETE',
        body: JSON.stringify(requestBody),
      }),
    addCredit: (requestBody) =>
      request<AssetDetailsDto>('/credits', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    updateCredit: (requestBody) =>
      request<AssetDetailsDto>('/credits', {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    deleteCredit: (requestBody) =>
      request<AssetDetailsDto>('/credits', {
        method: 'DELETE',
        body: JSON.stringify(requestBody),
      }),
    getDividendHistory: (ticker, exchange) =>
      request<DividendHistoryItemDto[]>(
        `/dividends/${encodeURIComponent(ticker)}/history${buildExchangeQuery(exchange)}`,
      ),
    getDividendSummary: (ticker, exchange) =>
      request<DividendSummaryDto>(
        `/dividends/${encodeURIComponent(ticker)}/summary${buildExchangeQuery(exchange)}`,
      ),
    getCurrentPrice: (exchange, ticker, assetClass, brokerName, name) => {
      const classQuery = assetClass ? `&assetClass=${encodeURIComponent(assetClass)}` : ''
      const brokerQuery = brokerName ? `&brokerName=${encodeURIComponent(brokerName)}` : ''
      const nameQuery = name ? `&name=${encodeURIComponent(name)}` : ''
      return request<AssetPriceDto>(
        `/prices/current?exchange=${encodeURIComponent(exchange)}&ticker=${encodeURIComponent(ticker)}${classQuery}${brokerQuery}${nameQuery}`,
      )
    },
    getWatchlist: () => request<WatchlistItemDto[]>('/watchlist'),
    getAssetPriceFetchScope: () => request<PortfolioReferenceDto[]>('/asset-price-fetch'),
    getPortfolioAssetsSummary: (brokerName, portfolioName, scope = 'active') =>
      request<PortfolioAssetSummaryItemDto[]>(
        `/summary/portfolio/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}/assets${buildScopeQuery(scope)}`,
      ),
    calculateXirr: (cashFlows, terminalValue) =>
      request<XirrResultDto>('/xirr/calculate', {
        method: 'POST',
        body: JSON.stringify({ cashFlows, terminalValue } satisfies CalculateXirrRequestDto),
      }),
  }
}
