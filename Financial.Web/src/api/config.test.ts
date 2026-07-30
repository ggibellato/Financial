import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

describe('API_BASE_URL', () => {
  beforeEach(() => {
    vi.resetModules()
  })

  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('API_BASE_URL_WithTrailingSlash_StripsTrailingSlash', async () => {
    vi.stubEnv('API_BASE_URL', '/api/v1/financial/')

    const { API_BASE_URL } = await import('./config')

    expect(API_BASE_URL).toBe('/api/v1/financial')
  })

  it('API_BASE_URL_WithoutTrailingSlash_ReturnsValueUnchanged', async () => {
    vi.stubEnv('API_BASE_URL', '/api/v1/financial')

    const { API_BASE_URL } = await import('./config')

    expect(API_BASE_URL).toBe('/api/v1/financial')
  })

  it('API_BASE_URL_WhenUnset_ReturnsEmptyString', async () => {
    vi.stubEnv('API_BASE_URL', undefined)

    const { API_BASE_URL } = await import('./config')

    expect(API_BASE_URL).toBe('')
  })
})
