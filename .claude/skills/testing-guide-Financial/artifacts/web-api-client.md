> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Web API Client (`Financial.Web/src/api/financialApiClient.ts`, `apiError.ts`, `config.ts`)

`createFinancialApiClient(options: FinancialApiClientOptions = {}): FinancialApiClient`
(line 254) builds the client; `export const apiClient: FinancialApiClient = createFinancialApiClient()`
(line 627) is the module-level instance every hook imports. `options` accepts `baseUrl` and
`fetch`, which is how tests inject a fake transport.

## What to test

- Per method: HTTP verb, URL (`${API_BASE_URL}/expenses`, query-string parameters for GETs
  per `docs/rules/implementation.md`), `Content-Type: application/json`, serialized body,
  and the parsed return value.
- Non-2xx → `ApiError` with `status` set and the problem-details `detail`/`title` as the
  message (`problemDetailsResponse`); network rejection propagates.
- `config.ts`: trailing slash stripped from `API_BASE_URL`; the empty-string fallback is a
  broken configuration, not a supported one (`feedback_docker_api_base_url`) — the test may
  assert the strip, never that empty is fine.
- Negative: 404/409/422/500 each produce an `ApiError` the hooks' `map*ErrorToField` can
  route; a body that is not JSON does not crash the caller.

## Layer assignment

- **Unit** — `createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })` with
  `fetchMock = vi.fn().mockResolvedValue(okResponse(payload))`; the fake is the browser's
  `fetch`, i.e. the network to the separate API deployable. This is the SPA's system-boundary
  contract test, so every method gets one (single-path utility exception does not apply).
- **Integration**: the generated-types freshness test and `tsc -b`
  (`api-contract-snapshot.md`) prove the client compiles against the real contract.
- **E2E**: the smoke journey is the only real-transport check.

## Setup pattern

From `Financial.Web/src/api/__tests__/financialApiClient.test.ts`:

```ts
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../apiError'
import { API_BASE_URL } from '../config'
import { createFinancialApiClient } from '../financialApiClient'
import type { TreeNodeDto } from '../types'

const okResponse = <T,>(payload: T) =>
  ({
    ok: true,
    status: 200,
    statusText: 'OK',
    json: async () => payload,
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
    const responseBody: TreeNodeDto = { nodeType: 'Investments', displayName: 'All Investments', children: [], metadata: {} }
    const fetchMock = vi.fn().mockResolvedValue(okResponse(responseBody))
    const client = createFinancialApiClient({ baseUrl: API_BASE_URL, fetch: fetchMock })

    // act: await client.getNavigationTree(...)
    // assert: expect(fetchMock).toHaveBeenCalledWith(`${API_BASE_URL}/...`, expect.objectContaining({ method: 'GET' }))
  })
})
```

Assert on the `fetchMock` call arguments (URL + `RequestInit`) and on the returned value; use
`expect(...).rejects.toBeInstanceOf(ApiError)` for error paths.

## When to skip

- Nothing per method — but do not repeat the JSON-parsing assertion for every method; one
  shared `okResponse` helper and one `ApiError` helper are enough.

## Examples from project

- `Financial.Web/src/api/__tests__/financialApiClient.test.ts` — Unit; every client method, problem-details mapping.
- `Financial.Web/src/api/__tests__/config.test.ts` — Unit; base-URL normalisation.
