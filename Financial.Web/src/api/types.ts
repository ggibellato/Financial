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
