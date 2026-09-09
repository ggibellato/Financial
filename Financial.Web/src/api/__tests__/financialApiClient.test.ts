import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../apiError'
import { API_BASE_URL } from '../config'
import { createFinancialApiClient } from '../financialApiClient'
import type {
  ArchiveAssetRequestDto,
  AssetAdminCreateDto,
  AssetAdminDto,
  AssetAdminUpdateDto,
  AssetDetailsDto,
  AssetPriceDto,
  BalanceAdjustmentDto,
  BankBalanceDto,
  BankCreateDto,
  BankDto,
  BankUpdateDto,
  BrokerCreateDto,
  BrokerDto,
  BrokerNodeDto,
  BrokerUpdateDto,
  CalendarConnectionStatusDto,
  CalendarDisconnectResultDto,
  CardStatementDto,
  CategoryAnnualAverageDto,
  CategoryCreateDto,
  CategoryDto,
  CategoryTotalDto,
  CategoryTotalsAnnualDto,
  CategoryUpdateDto,
  CreditCardCalendarSyncStatusDto,
  BalanceAdjustmentCreateDto,
  CreditCardCreateDto,
  CreditCreateDto,
  CreditDeleteDto,
  CreditDto,
  CreditUpdateDto,
  DeleteAssetPriceDto,
  DividendHistoryItemDto,
  ExpenseCreateDto,
  ExpenseDto,
  ExpenseUpdateDto,
  IncomeCreateDto,
  IncomeDto,
  IncomeSourceCreateDto,
  IncomeSourceDto,
  IncomeSourceUpdateDto,
  IncomeUpdateDto,
  InvestmentAccountCreateDto,
  InvestmentAccountDto,
  InvestmentAccountUpdateDto,
  InvestmentAnnualResultDto,
  InvestmentSnapshotSuggestionsDto,
  MaeLedgerEntryCreateDto,
  MarkCardStatementPaidDto,
  PortfolioCreateDto,
  PortfolioDto,
  PortfolioUpdateDto,
  RecurringBillCreateDto,
  ReserveBucketCreateDto,
  ReserveBucketUpdateDto,
  SetAssetPriceDto,
  TitheCarryForwardUpdateDto,
  TitheSummaryDto,
  TransactionDeleteDto,
  TransactionSummaryItemDto,
  TransactionUpdateDto,
  TransferCreateDto,
  CreditCardDto,
  IncomeSplitRequestDto,
  IncomeSplitResultDto,
  InvestmentSnapshotDto,
  MaeLedgerEntryDto,
  MaeLedgerTotalsDto,
  RecurringBillDto,
  ReserveBucketBalanceDto,
  ReserveBucketDto,
  ReserveMovementDto,
  SyncStatusResponseDto,
  TransferDto,
  TreeNodeDto,
  BalanceAdjustmentUpdateDto,
  CreditCardUpdateDto,
  InvestmentSnapshotValueUpdateDto,
  MaeLedgerEntryValuesUpdateDto,
  RecurringBillUpdateDto,
  RecurringBillStatusUpdateDto,
  ReserveMovementUpdateDto,
  TransferUpdateDto,
  WithdrawalRequestDto,
  XirrResultDto,
} from '../types'

const okResponse = <T,>(payload: T) =>
  ({
    ok: true,
    status: 200,
    statusText: 'OK',
    json: async () => payload,
  }) as Response

const errorResponse = () =>
  ({
    ok: false,
    status: 500,
    statusText: 'Server Error',
    json: async () => ({}),
  }) as Response

const problemDetailsResponse = (detail: string) =>
  ({
    ok: false,
    status: 404,
    statusText: 'Not Found',
    text: async () => JSON.stringify({ title: 'Dividend data not found', detail, status: 404 }),
  }) as Response

describe('financialApiClient', () => {
  it('calls navigation tree endpoint', async () => {
    const responseBody: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'All Investments',
      children: [],
      metadata: {},
    }

    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.getNavigationTree()

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/navigation/tree?scope=active`)
    const headers = init?.headers as Headers
    expect(headers.get('Accept')).toBe('application/json')
    expect(headers.get('Content-Type')).toBeNull()
  })

  it('defaults to scope=active on every scope-capable endpoint when no scope is passed', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse({}))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.getNavigationTree()
    await client.getAssetDetails('XPI', 'Default', 'BCIA11')
    await client.getSummaryByBroker('XPI')
    await client.getSummaryByPortfolio('XPI', 'Default')
    await client.getBrokerBreakdown('XPI')
    await client.getPortfolioAssetsSummary('XPI', 'Default')

    const urls = fetchMock.mock.calls.map(([url]) => url as string)
    expect(urls).toEqual([
      `${API_BASE_URL}/navigation/tree?scope=active`,
      `${API_BASE_URL}/assets/XPI/Default/BCIA11?scope=active`,
      `${API_BASE_URL}/summary/broker/XPI?scope=active`,
      `${API_BASE_URL}/summary/portfolio/XPI/Default?scope=active`,
      `${API_BASE_URL}/summary/broker/XPI/breakdown?scope=active`,
      `${API_BASE_URL}/summary/portfolio/XPI/Default/assets?scope=active`,
    ])
  })

  it('requests scope=historic on every scope-capable endpoint when historic is passed', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse({}))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.getNavigationTree('historic')
    await client.getAssetDetails('XPI', 'Uncategorized', 'CLOSEDASSET', 'historic')
    await client.getSummaryByBroker('XPI', 'historic')
    await client.getSummaryByPortfolio('XPI', 'Uncategorized', 'historic')
    await client.getBrokerBreakdown('XPI', 'historic')
    await client.getPortfolioAssetsSummary('XPI', 'Uncategorized', 'historic')

    const urls = fetchMock.mock.calls.map(([url]) => url as string)
    expect(urls).toEqual([
      `${API_BASE_URL}/navigation/tree?scope=historic`,
      `${API_BASE_URL}/assets/XPI/Uncategorized/CLOSEDASSET?scope=historic`,
      `${API_BASE_URL}/summary/broker/XPI?scope=historic`,
      `${API_BASE_URL}/summary/portfolio/XPI/Uncategorized?scope=historic`,
      `${API_BASE_URL}/summary/broker/XPI/breakdown?scope=historic`,
      `${API_BASE_URL}/summary/portfolio/XPI/Uncategorized/assets?scope=historic`,
    ])
  })

  it('posts a move to the move endpoint', async () => {
    const responseBody = { name: 'BCIA11', portfolioName: 'ISA' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.moveAsset({
      brokerName: 'XPI',
      scope: 'active',
      sourcePortfolioName: 'Default',
      assetName: 'BCIA11',
      destinationPortfolioName: 'ISA',
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/assets/move`)
    expect(init?.method).toBe('POST')
    expect(init?.body).toBe(
      JSON.stringify({
        brokerName: 'XPI',
        scope: 'active',
        sourcePortfolioName: 'Default',
        assetName: 'BCIA11',
        destinationPortfolioName: 'ISA',
      }),
    )
    expect(result.portfolioName).toBe('ISA')
  })

  it('surfaces the reason a move was refused', async () => {
    // 409 carries the rule that blocked it; the UI shows that sentence verbatim.
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ status: 409, detail: 'Portfolio "ISA" already holds an asset named "BCIA11".' }), {
        status: 409,
        headers: { 'Content-Type': 'application/problem+json' },
      }),
    )
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await expect(
      client.moveAsset({
        brokerName: 'XPI',
        scope: 'active',
        sourcePortfolioName: 'Default',
        assetName: 'BCIA11',
        destinationPortfolioName: 'ISA',
      }),
    ).rejects.toThrow('already holds an asset named "BCIA11"')
  })

  it('deletes an empty portfolio, scoped', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteEmptyPortfolio('Trading 212', 'ETF ISA', 'historic')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/portfolios/Trading%20212/ETF%20ISA?scope=historic`)
    expect(init?.method).toBe('DELETE')
  })

  it('defaults an empty-portfolio deletion to the active scope', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteEmptyPortfolio('XPI', 'Stale')

    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/portfolios/XPI/Stale?scope=active`)
  })

  it('posts a new transaction', async () => {
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    await client.addTransaction({
      brokerName: 'XPI',
      portfolioName: 'Default',
      assetName: 'BCIA11',
      date: '2024-01-01T00:00:00',
      type: 'Buy',
      quantity: 1,
      unitPrice: 10,
      fees: 0,
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transactions`)
    expect(init?.method).toBe('POST')
    const headers = init?.headers as Headers
    expect(headers.get('Content-Type')).toBe('application/json')
    expect(init?.body).toBe(
      JSON.stringify({
        brokerName: 'XPI',
        portfolioName: 'Default',
        assetName: 'BCIA11',
        date: '2024-01-01T00:00:00',
        type: 'Buy',
        quantity: 1,
        unitPrice: 10,
        fees: 0,
      }),
    )
  })

  it('calls current price endpoint', async () => {
    const responseBody = {
      exchange: 'BVMF',
      ticker: 'BCIA11',
      name: 'Sample Asset',
      price: 10.5,
      asOf: '2024-02-01T00:00:00Z',
      asOfDate: null,
      isManual: false,
    } satisfies AssetPriceDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.getCurrentPrice('BVMF', 'BCIA11')

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/prices/current?exchange=BVMF&ticker=BCIA11`)
    expect(init?.method).toBeUndefined()
  })

  it('calls current price endpoint with assetClass and name for bond', async () => {
    const responseBody = {
      exchange: '',
      ticker: 'TESOURO IPCA+ 2029',
      name: 'TESOURO IPCA+ 2029',
      price: 3775.97,
      asOf: '2024-02-01T00:00:00Z',
      asOfDate: null,
      isManual: false,
    } satisfies AssetPriceDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.getCurrentPrice('', 'TESOURO IPCA+ 2029', 'Bond', undefined, 'TESOURO IPCA+ 2029')

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(
      `${API_BASE_URL}/prices/current?exchange=&ticker=TESOURO%20IPCA%2B%202029&assetClass=Bond&name=TESOURO%20IPCA%2B%202029`,
    )
  })

  it('calls current price endpoint with assetClass and brokerName for cryptocurrency', async () => {
    const responseBody = {
      exchange: '',
      ticker: 'BTC',
      name: 'Bitcoin',
      price: 48000,
      asOf: '2024-02-01T00:00:00Z',
      asOfDate: null,
      isManual: false,
    } satisfies AssetPriceDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.getCurrentPrice('', 'BTC', 'Cryptocurrency', 'Coinbase')

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(
      `${API_BASE_URL}/prices/current?exchange=&ticker=BTC&assetClass=Cryptocurrency&brokerName=Coinbase`,
    )
  })

  it('calls current price endpoint with portfolioName and assetName when provided', async () => {
    const responseBody = {
      exchange: 'BVMF',
      ticker: 'GUEP11',
      name: 'Guepardo Institucional FIC FIA',
      price: 187.42,
      asOf: null,
      asOfDate: null,
      isManual: true,
    } satisfies AssetPriceDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.getCurrentPrice(
      'BVMF',
      'GUEP11',
      undefined,
      'XPI',
      undefined,
      'Retirement',
      'Guepardo Institucional FIC FIA',
    )

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(
      `${API_BASE_URL}/prices/current?exchange=BVMF&ticker=GUEP11&brokerName=XPI&portfolioName=Retirement&assetName=Guepardo%20Institucional%20FIC%20FIA`,
    )
  })

  it('omits portfolioName and assetName from the query when not provided', async () => {
    const responseBody = {
      exchange: 'BVMF',
      ticker: 'BCIA11',
      name: 'Sample Asset',
      price: 10.5,
      asOf: '2024-02-01T00:00:00Z',
      asOfDate: null,
      isManual: false,
    } satisfies AssetPriceDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    await client.getCurrentPrice('BVMF', 'BCIA11')

    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/prices/current?exchange=BVMF&ticker=BCIA11`)
  })

  it('calls watchlist endpoint', async () => {
    const responseBody = [{ group: 'Test', name: 'KLBN4' }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getWatchlist()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/watchlist`)
  })

  it('calls asset-price-fetch endpoint', async () => {
    const responseBody = [
      { brokerName: 'XPI', portfolioName: 'FII' },
      { brokerName: 'XPI', portfolioName: 'Acoes' },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const result = await client.getAssetPriceFetchScope()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/asset-price-fetch`)
  })

  it('posts a calculate xirr request', async () => {
    const responseBody: XirrResultDto = { xirr: 0.1234 }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    const cashFlows = [{ date: '2024-01-01T00:00:00', amount: -1000 }]
    const result = await client.calculateXirr(cashFlows, 1100)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/xirr/calculate`)
    expect(init?.method).toBe('POST')
    expect(init?.body).toBe(JSON.stringify({ cashFlows, terminalValue: 1100 }))
  })

  it('throws when the API returns an error', async () => {
    const fetchMock = vi.fn().mockResolvedValue(errorResponse())
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    await expect(client.getNavigationTree()).rejects.toThrow('API request failed')
  })

  it('throws the problem-details "detail" message when the API returns one', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(problemDetailsResponse("Could not find dividend data for 'ASDF'. Check the ticker and try again."))
    const client = createFinancialApiClient({
      baseUrl: API_BASE_URL,
      fetch: fetchMock,
    })

    await expect(client.getDividendSummary('ASDF', 'BVMF')).rejects.toThrow(
      "Could not find dividend data for 'ASDF'. Check the ticker and try again.",
    )
  })

  it('throws an ApiError carrying the response status', async () => {
    const fetchMock = vi.fn().mockResolvedValue(errorResponse())
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const error = await client.getNavigationTree().catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(500)
  })

  it('throws an ApiError with status 409 on a conflict response', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 409,
      statusText: 'Conflict',
      text: async () => JSON.stringify({ detail: 'This withdrawal exceeds the balance.' }),
    } as Response)
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const error = await client
      .postWithdrawal({ bucketId: 'b3', amount: 100, date: '2026-07-01', description: 'Test', confirmed: false })
      .catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(409)
  })

  it('calls reserve balances endpoint', async () => {
    const responseBody: ReserveBucketBalanceDto[] = [{ bucketId: 'b1', bucketName: 'Investimento', balance: 654.33 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getReserveBalances()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/reserve/balances`)
  })

  it('calls reserve movements endpoint', async () => {
    const responseBody: ReserveMovementDto[] = [
      { id: 'm1', bucketId: 'b1', bucketName: 'Investimento', amount: 10, date: '2026-07-01', description: 'Test', incomeId: null },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getReserveMovements()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/reserve/movements`)
  })

  it('calls reserve buckets endpoint', async () => {
    const responseBody: ReserveBucketDto[] = [
      { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 33.33, warning: null },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getReserveBuckets()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/reserve-buckets`)
  })

  it('posts an income split request', async () => {
    const requestBody: IncomeSplitRequestDto = {
      date: '2026-07-01',
      amount: 1963,
      description: 'Ramsay',
    }
    const responseBody: IncomeSplitResultDto = {
      buckets: [
        { bucketId: 'b1', bucketName: 'Investimento', amount: 654.33 },
        { bucketId: 'b2', bucketName: 'HouseTreats', amount: 654.33 },
        { bucketId: 'b3', bucketName: 'Ariana', amount: 327.17 },
        { bucketId: 'b4', bucketName: 'Gleison', amount: 327.17 },
      ],
      total: 1963,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.postIncomeSplit(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/reserve/income-split`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts a withdrawal request', async () => {
    const requestBody: WithdrawalRequestDto = {
      bucketId: 'b1',
      amount: 30,
      date: '2026-07-01',
      description: 'Groceries top-up',
      confirmed: false,
    }
    const responseBody: ReserveMovementDto = {
      id: 'm2',
      bucketId: 'b1', bucketName: 'Investimento',
      amount: -30,
      date: '2026-07-01',
      description: 'Groceries top-up',
      incomeId: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.postWithdrawal(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/reserve/withdrawals`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a reserve movement update', async () => {
    const requestBody: ReserveMovementUpdateDto = {
      bucketId: 'b3',
      amount: -45,
      date: '2026-07-03',
      description: 'Groceries (corrected)',
    }
    const responseBody: ReserveMovementDto = {
      id: 'm2',
      bucketId: 'b3', bucketName: 'Ariana',
      amount: -45,
      date: '2026-07-03',
      description: 'Groceries (corrected)',
      incomeId: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateReserveMovement('m2', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/reserve/movements/m2`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a reserve movement', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteReserveMovement('m2')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/reserve/movements/m2`)
    expect(init?.method).toBe('DELETE')
  })

  it('calls the mensais bills endpoint', async () => {
    const responseBody: RecurringBillDto[] = [
      {
        id: 'b1',
        dueDay: 10,
        description: 'INSS',
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
        value: 850,
        status: 'Unset',
      },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getMensaisBills()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/mensais`)
  })

  it('gets the admin brokers list', async () => {
    const responseBody: BrokerDto[] = [{ name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 2 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getAdminBrokers()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/brokers`)
  })

  it('posts a broker create request', async () => {
    const requestBody: BrokerCreateDto = { name: 'XPI', currency: 'BRL' }
    const responseBody: BrokerDto = { ...requestBody, status: 'Active', portfolioCount: 0 }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createBroker(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/brokers`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a broker update', async () => {
    const requestBody: BrokerUpdateDto = { name: 'XP Investimentos', currency: 'USD' }
    const responseBody: BrokerDto = { ...requestBody, status: 'Active', portfolioCount: 0 }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateBroker('XPI', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/brokers/XPI`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a broker', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteBroker('XPI')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/brokers/XPI`)
    expect(init?.method).toBe('DELETE')
  })

  it('posts a mensais bill create request', async () => {
    const requestBody: RecurringBillCreateDto = {
      dueDay: 10,
      description: 'INSS',
      value: 850,
      area: 'Brasil',
      note: '',
    }
    const responseBody: RecurringBillDto = {
      id: 'b1',
      status: 'Unset',
      nitNumber: null,
      minimumWageValue: null,
      ...requestBody,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createMensaisBill(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/mensais`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a mensais bill update', async () => {
    const requestBody: RecurringBillUpdateDto = {
      dueDay: 10,
      description: 'INSS',
      value: 900,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Paid',
    }
    const responseBody: RecurringBillDto = {
      id: 'b1',
      dueDay: 10,
      description: 'INSS',
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      value: 900,
      status: 'Paid',
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateMensaisBill('b1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/mensais/b1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts a mensais bill status update', async () => {
    const requestBody: RecurringBillStatusUpdateDto = { status: 'Paid' }
    const responseBody: RecurringBillDto = {
      id: 'b1',
      dueDay: 10,
      description: 'INSS',
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      value: 850,
      status: 'Paid',
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateMensaisBillStatus('b1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/mensais/b1/status`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a mensais bill', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteMensaisBill('b1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/mensais/b1`)
    expect(init?.method).toBe('DELETE')
  })

  it('posts a mensais reset-to-unset request', async () => {
    const responseBody: RecurringBillDto[] = [
      {
        id: 'b1',
        dueDay: 10,
        description: 'INSS',
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
        value: 850,
        status: 'Unset',
      },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.resetMensaisToUnset()

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/mensais/reset`)
    expect(init?.method).toBe('POST')
  })

  it('posts a mae ledger entry create request', async () => {
    const requestBody: MaeLedgerEntryCreateDto = {
      date: '2026-07-01',
      description: 'School supplies',
      note: 'Term start',
      sourceCurrency: 'BRL',
      sourceValue: 350,
    }
    const responseBody: MaeLedgerEntryDto = {
      id: 'e1',
      date: '2026-07-01',
      description: 'School supplies',
      note: 'Term start',
      sourceCurrency: 'BRL',
      brlValue: 350,
      gbpValue: 51.1,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createMaeLedgerEntry(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/controle-mae/entries`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('calls the mae ledger entries-from-date endpoint', async () => {
    const responseBody: MaeLedgerEntryDto[] = [
      {
        id: 'e1',
        date: '2026-07-01',
        description: 'School supplies',
        note: '',
        sourceCurrency: 'BRL',
        brlValue: 350,
        gbpValue: 51.1,
      },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getMaeLedgerEntriesFromDate('2025-01-01')

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/controle-mae/entries/from/2025-01-01`)
  })

  it('calls the mae ledger totals endpoint', async () => {
    const responseBody: MaeLedgerTotalsDto = { totalBrlValue: 1000, totalGbpValue: 145.3 }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getMaeLedgerTotals()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/controle-mae/entries/totals`)
  })

  it('puts a mae ledger entry values update', async () => {
    const requestBody: MaeLedgerEntryValuesUpdateDto = { brlValue: 355, gbpValue: 51.6 }
    const responseBody: MaeLedgerEntryDto = {
      id: 'e1',
      date: '2026-07-01',
      description: 'School supplies',
      note: '',
      sourceCurrency: 'BRL',
      brlValue: 355,
      gbpValue: 51.6,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateMaeLedgerEntryValues('e1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/controle-mae/entries/e1/values`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a mae ledger entry', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteMaeLedgerEntry('e1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/controle-mae/entries/e1`)
    expect(init?.method).toBe('DELETE')
  })

  it('calls the investment snapshots endpoint for a given year/month', async () => {
    const responseBody: InvestmentSnapshotDto[] = [
      { id: 's1', accountId: 'a1', accountName: 'ChaseSave', isLiability: false, year: 2026, month: 7, value: 1000 },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getInvestmentSnapshots(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/investment-snapshots/2026/7`)
  })

  it('puts an investment snapshot value update', async () => {
    const requestBody: InvestmentSnapshotValueUpdateDto = { value: 1200 }
    const responseBody: InvestmentSnapshotDto = {
      id: 's1',
      accountId: 'a1',
      accountName: 'ChaseSave',
      isLiability: false,
      year: 2026,
      month: 7,
      value: 1200,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateInvestmentSnapshotValue('s1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/investment-snapshots/s1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('gets the category list', async () => {
    const responseBody: CategoryDto[] = [
      { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: false },
      { id: 'category-investimento', name: 'Investimento', active: true, isInvestment: true, isTithe: false, hasReferences: false },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCategories()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/categories`)
  })

  it('gets the credit card list', async () => {
    const responseBody: CreditCardDto[] = [
      { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
      { id: 'card-paypal', name: 'PaypalCredit', isActive: false, nextInvoiceDueDate: '2026-09-05', latestInvoiceDate: '2026-08-01', hasReferences: false },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCreditCards()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credit-cards`)
  })

  it('puts a credit card update', async () => {
    const requestBody: CreditCardUpdateDto = { name: 'BaAmex', nextInvoiceDueDate: '2026-09-05', isActive: false }
    const responseBody: CreditCardDto = { id: 'card-baamex', hasReferences: false, latestInvoiceDate: null, ...requestBody }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateCreditCard('card-baamex', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credit-cards/card-baamex`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts a transfer create request', async () => {
    const requestBody: TransferCreateDto = {
      date: '2026-07-25',
      sourceBankId: 'bank-barclays',
      destinationBankId: 'bank-trading212',
      amount: 500,
      note: 'Round-up top-up',
    }
    const responseBody: TransferDto = {
      id: 't1',
      sourceBankName: 'Barclays',
      destinationBankName: 'Trading212',
      ...requestBody,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createTransfer(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transfers`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a transfer update', async () => {
    const requestBody: TransferUpdateDto = {
      date: '2026-07-25',
      sourceBankId: 'bank-chase',
      destinationBankId: 'bank-trading212',
      amount: 250,
      note: null,
    }
    const responseBody: TransferDto = {
      id: 't1',
      sourceBankName: 'Chase',
      destinationBankName: 'Trading212',
      ...requestBody,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateTransfer('t1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transfers/t1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts a balance adjustment create request', async () => {
    const requestBody: BalanceAdjustmentCreateDto = {
      date: '2026-07-25',
      targetBalance: 2340.17,
      note: 'Matched against July statement',
    }
    const responseBody: BalanceAdjustmentDto = {
      id: 'a1',
      bankId: 'Barclays',
      bankName: 'Barclays',
      delta: -4.2,
      ...requestBody,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createBalanceAdjustment('Barclays', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks/Barclays/adjustments`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a balance adjustment update', async () => {
    const requestBody: BalanceAdjustmentUpdateDto = {
      date: '2026-07-25',
      targetBalance: 120,
      note: 'Corrected',
    }
    const responseBody: BalanceAdjustmentDto = {
      id: 'a1',
      bankId: 'Barclays',
      bankName: 'Barclays',
      delta: 20,
      ...requestBody,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateBalanceAdjustment('Barclays', 'a1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks/Barclays/adjustments/a1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('calls the transfers-by-month endpoint', async () => {
    const responseBody: TransferDto[] = [
      {
        id: 't1',
        date: '2026-07-05',
        sourceBankId: 'bank-barclays',
        sourceBankName: 'Barclays',
        destinationBankId: 'bank-trading212',
        destinationBankName: 'Trading212',
        amount: 500,
        note: null,
      },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getTransfersByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transfers/month/2026/7`)
  })

  it('deletes a transfer', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteTransfer('t1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transfers/t1`)
    expect(init?.method).toBe('DELETE')
  })

  it('calls the adjustments-by-bank endpoint', async () => {
    const responseBody: BalanceAdjustmentDto[] = [
      {
        id: 'a1',
        date: '2026-07-05',
        bankId: 'Barclays',
        bankName: 'Barclays',
        targetBalance: 150,
        delta: 50,
        note: null,
      },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getAdjustmentsByBank('Barclays')

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks/Barclays/adjustments`)
  })

  it('deletes a balance adjustment', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteBalanceAdjustment('Barclays', 'a1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks/Barclays/adjustments/a1`)
    expect(init?.method).toBe('DELETE')
  })

  it('calls sync-status endpoint', async () => {
    const responseBody: SyncStatusResponseDto = {
      cashFlow: { state: 'Idle', lastError: null, lastSuccessfulSaveUtc: null },
      investment: {
        state: 'Failed',
        lastError: 'Drive request failed with a transient status (503 ServiceUnavailable).',
        lastSuccessfulSaveUtc: '2026-08-13T09:12:04Z',
      },
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getSyncStatus()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/sync-status`)
  })

  it('calls payments-due endpoint', async () => {
    const responseBody = [{ type: 'Mensais', name: 'Internet', dueDate: '2026-09-05', daysRemaining: 3 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getPaymentsDue()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/payments-due`)
  })

  it('calls the calendar status endpoint', async () => {
    const responseBody: CalendarConnectionStatusDto = {
      connected: true,
      accountEmail: 'user@gmail.com',
      calendarName: 'Financial - Credit Card Due Dates',
      calendarId: 'abc123@group.calendar.google.com',
      connectedAtUtc: '2026-09-01T10:00:00Z',
      disconnectReason: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCalendarStatus()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/integrations/calendar/status`)
  })

  it('posts to the calendar disconnect endpoint', async () => {
    const responseBody: CalendarDisconnectResultDto = { remoteCleanupSucceeded: true }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.disconnectCalendar()

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/integrations/calendar/disconnect`)
    expect(init?.method).toBe('POST')
  })

  it('calls the calendar sync-status list endpoint', async () => {
    const responseBody: CreditCardCalendarSyncStatusDto[] = [
      { creditCardId: 'c1', state: 'Synced', lastSuccessfulSyncUtc: '2026-09-01T10:00:00Z', lastError: null },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCalendarSyncStatuses()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/integrations/calendar/credit-cards/sync-status`)
  })

  it('posts to a single card resync endpoint', async () => {
    const responseBody: CreditCardCalendarSyncStatusDto = {
      creditCardId: 'c1',
      state: 'Synced',
      lastSuccessfulSyncUtc: '2026-09-01T10:00:00Z',
      lastError: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.resyncCreditCardCalendar('c1')

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/integrations/calendar/credit-cards/c1/resync`)
    expect(init?.method).toBe('POST')
  })

  it('posts to the resync-all endpoint', async () => {
    const responseBody: CreditCardCalendarSyncStatusDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.resyncAllCalendars()

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/integrations/calendar/resync-all`)
    expect(init?.method).toBe('POST')
  })

  it('builds the calendar connect url without making a network call', () => {
    const fetchMock = vi.fn()
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const url = client.buildCalendarConnectUrl()

    expect(url).toBe(`${API_BASE_URL}/integrations/calendar/connect`)
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('throws an error carrying calendar disconnect failure details', async () => {
    const fetchMock = vi.fn().mockResolvedValue(errorResponse())
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await expect(client.disconnectCalendar()).rejects.toBeInstanceOf(ApiError)
  })

  it('throws the problem-details "title" message when the API returns one with no "detail"', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 404,
      statusText: 'Not Found',
      text: async () => JSON.stringify({ title: 'Resource not found' }),
    } as Response)
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await expect(client.getNavigationTree()).rejects.toThrow('Resource not found')
  })

  it('gets the broker navigation list', async () => {
    const responseBody: BrokerNodeDto[] = [{ name: 'XPI', currency: 'BRL', portfolioCount: 1, totalAssets: 3, portfolios: [] }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getBrokers()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/navigation/brokers`)
  })

  it('gets credits by broker', async () => {
    const responseBody: CreditDto[] = [{ id: 'c1', type: 'Dividend', value: 10, date: '2026-07-01T00:00:00Z' }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCreditsByBroker('XPI')

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/credits/broker/XPI?scope=active`)
  })

  it('gets credits by portfolio', async () => {
    const responseBody: CreditDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCreditsByPortfolio('XPI', 'Default')

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/credits/portfolio/XPI/Default?scope=active`)
  })

  it('gets transactions by broker', async () => {
    const responseBody: TransactionSummaryItemDto[] = [{ assetName: 'BCIA11', type: 'Buy', totalPrice: 100, date: '2026-07-01T00:00:00Z' }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getTransactionsByBroker('XPI')

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/transactions/broker/XPI?scope=active`)
  })

  it('gets transactions by portfolio', async () => {
    const responseBody: TransactionSummaryItemDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getTransactionsByPortfolio('XPI', 'Default')

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/transactions/portfolio/XPI/Default?scope=active`)
  })

  it('posts an archive-asset request', async () => {
    const requestBody: ArchiveAssetRequestDto = {
      brokerName: 'XPI',
      sourcePortfolioName: 'Default',
      assetName: 'CLOSEDASSET',
      destinationPortfolioName: 'Uncategorized',
    }
    const responseBody = { name: 'CLOSEDASSET', portfolioName: 'Uncategorized' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.archiveAsset(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/assets/archive`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('gets the admin portfolios list', async () => {
    const responseBody: PortfolioDto[] = [{ name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 3 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getAdminPortfolios()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/portfolios`)
  })

  it('posts a portfolio create request', async () => {
    const requestBody: PortfolioCreateDto = { brokerName: 'XPI', name: 'ISA' }
    const responseBody: PortfolioDto = { name: 'ISA', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 0 }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createPortfolio(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/portfolios`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a portfolio update', async () => {
    const requestBody: PortfolioUpdateDto = { name: 'ISA Renamed' }
    const responseBody: PortfolioDto = { name: 'ISA Renamed', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 0 }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updatePortfolio('XPI', 'ISA', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/portfolios/XPI/ISA`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('gets the admin assets list', async () => {
    const responseBody = [{ name: 'BCIA11', brokerName: 'XPI', portfolioName: 'Default', brokerStatus: 'Active' }] as AssetAdminDto[]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getAdminAssets()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/assets`)
  })

  it('posts an asset create request', async () => {
    const requestBody = { brokerName: 'XPI', portfolioName: 'Default', name: 'BCIA11' } as AssetAdminCreateDto
    const responseBody = { name: 'BCIA11', brokerName: 'XPI', portfolioName: 'Default', brokerStatus: 'Active' } as AssetAdminDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createAsset(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/assets`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts an asset update', async () => {
    const requestBody = { name: 'BCIA11 Renamed' } as AssetAdminUpdateDto
    const responseBody = { name: 'BCIA11 Renamed', brokerName: 'XPI', portfolioName: 'Default', brokerStatus: 'Active' } as AssetAdminDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateAsset('XPI', 'Default', 'BCIA11', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/assets/XPI/Default/BCIA11`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a transaction update', async () => {
    const requestBody: TransactionUpdateDto = {
      id: 'tx1',
      brokerName: 'XPI',
      portfolioName: 'Default',
      assetName: 'BCIA11',
      type: 'Buy',
      date: '2026-07-01T00:00:00Z',
      fees: 0,
      quantity: 10,
      unitPrice: 10,
    }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateTransaction(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transactions`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a transaction', async () => {
    const requestBody: TransactionDeleteDto = { id: 'tx1', brokerName: 'XPI', portfolioName: 'Default', assetName: 'BCIA11' }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.deleteTransaction(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/transactions`)
    expect(init?.method).toBe('DELETE')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts a credit create request', async () => {
    const requestBody: CreditCreateDto = { brokerName: 'XPI', portfolioName: 'Default', assetName: 'BCIA11', type: 'Dividend', value: 10, date: '2026-07-01T00:00:00Z' }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.addCredit(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credits`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a credit update', async () => {
    const requestBody: CreditUpdateDto = { id: 'c1', brokerName: 'XPI', portfolioName: 'Default', assetName: 'BCIA11', type: 'Dividend', value: 12, date: '2026-07-01T00:00:00Z' }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateCredit(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credits`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a credit', async () => {
    const requestBody: CreditDeleteDto = { id: 'c1', brokerName: 'XPI', portfolioName: 'Default', assetName: 'BCIA11' }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.deleteCredit(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credits`)
    expect(init?.method).toBe('DELETE')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a manual asset price', async () => {
    const requestBody: SetAssetPriceDto = { brokerName: 'XPI', portfolioName: 'Default', assetName: 'BCIA11', date: '2026-07-01', price: 12.5 }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.setAssetPrice(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/prices`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a manual asset price', async () => {
    const requestBody: DeleteAssetPriceDto = { brokerName: 'XPI', portfolioName: 'Default', assetName: 'BCIA11', date: '2026-07-01' }
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.deleteAssetPrice(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/prices`)
    expect(init?.method).toBe('DELETE')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('gets dividend history', async () => {
    const responseBody: DividendHistoryItemDto[] = [{ date: '2026-07-01T00:00:00Z', type: 'Dividend', value: 1.5 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getDividendHistory('BCIA11', 'BVMF')

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/dividends/BCIA11/history?exchange=BVMF`)
  })

  it('posts a reserve bucket create request', async () => {
    const requestBody: ReserveBucketCreateDto = { name: 'Travel', splitPercentage: 20, isActive: true }
    const responseBody: ReserveBucketDto = { id: 'rb1', name: 'Travel', splitPercentage: 20, isActive: true, warning: null }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createReserveBucket(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/reserve-buckets`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a reserve bucket update', async () => {
    const requestBody: ReserveBucketUpdateDto = { name: 'Travel', splitPercentage: 25, isActive: true }
    const responseBody: ReserveBucketDto = { id: 'rb1', name: 'Travel', splitPercentage: 25, isActive: true, warning: null }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateReserveBucket('rb1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/reserve-buckets/rb1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('gets investment snapshot suggestions', async () => {
    const responseBody = { suggestions: [], notUpdated: [] } as InvestmentSnapshotSuggestionsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getInvestmentSnapshotSuggestions(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/investment-snapshots/2026/7/suggestions`)
  })

  it('gets expenses by month', async () => {
    const responseBody: ExpenseDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getExpensesByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/expenses/month/2026/7`)
  })

  it('gets unpaid card charges by month', async () => {
    const responseBody: ExpenseDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getUnpaidCardChargesByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/expenses/month/2026/7/unpaid-card-charges`)
  })

  it('gets category totals by month', async () => {
    const responseBody: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 250 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCategoryTotalsByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/expenses/month/2026/7/category-totals`)
  })

  it('gets the bank list', async () => {
    const responseBody: BankDto[] = [{ id: 'bank-barclays', name: 'Barclays', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getBanks()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/banks`)
  })

  it('posts a bank create request', async () => {
    const requestBody: BankCreateDto = { name: 'Barclays', roundUpEnabled: true }
    const responseBody: BankDto = { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createBank(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a bank update', async () => {
    const requestBody: BankUpdateDto = { name: 'Barclays Renamed', roundUpEnabled: false }
    const responseBody: BankDto = { id: 'bank-barclays', name: 'Barclays Renamed', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateBank('bank-barclays', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks/bank-barclays`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a bank', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteBank('bank-barclays')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/banks/bank-barclays`)
    expect(init?.method).toBe('DELETE')
  })

  it('gets the income source list', async () => {
    const responseBody: IncomeSourceDto[] = [{ id: 'is1', name: 'Salary', group: 'Salary', isActive: true, autoSplitToReserve: false, hasReferences: false }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getIncomeSources()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/income-sources`)
  })

  it('posts an income source create request', async () => {
    const requestBody: IncomeSourceCreateDto = { name: 'Salary', group: 'Salary', isActive: true, autoSplitToReserve: false }
    const responseBody: IncomeSourceDto = { id: 'is1', ...requestBody, hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createIncomeSource(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/income-sources`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts an income source update', async () => {
    const requestBody: IncomeSourceUpdateDto = { name: 'Salary Renamed', group: 'Salary', isActive: true, autoSplitToReserve: false }
    const responseBody: IncomeSourceDto = { id: 'is1', ...requestBody, hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateIncomeSource('is1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/income-sources/is1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes an income source', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteIncomeSource('is1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/income-sources/is1`)
    expect(init?.method).toBe('DELETE')
  })

  it('posts a category create request', async () => {
    const requestBody: CategoryCreateDto = { name: 'Lazer', active: true, isInvestment: false, isTithe: false }
    const responseBody: CategoryDto = { id: 'category-lazer', ...requestBody, hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createCategory(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/categories`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts a category update', async () => {
    const requestBody: CategoryUpdateDto = { name: 'Lazer Renamed', active: true, isInvestment: false, isTithe: false }
    const responseBody: CategoryDto = { id: 'category-lazer', ...requestBody, hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateCategory('category-lazer', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/categories/category-lazer`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a category', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteCategory('category-lazer')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/categories/category-lazer`)
    expect(init?.method).toBe('DELETE')
  })

  it('posts a credit card create request', async () => {
    const requestBody: CreditCardCreateDto = { name: 'BaAmex', isActive: true }
    const responseBody: CreditCardDto = { id: 'card-baamex', ...requestBody, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createCreditCard(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credit-cards`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes a credit card', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteCreditCard('card-baamex')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credit-cards/card-baamex`)
    expect(init?.method).toBe('DELETE')
  })

  it('gets the investment account list', async () => {
    const responseBody: InvestmentAccountDto[] = [
      { id: 'ia1', name: 'ChaseSave', isActive: true, isLiability: false, hasNonZeroInvestmentSnapshot: false, creditCardId: null, source: 'None' },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getInvestmentAccounts()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/investment-accounts`)
  })

  it('posts an investment account create request', async () => {
    const requestBody: InvestmentAccountCreateDto = { name: 'ChaseSave', isActive: true, isLiability: false, creditCardId: null, source: 'None' }
    const responseBody: InvestmentAccountDto = { id: 'ia1', ...requestBody, hasNonZeroInvestmentSnapshot: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createInvestmentAccount(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/investment-accounts`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts an investment account update', async () => {
    const requestBody: InvestmentAccountUpdateDto = { name: 'ChaseSave Renamed', isActive: true, isLiability: false, creditCardId: null, source: 'None' }
    const responseBody: InvestmentAccountDto = { id: 'ia1', ...requestBody, hasNonZeroInvestmentSnapshot: false }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateInvestmentAccount('ia1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/investment-accounts/ia1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes an investment account', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteInvestmentAccount('ia1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/investment-accounts/ia1`)
    expect(init?.method).toBe('DELETE')
  })

  it('gets bank balances by month', async () => {
    const responseBody: BankBalanceDto[] = [{ bank: 'Barclays', balance: 1500 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getBankBalancesByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/banks/month/2026/7/balances`)
  })

  it('posts an expense create request', async () => {
    const requestBody: ExpenseCreateDto = {
      categoryId: 'category-mercado',
      date: '2026-07-01',
      description: 'Groceries',
      value: 100,
      countsAsTithe: true,
      creditCardId: null,
      invoiceDate: null,
      paymentSourceBankId: null,
      roundUpAmount: null,
    }
    const responseBody = { id: 'e1', categoryName: 'Mercado', paymentStatus: 'ImmediatePayment', ...requestBody } as ExpenseDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createExpense(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/expenses`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts an expense update', async () => {
    const requestBody: ExpenseUpdateDto = {
      categoryId: 'category-mercado',
      date: '2026-07-01',
      description: 'Groceries',
      value: 120,
      countsAsTithe: true,
      creditCardId: null,
      invoiceDate: null,
      paymentSourceBankId: null,
      roundUpAmount: null,
    }
    const responseBody = { id: 'e1', categoryName: 'Mercado', paymentStatus: 'ImmediatePayment', ...requestBody } as ExpenseDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateExpense('e1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/expenses/e1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes an expense', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteExpense('e1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/expenses/e1`)
    expect(init?.method).toBe('DELETE')
  })

  it('gets incomes by month', async () => {
    const responseBody: IncomeDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getIncomesByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/incomes/month/2026/7`)
  })

  it('gets tithe summary by month', async () => {
    const responseBody: TitheSummaryDto = { calculatedTithe: 100, titheBalance: 0, carryForward: null }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getTitheSummaryByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/tithe/month/2026/7`)
  })

  it('puts a tithe carry-forward update', async () => {
    const requestBody: TitheCarryForwardUpdateDto = { included: true }
    const responseBody: TitheSummaryDto = { calculatedTithe: 100, titheBalance: 0, carryForward: { amount: 50, fromMonth: 6, fromYear: 2026, included: true } }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateTitheCarryForward(2026, 7, requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/tithe/month/2026/7/carry-forward`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts an income create request', async () => {
    const requestBody: IncomeCreateDto = {
      incomeSourceId: 'is1',
      date: '2026-07-01',
      netValue: 3000,
      bankId: null,
      description: null,
      grossValue: null,
      splitToReserve: false,
    }
    const responseBody = { id: 'i1', incomeSourceName: 'Salary', ...requestBody } as IncomeDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.createIncome(requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/incomes`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('puts an income update', async () => {
    const requestBody: IncomeUpdateDto = {
      incomeSourceId: 'is1',
      date: '2026-07-01',
      netValue: 3200,
      bankId: null,
      description: null,
      grossValue: null,
      splitToReserve: false,
    }
    const responseBody = { id: 'i1', incomeSourceName: 'Salary', ...requestBody } as IncomeDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.updateIncome('i1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/incomes/i1`)
    expect(init?.method).toBe('PUT')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('deletes an income', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okResponse(undefined))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    await client.deleteIncome('i1')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/incomes/i1`)
    expect(init?.method).toBe('DELETE')
  })

  it('gets card statements by month', async () => {
    const responseBody: CardStatementDto[] = []
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCardStatementsByMonth(2026, 7)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/card-statements/2026/7`)
  })

  it('posts a mark-card-statement-paid request', async () => {
    const requestBody: MarkCardStatementPaidDto = { paymentSourceBankId: 'bank-barclays' }
    const responseBody: CardStatementDto = {
      id: 'cs1',
      creditCardId: 'card-baamex',
      creditCardName: 'BaAmex',
      year: 2026,
      month: 7,
      isPaid: true,
      outstandingTotal: 0,
      accumulatedOutstandingTotal: 0,
      warning: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.markCardStatementPaid('cs1', requestBody)

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/card-statements/cs1/mark-paid`)
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual(requestBody)
  })

  it('posts an unmark-card-statement-paid request', async () => {
    const responseBody: CardStatementDto = {
      id: 'cs1',
      creditCardId: 'card-baamex',
      creditCardName: 'BaAmex',
      year: 2026,
      month: 7,
      isPaid: false,
      outstandingTotal: 100,
      accumulatedOutstandingTotal: 100,
      warning: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.unmarkCardStatementPaid('cs1')

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/card-statements/cs1/unmark-paid`)
    expect(init?.method).toBe('POST')
  })

  it('gets category totals for a year', async () => {
    const responseBody = { categoryTotals: [] } as unknown as CategoryTotalsAnnualDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCategoryTotalsAnnualForYear(2026)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/annual-summary/2026/category-totals`)
  })

  it('gets the investment annual result for a year', async () => {
    const responseBody = { accounts: [] } as unknown as InvestmentAnnualResultDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getInvestmentAnnualResultForYear(2026)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/annual-summary/2026/investment-annual-result`)
  })

  it('gets the historic summary average from a year', async () => {
    const responseBody: CategoryAnnualAverageDto[] = [{ year: 2026, annualAverages: [{ category: 'Mercado', value: 250 }] }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getHistoricSummaryAverageFromYear(2026)

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/annual-summary/2026/historic-summary-averages`)
  })
})
