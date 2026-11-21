import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TitheMonthDto, TitheSummaryDto } from '../../api/types'
import { CARRY_FORWARD_UPDATE_ERROR, NO_CARRY_FORWARD_MESSAGE, useTithe } from '../useTithe'
import { deferred, never } from '../../test-utils/deferred'

const { getTitheSummaryByMonthMock, getTitheYearMock, updateTitheCarryForwardMock } = vi.hoisted(() => ({
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
  getTitheYearMock: vi.fn<FinancialApiClient['getTitheYear']>(),
  updateTitheCarryForwardMock: vi.fn<FinancialApiClient['updateTitheCarryForward']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
    getTitheYear: getTitheYearMock,
    updateTitheCarryForward: updateTitheCarryForwardMock,
  } as Partial<FinancialApiClient>,
}))

const AC_02 = '[AC P61-F03-tithe-tab-and-history-02]'

const INCLUDED: TitheSummaryDto = {
  incomeBase: 4227.3,
  calculatedTithe: 422.73,
  tithePaid: 400,
  titheBalance: 142.73,
  carryForward: { amount: 120, included: true, fromYear: 2026, fromMonth: 9 },
}

const EXCLUDED: TitheSummaryDto = { ...INCLUDED, titheBalance: 22.73, carryForward: { amount: 120, included: false, fromYear: 2026, fromMonth: 9 } }

function year(summary: TitheSummaryDto): TitheMonthDto[] {
  return Array.from({ length: 12 }, (_, index) => ({ year: 2026, month: index + 1, summary }))
}

async function renderLoaded() {
  const rendered = renderHook(() => useTithe(2026, 10))
  await waitFor(() => expect(rendered.result.current.detail.isLoading || rendered.result.current.history.isLoading).toBe(false))
  return rendered
}

describe('useTithe', () => {
  beforeEach(() => {
    getTitheSummaryByMonthMock.mockReset().mockResolvedValue(INCLUDED)
    getTitheYearMock.mockReset().mockResolvedValue(year(INCLUDED))
    updateTitheCarryForwardMock.mockReset()
  })

  it('loads the month detail and the year history once each', async () => {
    const { result } = await renderLoaded()

    expect(getTitheSummaryByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 10)
    expect(getTitheYearMock).toHaveBeenCalledExactlyOnceWith(2026)
    expect(result.current.detail.data).toEqual(INCLUDED)
    expect(result.current.history.data).toHaveLength(12)
  })

  it('shows the detail and the history loading from the first render', () => {
    const seen: boolean[][] = []

    renderHook(() => {
      const tithe = useTithe(2026, 10)
      seen.push([tithe.detail.isLoading, tithe.history.isLoading])
    })

    expect(seen[0]).toEqual([true, true])
  })

  it('loads the history again for another year', async () => {
    const { rerender } = renderHook(({ y }) => useTithe(y, 10), { initialProps: { y: 2026 } })
    await waitFor(() => expect(getTitheYearMock).toHaveBeenCalledWith(2026))

    rerender({ y: 2025 })

    await waitFor(() => expect(getTitheYearMock).toHaveBeenCalledWith(2025))
  })

  it('keeps the history when the detail fails to load', async () => {
    getTitheSummaryByMonthMock.mockRejectedValue(new Error('boom'))

    const { result } = await renderLoaded()

    await waitFor(() => expect(result.current.detail.error).toBe('boom'))
    expect(result.current.history.data).toHaveLength(12)
  })

  it(`${AC_02} updates the carry-forward, marks it busy until done, then refreshes the detail and the history`, async () => {
    const pendingUpdate = deferred<TitheSummaryDto>()
    updateTitheCarryForwardMock.mockReturnValue(pendingUpdate.promise)
    const { result } = await renderLoaded()
    getTitheSummaryByMonthMock.mockResolvedValue(EXCLUDED)
    getTitheYearMock.mockResolvedValue(year(EXCLUDED))

    act(() => result.current.updateCarryForwardInclusion(false))
    expect(result.current.carryForwardUpdating).toBe(true)

    await act(async () => pendingUpdate.resolve(EXCLUDED))

    await waitFor(() => expect(result.current.carryForwardUpdating).toBe(false))
    expect(updateTitheCarryForwardMock).toHaveBeenCalledWith(2026, 10, { included: false })
    expect(result.current.detail.data).toEqual(EXCLUDED)
    expect(result.current.history.data?.[0].summary).toEqual(EXCLUDED)
    expect(getTitheYearMock).toHaveBeenCalledTimes(2)
    expect(result.current.carryForwardError).toBeNull()
  })

  it(`${AC_02} explains a refused update when there is no balance to carry, and refreshes`, async () => {
    updateTitheCarryForwardMock.mockRejectedValue(new ApiError('No carry-forward is available for 2026-10.', 400))
    const { result } = await renderLoaded()

    act(() => result.current.updateCarryForwardInclusion(false))

    await waitFor(() => expect(result.current.carryForwardError).toBe(NO_CARRY_FORWARD_MESSAGE))
    await waitFor(() => expect(result.current.carryForwardUpdating).toBe(false))
    expect(getTitheSummaryByMonthMock).toHaveBeenCalledTimes(2)
    expect(getTitheYearMock).toHaveBeenCalledTimes(2)
  })

  it(`${AC_02} shows a retry message for any other failure, keeps the decision and clears the message on the next attempt`, async () => {
    updateTitheCarryForwardMock.mockRejectedValueOnce(new ApiError('Server error', 500))
    updateTitheCarryForwardMock.mockReturnValueOnce(never())
    const { result } = await renderLoaded()

    act(() => result.current.updateCarryForwardInclusion(false))

    await waitFor(() => expect(result.current.carryForwardError).toBe(CARRY_FORWARD_UPDATE_ERROR))
    expect(result.current.carryForwardUpdating).toBe(false)
    expect(result.current.detail.data?.carryForward?.included).toBe(true)
    expect(getTitheSummaryByMonthMock).toHaveBeenCalledOnce()

    act(() => result.current.updateCarryForwardInclusion(false))

    expect(result.current.carryForwardError).toBeNull()
  })

  it(`${AC_02} drops an update error once its month is left, so returning to it shows none`, async () => {
    updateTitheCarryForwardMock.mockRejectedValue(new ApiError('Server error', 500))
    const { result, rerender } = renderHook(({ month }) => useTithe(2026, month), { initialProps: { month: 10 } })
    await waitFor(() => expect(result.current.detail.isLoading).toBe(false))

    act(() => result.current.updateCarryForwardInclusion(false))
    await waitFor(() => expect(result.current.carryForwardError).toBe(CARRY_FORWARD_UPDATE_ERROR))

    rerender({ month: 11 })

    expect(result.current.carryForwardError).toBeNull()
    await waitFor(() => expect(getTitheSummaryByMonthMock).toHaveBeenLastCalledWith(2026, 11))
    expect(getTitheYearMock).toHaveBeenCalledOnce()

    rerender({ month: 10 })

    expect(result.current.carryForwardError).toBeNull()
  })

  it(`${AC_02} keeps a later month busy when an earlier month's update finishes first`, async () => {
    const pendingOctober = deferred<TitheSummaryDto>()
    updateTitheCarryForwardMock
      .mockReturnValueOnce(pendingOctober.promise)
      .mockReturnValueOnce(never())
    const { result, rerender } = renderHook(({ month }) => useTithe(2026, month), { initialProps: { month: 10 } })
    await waitFor(() => expect(result.current.detail.isLoading).toBe(false))

    act(() => result.current.updateCarryForwardInclusion(false))
    rerender({ month: 11 })
    act(() => result.current.updateCarryForwardInclusion(true))
    expect(result.current.carryForwardUpdating).toBe(true)

    await act(async () => pendingOctober.resolve(EXCLUDED))

    expect(result.current.carryForwardUpdating).toBe(true)
  })
})
