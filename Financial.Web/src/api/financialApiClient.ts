import { resolveApiBaseUrl } from './config'
import type {
  AssetDetailsDto,
  BrokerNodeDto,
  CreditCreateDto,
  CreditDeleteDto,
  CreditDto,
  CreditUpdateDto,
  DividendHistoryItemDto,
  DividendSummaryDto,
  OperationCreateDto,
  OperationDeleteDto,
  OperationUpdateDto,
  TreeNodeDto,
} from './types'

export interface FinancialApiClient {
  getNavigationTree: () => Promise<TreeNodeDto>
  getBrokers: () => Promise<BrokerNodeDto[]>
  getAssetDetails: (brokerName: string, portfolioName: string, assetName: string) => Promise<AssetDetailsDto>
  getCreditsByBroker: (brokerName: string) => Promise<CreditDto[]>
  getCreditsByPortfolio: (brokerName: string, portfolioName: string) => Promise<CreditDto[]>
  addOperation: (request: OperationCreateDto) => Promise<AssetDetailsDto>
  updateOperation: (request: OperationUpdateDto) => Promise<AssetDetailsDto>
  deleteOperation: (request: OperationDeleteDto) => Promise<AssetDetailsDto>
  addCredit: (request: CreditCreateDto) => Promise<AssetDetailsDto>
  updateCredit: (request: CreditUpdateDto) => Promise<AssetDetailsDto>
  deleteCredit: (request: CreditDeleteDto) => Promise<AssetDetailsDto>
  getDividendHistory: (ticker: string, exchange?: string) => Promise<DividendHistoryItemDto[]>
  getDividendSummary: (ticker: string, exchange?: string) => Promise<DividendSummaryDto>
}

export interface FinancialApiClientOptions {
  baseUrl?: string
  fetch?: typeof fetch
}

export function createFinancialApiClient(options: FinancialApiClientOptions = {}): FinancialApiClient {
  const baseUrl = resolveApiBaseUrl(options.baseUrl ?? import.meta.env.VITE_API_BASE_URL)
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
      let errorDetail = response.statusText
      try {
        const body = await response.text()
        if (body) {
          errorDetail = body
        }
      } catch {
        // Ignore response body failures; status text is enough.
      }
      const method = init?.method ?? 'GET'
      throw new Error(`API request failed: ${method} ${url} (${response.status} ${errorDetail})`)
    }

    return (await response.json()) as T
  }

  const buildExchangeQuery = (exchange?: string) => {
    const trimmed = exchange?.trim()
    return trimmed && trimmed.length > 0 ? `?exchange=${encodeURIComponent(trimmed)}` : ''
  }

  return {
    getNavigationTree: () => request<TreeNodeDto>('/navigation/tree'),
    getBrokers: () => request<BrokerNodeDto[]>('/navigation/brokers'),
    getAssetDetails: (brokerName, portfolioName, assetName) =>
      request<AssetDetailsDto>(
        `/assets/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}/${encodeURIComponent(assetName)}`,
      ),
    getCreditsByBroker: (brokerName) =>
      request<CreditDto[]>(`/credits/broker/${encodeURIComponent(brokerName)}`),
    getCreditsByPortfolio: (brokerName, portfolioName) =>
      request<CreditDto[]>(
        `/credits/portfolio/${encodeURIComponent(brokerName)}/${encodeURIComponent(portfolioName)}`,
      ),
    addOperation: (requestBody) =>
      request<AssetDetailsDto>('/operations', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    updateOperation: (requestBody) =>
      request<AssetDetailsDto>('/operations', {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    deleteOperation: (requestBody) =>
      request<AssetDetailsDto>('/operations', {
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
  }
}
