import { act } from '@testing-library/react'
import { createElement } from 'react'
import type { ReactNode } from 'react'
import type { SelectedNode } from '../api/types'
import { SelectedNodeProvider, useSelectedNode } from '../context/SelectedNodeContext'

/**
 * Shared test wrapper for hooks/components that read `useSelectedNode()`.
 * Renders a `SelectedNodeProvider` and exposes a `setNode` helper that
 * pushes a node into context (wrapped in `act`) from outside the tree.
 */
export function createSelectedNodeWrapper() {
  let setNodeRef: ((node: SelectedNode | null) => void) | undefined

  function NodeControl() {
    const { setSelectedNode } = useSelectedNode()
    setNodeRef = setSelectedNode
    return null
  }

  function Wrapper({ children }: { children: ReactNode }) {
    return createElement(SelectedNodeProvider, null, createElement(NodeControl), children)
  }

  return {
    wrapper: Wrapper,
    setNode: (node: SelectedNode | null) => act(() => { setNodeRef?.(node) }),
  }
}
