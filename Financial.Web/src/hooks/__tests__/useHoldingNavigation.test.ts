import { act, renderHook } from '@testing-library/react'
import { MemoryRouter, useLocation } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { EMPTY_TREE, treeWithAsset } from '../../test/navigationTree'
import { useHoldingNavigation } from '../useHoldingNavigation'

const { getNavigationTreeMock } = vi.hoisted(() => ({
  getNavigationTreeMock: vi.fn<FinancialApiClient['getNavigationTree']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getNavigationTree: getNavigationTreeMock,
  } as Partial<FinancialApiClient>,
}))

function renderNavigation() {
  return renderHook(() => ({ navigation: useHoldingNavigation(), location: useLocation() }), { wrapper: MemoryRouter })
}

describe('useHoldingNavigation', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
  })

  it('navigates_to_the_resolved_route_carrying_the_pending_selection', async () => {
    getNavigationTreeMock.mockResolvedValue(treeWithAsset('Trading212', 'ISA', 'VUSA'))
    const { result } = renderNavigation()

    let navigated = false
    await act(async () => {
      navigated = await result.current.navigation.navigateToHolding('Trading212', 'ISA', 'VUSA')
    })

    expect(navigated).toBe(true)
    expect(result.current.location.pathname).toBe('/investments/active-investments')
    expect(result.current.location.state).toStrictEqual({
      pendingSelection: { brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' },
    })
  })

  it('carries_the_corporate_action_id_in_the_router_state_when_given', async () => {
    getNavigationTreeMock.mockResolvedValue(treeWithAsset('Trading212', 'ISA', 'VUSA'))
    const { result } = renderNavigation()

    await act(async () => {
      await result.current.navigation.navigateToHolding('Trading212', 'ISA', 'VUSA', 'ca-123')
    })

    expect(result.current.location.state).toStrictEqual({
      pendingSelection: { brokerName: 'Trading212', portfolioName: 'ISA', assetName: 'VUSA' },
      pendingCorporateActionId: 'ca-123',
    })
  })

  it('reports_failure_without_navigating_when_the_holding_cannot_be_resolved', async () => {
    getNavigationTreeMock.mockResolvedValue(EMPTY_TREE)
    const { result } = renderNavigation()

    let navigated = true
    await act(async () => {
      navigated = await result.current.navigation.navigateToHolding('XPI', 'Acoes', 'KLBN4')
    })

    expect(navigated).toBe(false)
    expect(result.current.location.pathname).toBe('/')
  })

  it('reports_failure_instead_of_throwing_when_the_tree_lookup_fails', async () => {
    getNavigationTreeMock.mockRejectedValue(new Error('Service unavailable'))
    const { result } = renderNavigation()

    let navigated = true
    await act(async () => {
      navigated = await result.current.navigation.navigateToHolding('XPI', 'Acoes', 'KLBN4')
    })

    expect(navigated).toBe(false)
    expect(result.current.location.pathname).toBe('/')
  })
})
