import { act } from '@testing-library/react'
import type { ReactNode } from 'react'
import type { InvestmentScope, SelectedNode } from '../api/types'
import { SelectedNodeProvider, useSelectedNode } from '../context/SelectedNodeContext'

/**
 * Shared test wrapper for hooks/components that read `useSelectedNode()`.
 * Renders a `SelectedNodeProvider` and exposes a `setNode` helper that
 * pushes a node into context (wrapped in `act`) from outside the tree.
 * Defaults to Active scope; pass `'historic'` to test Historic-scope behavior.
 */
export function createSelectedNodeWrapper(scope: InvestmentScope = 'active') {
  let setNodeRef: ((node: SelectedNode | null) => void) | undefined

  function NodeControl() {
    const { setSelectedNode } = useSelectedNode()
    setNodeRef = setSelectedNode
    return null
  }

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <SelectedNodeProvider scope={scope}>
        <NodeControl />
        {children}
      </SelectedNodeProvider>
    )
  }

  return {
    wrapper: Wrapper,
    setNode: (node: SelectedNode | null) => act(() => { setNodeRef?.(node) }),
  }
}
