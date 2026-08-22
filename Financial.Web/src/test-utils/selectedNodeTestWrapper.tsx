import { act } from '@testing-library/react'
import type { ReactNode } from 'react'
import type { InvestmentScope, SelectedNode } from '../api/types'
import { SelectedNodeProvider, useSelectedNode } from '../context/SelectedNodeContext'

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
