import { ApiError } from './apiError'
import { API_BASE_URL } from './config'
import type {
  AggregatedSummaryDto,
  AssetCashFlowDto,
  AssetDetailsDto,
  AssetPriceDto,
  BrokerNodeDto,
  CalculateXirrRequestDto,
  CardStatementDto,
  CategoryTotalDto,
  CategoryYearlyTotalDto,
  CreateExpenseDto,
  CreateMaeLedgerEntryDto,
  CreateRecurringBillDto,
  CreditCreateDto,
  CreditDeleteDto,
  CreditDto,
  CreditUpdateDto,
  DividendHistoryItemDto,
  DividendSummaryDto,
  ExpenseDto,
  IncomeSplitRequestDto,
  IncomeSplitResultDto,
  InvestmentDiffsYearlyDto,
  InvestmentScope,
  InvestmentSnapshotDto,
  MaeLedgerEntryDto,
  MaeLedgerTotalsDto,
  PortfolioAssetSummaryItemDto,
  PortfolioBreakdownItemDto,
  PortfolioReferenceDto,
  RecurringBillDto,
  ReserveBucketBalanceDto,
  ReserveMovementDto,
  TransactionCreateDto,
  TransactionDeleteDto,
  TransactionSummaryItemDto,
  TransactionUpdateDto,
  TreeNodeDto,
  UpdateExpenseDto,
  UpdateInvestmentSnapshotValueDto,
  UpdateMaeLedgerEntryValuesDto,
  UpdateRecurringBillDto,
  WatchlistItemDto,
  WithdrawalRequestDto,
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
  getReserveBalances: () => Promise<ReserveBucketBalanceDto[]>
  getReserveMovements: () => Promise<ReserveMovementDto[]>
  postIncomeSplit: (request: IncomeSplitRequestDto) => Promise<IncomeSplitResultDto>
  postWithdrawal: (request: WithdrawalRequestDto) => Promise<ReserveMovementDto>
  getMensaisBills: () => Promise<RecurringBillDto[]>
  createMensaisBill: (request: CreateRecurringBillDto) => Promise<RecurringBillDto>
  updateMensaisBill: (id: string, request: UpdateRecurringBillDto) => Promise<RecurringBillDto>
  deleteMensaisBill: (id: string) => Promise<void>
  resetMensaisToUnset: () => Promise<RecurringBillDto[]>
  createMaeLedgerEntry: (request: CreateMaeLedgerEntryDto) => Promise<MaeLedgerEntryDto>
  getMaeLedgerEntriesFromDate: (fromDate: string) => Promise<MaeLedgerEntryDto[]>
  getMaeLedgerTotals: () => Promise<MaeLedgerTotalsDto>
  updateMaeLedgerEntryValues: (id: string, request: UpdateMaeLedgerEntryValuesDto) => Promise<MaeLedgerEntryDto>
  deleteMaeLedgerEntry: (id: string) => Promise<void>
  getInvestmentSnapshots: (year: number, month: number) => Promise<InvestmentSnapshotDto[]>
  updateInvestmentSnapshotValue: (id: string, request: UpdateInvestmentSnapshotValueDto) => Promise<InvestmentSnapshotDto>
  getExpensesByMonth: (year: number, month: number) => Promise<ExpenseDto[]>
  getCategoryTotalsByMonth: (year: number, month: number) => Promise<CategoryTotalDto[]>
  createExpense: (request: CreateExpenseDto) => Promise<ExpenseDto>
  updateExpense: (id: string, request: UpdateExpenseDto) => Promise<ExpenseDto>
  deleteExpense: (id: string) => Promise<void>
  getCardStatementsByMonth: (year: number, month: number) => Promise<CardStatementDto[]>
  markCardStatementPaid: (id: string) => Promise<CardStatementDto>
  getCategoryTotalsForYear: (year: number) => Promise<CategoryYearlyTotalDto[]>
  getInvestmentDiffsForYear: (year: number) => Promise<InvestmentDiffsYearlyDto>
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

  const sendRequest = async (path: string, init?: RequestInit): Promise<Response> => {
    const url = `${baseUrl}${path}`
    const response = await fetcher(url, init)

    if (!response.ok) {
      const method = init?.method ?? 'GET'
      throw new ApiError(await buildErrorMessage(response, method, url), response.status)
    }

    return response
  }

  const request = async <T>(path: string, init?: RequestInit): Promise<T> => {
    const headers = new Headers(init?.headers)
    if (!headers.has('Accept')) {
      headers.set('Accept', 'application/json')
    }
    if (init?.body !== undefined && init?.body !== null && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    const response = await sendRequest(path, { ...init, headers })
    return (await response.json()) as T
  }

  const requestVoid = async (path: string, init?: RequestInit): Promise<void> => {
    await sendRequest(path, init)
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
    getReserveBalances: () => request<ReserveBucketBalanceDto[]>('/reserve/balances'),
    getReserveMovements: () => request<ReserveMovementDto[]>('/reserve/movements'),
    postIncomeSplit: (requestBody) =>
      request<IncomeSplitResultDto>('/reserve/income-split', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    postWithdrawal: (requestBody) =>
      request<ReserveMovementDto>('/reserve/withdrawals', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    getMensaisBills: () => request<RecurringBillDto[]>('/mensais'),
    createMensaisBill: (requestBody) =>
      request<RecurringBillDto>('/mensais', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    updateMensaisBill: (id, requestBody) =>
      request<RecurringBillDto>(`/mensais/${encodeURIComponent(id)}`, {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    deleteMensaisBill: (id) => requestVoid(`/mensais/${encodeURIComponent(id)}`, { method: 'DELETE' }),
    resetMensaisToUnset: () => request<RecurringBillDto[]>('/mensais/reset', { method: 'POST' }),
    createMaeLedgerEntry: (requestBody) =>
      request<MaeLedgerEntryDto>('/controle-mae/entries', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    getMaeLedgerEntriesFromDate: (fromDate) =>
      request<MaeLedgerEntryDto[]>(`/controle-mae/entries/from/${fromDate}`),
    getMaeLedgerTotals: () => request<MaeLedgerTotalsDto>('/controle-mae/entries/totals'),
    updateMaeLedgerEntryValues: (id, requestBody) =>
      request<MaeLedgerEntryDto>(`/controle-mae/entries/${encodeURIComponent(id)}/values`, {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    deleteMaeLedgerEntry: (id) => requestVoid(`/controle-mae/entries/${encodeURIComponent(id)}`, { method: 'DELETE' }),
    getInvestmentSnapshots: (year, month) =>
      request<InvestmentSnapshotDto[]>(`/investment-snapshots/${year}/${month}`),
    updateInvestmentSnapshotValue: (id, requestBody) =>
      request<InvestmentSnapshotDto>(`/investment-snapshots/${encodeURIComponent(id)}`, {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    getExpensesByMonth: (year, month) => request<ExpenseDto[]>(`/expenses/month/${year}/${month}`),
    getCategoryTotalsByMonth: (year, month) =>
      request<CategoryTotalDto[]>(`/expenses/month/${year}/${month}/category-totals`),
    createExpense: (requestBody) =>
      request<ExpenseDto>('/expenses', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      }),
    updateExpense: (id, requestBody) =>
      request<ExpenseDto>(`/expenses/${encodeURIComponent(id)}`, {
        method: 'PUT',
        body: JSON.stringify(requestBody),
      }),
    deleteExpense: (id) => requestVoid(`/expenses/${encodeURIComponent(id)}`, { method: 'DELETE' }),
    getCardStatementsByMonth: (year, month) =>
      request<CardStatementDto[]>(`/card-statements/${year}/${month}`),
    markCardStatementPaid: (id) =>
      request<CardStatementDto>(`/card-statements/${encodeURIComponent(id)}/mark-paid`, {
        method: 'POST',
      }),
    getCategoryTotalsForYear: (year) =>
      request<CategoryYearlyTotalDto[]>(`/yearly-summary/${year}/expense-categories`),
    getInvestmentDiffsForYear: (year) =>
      request<InvestmentDiffsYearlyDto>(`/yearly-summary/${year}/investment-diffs`),
  }
}
