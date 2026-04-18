import { resolveApiBaseUrl } from './config'
import type {
  AssetDetailsDto,
  BrokerNodeDto,
  CreditDto,
  OperationCreateDto,
  TreeNodeDto,
} from './types'

export interface FinancialApiClient {
  getNavigationTree: () => Promise<TreeNodeDto>
  getBrokers: () => Promise<BrokerNodeDto[]>
  getAssetDetails: (brokerName: string, portfolioName: string, assetName: string) => Promise<AssetDetailsDto>
  getCreditsByBroker: (brokerName: string) => Promise<CreditDto[]>
  getCreditsByPortfolio: (brokerName: string, portfolioName: string) => Promise<CreditDto[]>
  addOperation: (request: OperationCreateDto) => Promise<AssetDetailsDto>
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
  }
}
