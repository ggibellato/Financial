> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# API Client / Config / Utilities (`*.ts` in `Financial.Web/src/api/`)

## What to test

- **`config.ts`**: the exported URL is read from `import.meta.env.API_BASE_URL` — use `vi.stubEnv` to inject the env var and verify the value
- **`financialApiClient.ts`**: the client itself is tested indirectly through page component tests via `vi.mock`. Add direct tests only if there is pure transformation logic (e.g., mapping a raw response shape to an internal DTO) that is separate from the HTTP call
- **Pure utility functions**: any `.ts` function with branching or transformation logic

## Layer assignment

**Unit** — pure functions, no DOM, no HTTP calls. Config tests use `vi.stubEnv` to inject environment variables.

## Setup pattern

```typescript
// Config test — must reset modules so env var change is re-evaluated at import time
import { describe, it, expect, vi, beforeEach } from 'vitest'

describe('config', () => {
  beforeEach(() => {
    vi.resetModules()     // force re-import so stubbed env is picked up
    vi.unstubAllEnvs()
  })

  it('reads API_BASE_URL from environment', async () => {
    vi.stubEnv('API_BASE_URL', 'http://test-api.example.com')

    const { apiBaseUrl } = await import('./config')

    expect(apiBaseUrl).toBe('http://test-api.example.com')
  })

  it('returns empty string when API_BASE_URL is not set', async () => {
    // env var not stubbed — falls back to undefined / empty

    const { apiBaseUrl } = await import('./config')

    expect(apiBaseUrl).toBe('')
  })
})

// Pure utility function test
import { describe, it, expect } from 'vitest'
import { transformResponseData } from './utils'

describe('transformResponseData', () => {
  it('maps raw response fields to internal shape', () => {
    const raw = { raw_field: 'value', amount: 100 }

    const result = transformResponseData(raw)

    expect(result.mappedField).toBe('value')
    expect(result.amount).toBe(100)
  })

  it('handles empty response', () => {
    const result = transformResponseData({})

    expect(result).toBeDefined()
  })
})
```

**`vi.resetModules()`**: required when testing config values because `import.meta.env.*` is evaluated at module load time. Without resetting, a cached module is returned and the stubbed env is never read.

## When to skip

- HTTP fetch calls inside `financialApiClient.ts` — these are tested implicitly through page component tests that mock the client factory
- TypeScript type definitions in `types.ts` — the compiler verifies these; no runtime test needed
- The `createFinancialApiClient` factory function itself — it's a constructor wrapper; its behavior is covered by page tests

## Examples from project

`config.ts` reads `API_BASE_URL` from `import.meta.env`. A test that stubs this env var, resets modules, re-imports config, and asserts the exported value covers the environment variable wiring without making any HTTP call.
