export type NodeType = 'Asset' | 'Portfolio' | 'Broker'

export interface SelectedNode {
  nodeType: NodeType
  brokerName: string
  portfolioName?: string
  assetName?: string
  ticker?: string
  exchange?: string
  currency?: string
  isActive?: boolean
}

export interface SelectedNodeContextValue {
  selectedNode: SelectedNode | null
  setSelectedNode: (node: SelectedNode | null) => void
}

export interface TreeNodeDto {
  nodeType: string
  displayName: string
  children: TreeNodeDto[]
  metadata: Record<string, unknown>
}

export interface BrokerNodeDto {
  name: string
  currency: string
  portfolioCount: number
  totalAssets: number
  portfolios: PortfolioNodeDto[]
}

export interface PortfolioNodeDto {
  name: string
  assetCount: number
  activeAssetCount: number
  assets: AssetNodeDto[]
}

export interface AssetNodeDto {
  name: string
  ticker: string
  exchange: string
  country: string
  localTypeCode: string
  class: string
  isin: string
  quantity: number
  averagePrice: number
  isActive: boolean
  transactionCount: number
  creditCount: number
}

export interface TransactionDto {
  id: string
  date: string
  type: string
  quantity: number
  unitPrice: number
  fees: number
  totalPrice: number
}

export interface CreditDto {
  id: string
  date: string
  type: string
  value: number
}

export interface AssetDetailsDto {
  name: string
  brokerName: string
  portfolioName: string
  ticker: string
  isin: string
  exchange: string
  country: string
  localTypeCode: string
  class: string
  quantity: number
  averagePrice: number
  isActive: boolean
  totalBought: number
  totalSold: number
  totalCredits: number
  transactions: TransactionDto[]
  credits: CreditDto[]
}

export interface TransactionCreateDto {
  brokerName: string
  portfolioName: string
  assetName: string
  date: string
  type: string
  quantity: number
  unitPrice: number
  fees: number
}

export interface TransactionUpdateDto {
  brokerName: string
  portfolioName: string
  assetName: string
  id: string
  date: string
  type: string
  quantity: number
  unitPrice: number
  fees: number
}

export interface TransactionDeleteDto {
  brokerName: string
  portfolioName: string
  assetName: string
  id: string
}

export interface CreditCreateDto {
  brokerName: string
  portfolioName: string
  assetName: string
  date: string
  type: string
  value: number
}

export interface CreditUpdateDto {
  brokerName: string
  portfolioName: string
  assetName: string
  id: string
  date: string
  type: string
  value: number
}

export interface CreditDeleteDto {
  brokerName: string
  portfolioName: string
  assetName: string
  id: string
}

export interface DividendHistoryItemDto {
  type: string
  date: string
  value: number
}

export interface DividendYearTotalDto {
  year: number
  total: number
}

export interface DividendSummaryDto {
  exchange: string
  ticker: string
  name: string
  currentPrice: number
  priceAsOf: string
  averageDividendLastFiveYears: number
  dividendYieldPercent: number
  priceMaxBuy: number
  discountPercent: number
  yearTotals: DividendYearTotalDto[]
}

export interface AssetPriceDto {
  exchange: string
  ticker: string
  name: string
  price: number
  asOf: string | null
}

export interface AggregatedSummaryDto {
  totalBought: number
  totalSold: number
  totalCredits: number
}

export interface WatchlistItemDto {
  group: string
  name: string
}
