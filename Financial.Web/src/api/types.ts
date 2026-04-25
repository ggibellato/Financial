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
  country: number
  localTypeCode: string
  class: number
  isin: string
  quantity: number
  averagePrice: number
  isActive: boolean
  operationCount: number
  creditCount: number
}

export interface OperationDto {
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
  country: number
  localTypeCode: string
  class: number
  quantity: number
  averagePrice: number
  isActive: boolean
  totalBought: number
  totalSold: number
  totalCredits: number
  operations: OperationDto[]
  credits: CreditDto[]
}

export interface OperationCreateDto {
  brokerName: string
  portfolioName: string
  assetName: string
  date: string
  type: string
  quantity: number
  unitPrice: number
  fees: number
}

export interface OperationUpdateDto {
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

export interface OperationDeleteDto {
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
