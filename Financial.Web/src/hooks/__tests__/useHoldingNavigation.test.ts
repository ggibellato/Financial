import { renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useHoldingNavigation } from '../useHoldingNavigation'

const { navigateMock, resolveHoldingLocationMock } = vi.hoisted(() => ({
  navigateMock: vi.fn(),
  resolveHoldingLocationMock: vi.fn(),
}))

vi.mock('react-router-dom', () => ({
  useNavigate: () => navigateMock,
}))

vi.mock('../../utils/holdingNavigation', () => ({
  resolveHoldingLocation: resolveHoldingLocationMock,
}))

describe('useHoldingNavigation', () => {
  beforeEach(() => {
    navigateMock.mockReset()
    resolveHoldingLocationMock.mockReset()
  })

  it('navigates_to_the_resolved_route_carrying_the_pending_selection', async () => {
    resolveHoldingLocationMock.mockResolvedValue({ scope: 'active', route: '/investments/active-investments' })

    const { result } = renderHook(() => useHoldingNavigation())
    const navigated = await result.current.navigateToHolding('Trading212', 'ISA', 'VUSA')

    expect(navigated).toBe(true)
    expect(resolveHoldingLocationMock).toHaveBeenCalledWith('Trading212', 'ISA', 'VUSA')
    expect(navigateMock).toHaveBeenCalledWith('/investments/active-investments', {
      state: { pendingSelection: { brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' } },
    })
  })

  it('includes_pendingCorporateActionId_in_router_state_when_provided', async () => {
    resolveHoldingLocationMock.mockResolvedValue({ scope: 'active', route: '/investments/active-investments' })

    const { result } = renderHook(() => useHoldingNavigation())
    await result.current.navigateToHolding('Trading212', 'ISA', 'VUSA', 'ca-123')

    expect(navigateMock).toHaveBeenCalledWith('/investments/active-investments', {
      state: {
        pendingSelection: { brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' },
        pendingCorporateActionId: 'ca-123',
      },
    })
  })

  it('omits_pendingCorporateActionId_entirely_when_not_provided', async () => {
    resolveHoldingLocationMock.mockResolvedValue({ scope: 'active', route: '/investments/active-investments' })

    const { result } = renderHook(() => useHoldingNavigation())
    await result.current.navigateToHolding('Trading212', 'ISA', 'VUSA')

    const stateArg = navigateMock.mock.calls[0][1].state
    expect('pendingCorporateActionId' in stateArg).toBe(false)
  })

  it('navigates_to_the_historic_route_when_that_is_what_resolved', async () => {
    resolveHoldingLocationMock.mockResolvedValue({ scope: 'historic', route: '/investments/historic-investments' })

    const { result } = renderHook(() => useHoldingNavigation())
    await result.current.navigateToHolding('XPI', 'Acoes', 'KLBN4')

    expect(navigateMock).toHaveBeenCalledWith('/investments/historic-investments', expect.anything())
  })

  it('reports_failure_without_navigating_when_the_holding_cannot_be_resolved', async () => {
    resolveHoldingLocationMock.mockResolvedValue(null)

    const { result } = renderHook(() => useHoldingNavigation())
    const navigated = await result.current.navigateToHolding('XPI', 'Acoes', 'KLBN4')

    expect(navigated).toBe(false)
    expect(navigateMock).not.toHaveBeenCalled()
  })

  it('reports_failure_instead_of_throwing_when_the_tree_lookup_fails', async () => {
    resolveHoldingLocationMock.mockRejectedValue(new Error('Service unavailable'))

    const { result } = renderHook(() => useHoldingNavigation())

    await expect(result.current.navigateToHolding('XPI', 'Acoes', 'KLBN4')).resolves.toBe(false)
    expect(navigateMock).not.toHaveBeenCalled()
  })
})
