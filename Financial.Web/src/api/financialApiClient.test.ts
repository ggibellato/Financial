import { describe, expect, it, vi } from 'vitest'
import { ApiError } from './apiError'
import { API_BASE_URL } from './config'
import { createFinancialApiClient } from './financialApiClient'
import type {
  AssetDetailsDto,
  AssetPriceDto,
  BalanceAdjustmentDto,
  CategoryDto,
  CreateBalanceAdjustmentDto,
  CreateMaeLedgerEntryDto,
  CreateRecurringBillDto,
  CreateTransferDto,
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
  UpdateBalanceAdjustmentDto,
  UpdateCreditCardDto,
  UpdateInvestmentSnapshotValueDto,
  UpdateMaeLedgerEntryValuesDto,
  UpdateRecurringBillDto,
  UpdateReserveMovementDto,
  UpdateTransferDto,
  WithdrawalRequestDto,
  XirrResultDto,
} from './types'

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
      .postWithdrawal({ bucket: 'Ariana', amount: 100, date: '2026-07-01', description: 'Test', confirmed: false })
      .catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(409)
  })

  it('calls reserve balances endpoint', async () => {
    const responseBody: ReserveBucketBalanceDto[] = [{ bucket: 'Investimento', balance: 654.33 }]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getReserveBalances()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/reserve/balances`)
  })

  it('calls reserve movements endpoint', async () => {
    const responseBody: ReserveMovementDto[] = [
      { id: 'm1', bucket: 'Investimento', amount: 10, date: '2026-07-01', description: 'Test' },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getReserveMovements()

    expect(result).toEqual(responseBody)
    expect(fetchMock.mock.calls[0][0]).toBe(`${API_BASE_URL}/reserve/movements`)
  })

  it('calls reserve buckets endpoint', async () => {
    const responseBody: ReserveBucketDto[] = [
      { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 33.33 },
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
        { bucket: 'Investimento', amount: 654.33 },
        { bucket: 'HouseTreats', amount: 654.33 },
        { bucket: 'Ariana', amount: 327.17 },
        { bucket: 'Gleison', amount: 327.17 },
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
      bucket: 'Investimento',
      amount: 30,
      date: '2026-07-01',
      description: 'Groceries top-up',
      confirmed: false,
    }
    const responseBody: ReserveMovementDto = {
      id: 'm2',
      bucket: 'Investimento',
      amount: -30,
      date: '2026-07-01',
      description: 'Groceries top-up',
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
    const requestBody: UpdateReserveMovementDto = {
      bucket: 'Ariana',
      amount: -45,
      date: '2026-07-03',
      description: 'Groceries (corrected)',
    }
    const responseBody: ReserveMovementDto = {
      id: 'm2',
      bucket: 'Ariana',
      amount: -45,
      date: '2026-07-03',
      description: 'Groceries (corrected)',
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

  it('posts a mensais bill create request', async () => {
    const requestBody: CreateRecurringBillDto = {
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
    const requestBody: UpdateRecurringBillDto = { status: 'Paid', value: 900 }
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
    const requestBody: CreateMaeLedgerEntryDto = {
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
    const requestBody: UpdateMaeLedgerEntryValuesDto = { brlValue: 355, gbpValue: 51.6 }
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
    const requestBody: UpdateInvestmentSnapshotValueDto = { value: 1200 }
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
      { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false },
      { id: 'category-investimento', name: 'Investimento', active: true, isInvestment: true, isTithe: false },
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
      { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null },
      { id: 'card-paypal', name: 'PaypalCredit', isActive: false, nextInvoiceDueDate: '2026-09-05' },
    ]
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    const result = await client.getCreditCards()

    expect(result).toEqual(responseBody)
    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe(`${API_BASE_URL}/credit-cards`)
  })

  it('puts a credit card update', async () => {
    const requestBody: UpdateCreditCardDto = { nextInvoiceDueDate: '2026-09-05', isActive: false }
    const responseBody: CreditCardDto = { id: 'card-baamex', name: 'BaAmex', ...requestBody }
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
    const requestBody: CreateTransferDto = {
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
    const requestBody: UpdateTransferDto = {
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
    const requestBody: CreateBalanceAdjustmentDto = {
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
    const requestBody: UpdateBalanceAdjustmentDto = {
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
})
