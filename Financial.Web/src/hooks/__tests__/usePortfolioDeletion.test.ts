import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { useSelectedNode } from '../../context/SelectedNodeContext'
import { createSelectedNodeWrapper } from '../../test-utils/selectedNodeTestWrapper'
import { usePortfolioDeletion } from '../usePortfolioDeletion'

const { deleteEmptyPortfolioMock } = vi.hoisted(() => ({
  deleteEmptyPortfolioMock: vi.fn<FinancialApiClient['deleteEmptyPortfolio']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { deleteEmptyPortfolio: deleteEmptyPortfolioMock } as Partial<FinancialApiClient>,
}))

function renderDeletion() {
  const { wrapper, setNode } = createSelectedNodeWrapper()
  const rendered = renderHook(() => ({ deletion: usePortfolioDeletion(), selected: useSelectedNode() }), { wrapper })
  return { ...rendered, setNode }
}

describe('usePortfolioDeletion', () => {
  beforeEach(() => {
    deleteEmptyPortfolioMock.mockReset()
  })

  it('shows the delete failure message when the request is rejected', async () => {
    deleteEmptyPortfolioMock.mockRejectedValue('unexpected')
    const { result } = renderDeletion()

    await act(async () => result.current.deletion.deletePortfolio('XPI', 'Default'))

    expect(result.current.deletion.deleteError).toBe('The portfolio could not be deleted. Try again.')
  })

  it('clears the previous error when deleting again', async () => {
    deleteEmptyPortfolioMock.mockRejectedValueOnce('unexpected').mockReturnValueOnce(new Promise(() => {}))
    const { result } = renderDeletion()
    await act(async () => result.current.deletion.deletePortfolio('XPI', 'Default'))

    act(() => result.current.deletion.deletePortfolio('XPI', 'Default'))

    expect(result.current.deletion.deleteError).toBeNull()
  })

  it('clears the selection and reloads once the portfolio is deleted', async () => {
    deleteEmptyPortfolioMock.mockResolvedValue(undefined as never)
    const { result, setNode } = renderDeletion()
    setNode({ nodeType: 'Portfolio', brokerName: 'XPI', portfolioName: 'Default' })
    const tokenBefore = result.current.selected.reloadToken

    await act(async () => result.current.deletion.deletePortfolio('XPI', 'Default'))

    expect(result.current.selected.selectedNode).toBeNull()
    expect(result.current.selected.reloadToken).not.toBe(tokenBefore)
  })
})
