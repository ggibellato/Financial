> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# API Client / Config / Utilities (`*.ts` in `Financial.Web/src/api/`, `src/utils/`)

Examples: `financialApiClient.ts`, `apiError.ts`, `config.ts`, `types.ts`.

## What to test

- **`financialApiClient.ts`**: request construction (URL, method, body) and response parsing/error handling for each method — this is the one file that's *allowed* to talk about `fetch`/`Response` directly, since it *is* the system boundary everything else mocks (see `financialApiClient.test.ts`'s `okResponse`/`errorResponse` helpers)
- **`apiError.ts`**: error classification/formatting logic (`ApiError` construction from a failed response)
- **`config.ts`**: `API_BASE_URL` resolution — this has a documented project-wide invariant (see `references/gotchas.md`): it must resolve to a relative path like `/api/v1/financial` and must **never** be empty, or Docker serves the SPA fallback HTML instead of JSON. The actual default comes from `vite.config.ts`'s `define`, not a runtime fallback — don't write a test asserting an empty-string fallback, since that would codify the exact failure mode the project has already been bitten by
- **Pure utility functions**: any `.ts` function with branching or transformation logic

## Layer assignment

**Unit** — plain Vitest, no `jsdom`/RTL needed since these are not React components.

## Setup pattern

```typescript
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from './apiError'
import { createFinancialApiClient } from './financialApiClient'

const okResponse = <T,>(payload: T) =>
  ({ ok: true, status: 200, statusText: 'OK', json: async () => payload }) as Response

const errorResponse = () =>
  ({ ok: false, status: 500, statusText: 'Server Error', json: async () => ({}) }) as Response

describe('financialApiClient', () => {
  it('parses a successful JSON response', async () => {
    vi.spyOn(global, 'fetch').mockResolvedValue(okResponse({ ticker: 'BCIA11' }))

    const client = createFinancialApiClient()
    const result = await client.getAssetDetails('BCIA11', 'BVMF')

    expect(result.ticker).toBe('BCIA11')
  })

  it('throws ApiError on a non-ok response', async () => {
    vi.spyOn(global, 'fetch').mockResolvedValue(errorResponse())

    const client = createFinancialApiClient()

    await expect(client.getAssetDetails('MISSING', 'BVMF')).rejects.toThrow(ApiError)
  })
})
```

This is the only file where mocking `fetch` directly is correct — everywhere else (pages, components, hooks), mock `financialApiClient` itself instead (see `references/mock-health-rules.md`).

## When to skip

- Don't test TypeScript's own type-checking — the compiler already guarantees method signatures match `types.ts`
- Don't write a test per DTO field with no transformation — only test fields the client computes or transforms
- Don't test `config.ts`'s empty-string branch as if it were a supported, correct outcome (see above)

## Examples from project

- `financialApiClient.test.ts` — the reference example; covers every client method's success and error-mapping path using the `okResponse`/`errorResponse` helpers
