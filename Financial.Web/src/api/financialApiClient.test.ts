import { describe, expect, it, vi } from 'vitest'
import { resolveApiBaseUrl } from './config'
import { createFinancialApiClient } from './financialApiClient'
import type { AssetDetailsDto, TreeNodeDto } from './types'

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

describe('resolveApiBaseUrl', () => {
  it('uses the default when empty', () => {
    expect(resolveApiBaseUrl('')).toBe('http://localhost:5190/api/v1/financial')
  })

  it('trims trailing slash', () => {
    expect(resolveApiBaseUrl('http://example/api/')).toBe('http://example/api')
  })
})

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
      baseUrl: 'http://localhost:5190/api/v1/financial',
      fetch: fetchMock,
    })

    const result = await client.getNavigationTree()

    expect(result).toEqual(responseBody)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('http://localhost:5190/api/v1/financial/navigation/tree')
    expect(init?.headers).toEqual(
      expect.objectContaining({
        Accept: 'application/json',
      }),
    )
  })

  it('posts a new operation', async () => {
    const responseBody = { name: 'BCIA11' } as AssetDetailsDto
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({
      baseUrl: 'http://localhost:5190/api/v1/financial',
      fetch: fetchMock,
    })

    await client.addOperation({
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
    expect(url).toBe('http://localhost:5190/api/v1/financial/operations')
    expect(init?.method).toBe('POST')
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

  it('throws when the API returns an error', async () => {
    const fetchMock = vi.fn().mockResolvedValue(errorResponse())
    const client = createFinancialApiClient({
      baseUrl: 'http://localhost:5190/api/v1/financial',
      fetch: fetchMock,
    })

    await expect(client.getNavigationTree()).rejects.toThrow('API request failed')
  })
})
