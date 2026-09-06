> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# React Hooks (`Financial.Web/src/hooks/use*.ts`)

Two shapes exist: data hooks built on `useAsyncResource(fetcher, deps, errorFallback)`
(returns `{ data, isLoading, error, retry }`) — `useBanks`, `useMensais`, `useAnnualSummary`,
`useAggregatedSummary`, … — and form/state hooks (`useExpenseForm`, `useTransferForm`,
`useBalanceAdjustmentForm`, `useFieldError`, `useColumnFilters`, `useSortableRows`) plus the
error-to-field mappers (`mapTransferErrorToField.ts`, `mapBalanceAdjustmentErrorToField.ts`).

## What to test

- **Data hooks**: `isLoading` true on mount then false; `apiClient.getX` called exactly once
  (no polling — `usePaymentsDue.does_not_poll` advances 60 s of fake time and asserts one
  call); data equals the mocked payload; a rejected promise → `error` set to the message (or
  the `errorFallback`) and `data` null; `retry()` re-fetches; a `null` fetcher (no selection)
  → idle state; deps change → refetch with the new argument; selection-driven hooks via
  `createSelectedNodeWrapper()` from `src/test-utils/selectedNodeTestWrapper.tsx`.
- **Mutation hooks** (`create*`/`update*`/`delete*` in `useBanks` etc.): the client method is
  called with the DTO, the list is refreshed or patched, a failure surfaces `saveError` /
  `saveErrorFields` and leaves the list untouched.
- **Form hooks**: defaults (`createFormDefaults`, remembered last bank —
  `useBalanceAdjustmentForm` P38-F10), validation messages per field, `isSaving` blocks a second
  submit, backend `ApiError` mapped to the named field by `map*ErrorToField`.
- **Timers**: auto-dismiss after `PAYMENT_DUE_BANNER_DISMISS_MS` with
  `vi.useFakeTimers({ shouldAdvanceTime: true })` + `vi.advanceTimersByTimeAsync`.
- Negative: rejected fetch, `ApiError` with a status the mapper does not know (falls back to a
  general error), empty arrays.

## Layer assignment

- **Unit** — `renderHook` from `@testing-library/react`, with `financialApiClient` replaced at
  module level by `vi.mock('../../api/financialApiClient', …)`. The API is a separate
  deployable, so this fake is the sanctioned one (`../references/mock-health-rules.md`); nothing
  else is mocked. Hooks that read `localStorage`/`sessionStorage` use the real jsdom storage and
  clear it in `afterEach`.
- **Integration (frontend)** happens at the page level (`react-pages.md`); the hook's own
  Integration coverage is whichever page composes it.
- **No E2E of its own**; the smoke journey exercises `useAnnualSummary` end to end.

## Setup pattern

From `Financial.Web/src/hooks/__tests__/useBanks.test.ts`:

```ts
import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto } from '../../api/types'
import { useBanks } from '../useBanks'

const { getBanksMock, createBankMock, updateBankMock, deleteBankMock } = vi.hoisted(() => ({
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  createBankMock: vi.fn<FinancialApiClient['createBank']>(),
  updateBankMock: vi.fn<FinancialApiClient['updateBank']>(),
  deleteBankMock: vi.fn<FinancialApiClient['deleteBank']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getBanks: getBanksMock,
    createBank: createBankMock,
    updateBank: updateBankMock,
    deleteBank: deleteBankMock,
  } as Partial<FinancialApiClient>,
}))

describe('useBanks', () => {
  beforeEach(() => {
    getBanksMock.mockReset()
    getBanksMock.mockResolvedValue(BANKS)
  })

  it('fetches the bank list once on mount', async () => {
    const { result } = renderHook(() => useBanks())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getBanksMock).toHaveBeenCalledTimes(1)
    expect(result.current.banks).toEqual(BANKS)
  })
})
```

`vi.hoisted` is what lets the mocks be referenced inside the hoisted `vi.mock` factory. Typing
each mock as `vi.fn<FinancialApiClient['getBanks']>()` makes a renamed client method a type
error in the test too. Re-read `result.current` after every `waitFor`; do not destructure it
once at render time.

## When to skip

- A hook that is a one-line `useAsyncResource` call with no mapping — one success + one error
  test; the reducer is covered in `useAsyncResource` itself.
- Testing `useAsyncResource`'s reducer through every consumer.

## Examples from project

- `Financial.Web/src/hooks/__tests__/useBanks.test.ts` — Unit; CRUD + list refresh.
- `Financial.Web/src/hooks/__tests__/usePaymentsDue.test.ts` — Unit; fake timers, no polling, auto-dismiss.
- `Financial.Web/src/hooks/__tests__/useAggregatedSummary.test.ts` — Unit; `createSelectedNodeWrapper` for node-type branching.
- `Financial.Web/src/hooks/__tests__/useTransferForm.test.ts` + `mapTransferErrorToField.test.ts` — Unit; validation and backend-error mapping.
- `Financial.Web/src/hooks/__tests__/useColumnFilters.test.ts`, `useSortableRows.test.ts` — Unit; grid state (P37).
