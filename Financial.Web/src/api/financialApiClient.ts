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
    const response = await fetcher(`${baseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        ...(init?.headers ?? {}),
      },
    })

    if (!response.ok) {
      throw new Error(`API request failed (${response.status} ${response.statusText})`)
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
