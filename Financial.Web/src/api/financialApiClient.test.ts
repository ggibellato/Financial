import { describe, expect, it, vi } from 'vitest'
import { API_BASE_URL } from './config'
import { createFinancialApiClient } from './financialApiClient'
import type { AssetDetailsDto, AssetPriceDto, TreeNodeDto, XirrResultDto } from './types'

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
    expect(url).toBe(`${API_BASE_URL}/navigation/tree`)
    const headers = init?.headers as Headers
    expect(headers.get('Accept')).toBe('application/json')
    expect(headers.get('Content-Type')).toBeNull()
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
})
