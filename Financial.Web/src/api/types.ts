export type NodeType = 'Asset' | 'Portfolio' | 'Broker'

export type PositionType = 'Long' | 'Flat' | 'Short'

export type InvestmentScope = 'active' | 'historic'

export interface SelectedNode {
  nodeType: NodeType
  brokerName: string
  portfolioName?: string
  assetName?: string
  ticker?: string
  exchange?: string
  currency?: string
  positionType?: PositionType
  assetClass?: string
}

export interface SelectedNodeContextValue {
  selectedNode: SelectedNode | null
  setSelectedNode: (node: SelectedNode | null) => void
  scope: InvestmentScope
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
  positionType: PositionType
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

export interface TransactionSummaryItemDto {
  assetName: string
  date: string
  type: string
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
  averageSellPrice: number | null
  positionType: PositionType
  totalBought: number
  totalSold: number
  totalCredits: number
  realizedGainLoss: number
  transactions: TransactionDto[]
  credits: CreditDto[]
  cashFlowsWithCredits: AssetCashFlowDto[]
  cashFlowsWithoutCredits: AssetCashFlowDto[]
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
  totalInvested: number
}

export interface AssetBreakdownItemDto {
  assetName: string
  totalInvested: number
}

export interface PortfolioBreakdownItemDto {
  portfolioName: string
  totalInvested: number
  assets: AssetBreakdownItemDto[]
}

export interface WatchlistItemDto {
  group: string
  name: string
}

export interface PortfolioReferenceDto {
  brokerName: string
  portfolioName: string
}

export interface AssetCashFlowDto {
  date: string
  amount: number
}

export interface CalculateXirrRequestDto {
  cashFlows: AssetCashFlowDto[]
  terminalValue: number
}

export interface XirrResultDto {
  xirr: number | null
}

export interface PortfolioAssetSummaryItemDto {
  assetName: string
  ticker: string
  exchange: string
  class: string
  firstInvestmentDate: string | null
  currentQuantity: number
  averagePrice: number
  averageSellPrice: number | null
  totalBought: number
  totalSold: number
  totalInvested: number
  realizedGainLoss: number
  portfolioWeight: number
  totalCredits: number
  cashFlows: AssetCashFlowDto[]
  lastMonthCredits: number
  lastCreditMonth: string | null
  lastMonthCreditsPercent: number | null
  creditFrequencyPerYear: number | null
  estimatedAnnualCredits: number | null
  estimatedAnnualPercent: number | null
  currentMonthCredits: number
}

export interface ReserveBucketBalanceDto {
  bucket: string
  balance: number
}

export interface ReserveMovementDto {
  id: string
  bucket: string
  amount: number
  date: string
  description: string
}

export interface IncomeSplitRequestDto {
  date: string
  gleisonSalaryGross: number
  gleisonSalaryNet: number
  arianaSalaryGross: number
  arianaSalaryNet: number
  lottery: number
  dividendoJuros: number
}

export interface IncomeSplitResultDto {
  dizimo: number
  investimento: number
  houseTreats: number
  ariana: number
  gleison: number
}

export interface WithdrawalRequestDto {
  bucket: string
  amount: number
  date: string
  description: string
  confirmed: boolean
}

export interface RecurringBillInstanceDto {
  id: string
  templateId: string
  year: number
  month: number
  dueDay: number
  description: string
  area: string
  note: string
  nitNumber: string | null
  minimumWageValue: number | null
  value: number
  status: string
}

export interface UpdateRecurringBillInstanceDto {
  status: string
  value: number
}

export interface MaeLedgerEntryDto {
  id: string
  date: string
  description: string
  note: string
  sourceCurrency: string
  brlValue: number | null
  gbpValue: number | null
}

export interface CreateMaeLedgerEntryDto {
  date: string
  description: string
  note: string
  sourceCurrency: string
  sourceValue: number
}

export interface UpdateMaeLedgerEntryValuesDto {
  brlValue: number | null
  gbpValue: number | null
}

export interface ExpenseDto {
  id: string
  date: string
  description: string
  value: number
  category: string
  paymentSource: string
  cardTag: string | null
}

export interface CreateExpenseDto {
  date: string
  description: string
  value: number
  category: string
  paymentSource: string
  cardTag: string | null
}

export interface UpdateExpenseDto {
  date: string
  description: string
  value: number
  category: string
  paymentSource: string
  cardTag: string | null
}

export interface CategoryTotalDto {
  category: string
  totalValue: number
}

export interface CardStatementDto {
  id: string
  card: string
  year: number
  month: number
  isPaid: boolean
  outstandingTotal: number
}

export interface CategoryYearlyTotalDto {
  category: string
  monthlyTotals: number[]
  yearlyTotal: number
}

export interface InvestmentAccountYearlyDiffDto {
  account: string
  isLiability: boolean
  monthlyValues: number[]
  monthlyDiffs: number[]
}

export interface NetPositionYearlyDiffDto {
  monthlyValues: number[]
  monthlyDiffs: number[]
  fullYearNetChange: number
}

export interface InvestmentDiffsYearlyDto {
  accounts: InvestmentAccountYearlyDiffDto[]
  netPosition: NetPositionYearlyDiffDto
}

export interface InvestmentSnapshotDto {
  id: string
  account: string
  isLiability: boolean
  year: number
  month: number
  value: number
}

export interface UpdateInvestmentSnapshotValueDto {
  value: number
}
