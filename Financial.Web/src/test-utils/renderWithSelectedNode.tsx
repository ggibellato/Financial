import type { ReactElement } from 'react'
import type { InvestmentScope, SelectedNode } from '../api/types'
import { render } from '../test/renderWithFluent'
import { createSelectedNodeWrapper } from './selectedNodeTestWrapper'

export function renderWithSelectedNode(ui: ReactElement, node: SelectedNode, scope: InvestmentScope = 'active') {
  const { wrapper, setNode } = createSelectedNodeWrapper(scope)
  const result = render(ui, { wrapper })
  setNode(node)
  return result
}
