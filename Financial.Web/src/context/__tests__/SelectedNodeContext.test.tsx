import { render, screen, act } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { SelectedNodeProvider, useSelectedNode } from '../SelectedNodeContext'
import type { SelectedNode } from '../../api/types'

function NodeTypeDisplay() {
  const { selectedNode } = useSelectedNode()
  return <div data-testid="node-type">{selectedNode?.nodeType ?? 'null'}</div>
}

function NodeSetter({ node }: { node: SelectedNode }) {
  const { setSelectedNode } = useSelectedNode()
  return <button onClick={() => setSelectedNode(node)}>set</button>
}

describe('SelectedNodeContext', () => {
  it('useSelectedNode returns null by default', () => {
    render(
      <SelectedNodeProvider>
        <NodeTypeDisplay />
      </SelectedNodeProvider>,
    )
    expect(screen.getByTestId('node-type').textContent).toBe('null')
  })

  it('setSelectedNode updates context value', () => {
    const brokerNode: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' }
    render(
      <SelectedNodeProvider>
        <NodeSetter node={brokerNode} />
        <NodeTypeDisplay />
      </SelectedNodeProvider>,
    )
    act(() => {
      screen.getByText('set').click()
    })
    expect(screen.getByTestId('node-type').textContent).toBe('Broker')
  })

  it('useSelectedNode throws when called outside provider', () => {
    const original = console.error
    console.error = () => {}
    expect(() => render(<NodeTypeDisplay />)).toThrow(
      'useSelectedNode must be used within a SelectedNodeProvider',
    )
    console.error = original
  })
})
