import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import InvestmentTree from '../InvestmentTree'
import { SelectedNodeProvider, useSelectedNode } from '../../context/SelectedNodeContext'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TreeNodeDto } from '../../api/types'

const getNavigationTreeMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getNavigationTree: getNavigationTreeMock,
  }),
}))

function makeAsset(name: string, isActive: boolean, assetClass: number): TreeNodeDto {
  return {
    nodeType: 'Asset',
    displayName: name,
    metadata: {
      AssetName: name,
      Ticker: name,
      Exchange: 'BVMF',
      IsActive: isActive,
      GlobalAssetClass: assetClass,
    },
    children: [],
  }
}

function makePortfolio(name: string, assets: TreeNodeDto[]): TreeNodeDto {
  return {
    nodeType: 'Portfolio',
    displayName: `${name} (${assets.length} assets)`,
    metadata: { PortfolioName: name, AssetCount: assets.length },
    children: assets,
  }
}

function makeBroker(name: string, currency: string, portfolios: TreeNodeDto[]): TreeNodeDto {
  return {
    nodeType: 'Broker',
    displayName: `${name} (${currency})`,
    metadata: { BrokerName: name, Currency: currency },
    children: portfolios,
  }
}

const stubTree: TreeNodeDto = {
  nodeType: 'Investments',
  displayName: 'Investments',
  metadata: {},
  children: [
    makeBroker('XPI', 'BRL', [
      makePortfolio('Acoes', [
        makeAsset('KLBN4', true, 1),
        makeAsset('TRPL4', false, 1),
      ]),
    ]),
  ],
}

function SelectedNodeDisplay() {
  const { selectedNode } = useSelectedNode()
  if (!selectedNode) return <div data-testid="selected">none</div>
  return (
    <div data-testid="selected">
      {selectedNode.nodeType}:{selectedNode.brokerName}:{selectedNode.portfolioName ?? ''}:{selectedNode.assetName ?? ''}
    </div>
  )
}

function renderTree(tree: TreeNodeDto = stubTree) {
  getNavigationTreeMock.mockResolvedValue(tree)
  return render(
    <SelectedNodeProvider>
      <InvestmentTree />
      <SelectedNodeDisplay />
    </SelectedNodeProvider>,
  )
}

describe('InvestmentTree', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
  })

  it('shows loading state on mount', () => {
    getNavigationTreeMock.mockReturnValue(new Promise(() => {}))
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    expect(screen.getByText('Loading investments...')).toBeInTheDocument()
  })

  it('renders broker nodes after successful load', async () => {
    renderTree()
    expect(await screen.findByText('XPI (BRL)')).toBeInTheDocument()
  })

  it('renders portfolio nodes under broker', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
  })

  it('renders active asset with filled circle prefix', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    const expandBtn = screen.getAllByLabelText('Expand')[0]
    fireEvent.click(expandBtn)
    expect(screen.getByRole('button', { name: '● KLBN4' })).toBeInTheDocument()
  })

  it('renders inactive asset with empty circle prefix', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    const expandBtn = screen.getAllByLabelText('Expand')[0]
    fireEvent.click(expandBtn)
    expect(screen.getByRole('button', { name: '○ TRPL4' })).toBeInTheDocument()
  })

  it('renders active asset status icon in green and inactive in red', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    const expandBtn = screen.getAllByLabelText('Expand')[0]
    fireEvent.click(expandBtn)
    expect(screen.getByText('●')).toHaveClass('investment-tree__status-icon--active')
    expect(screen.getByText('○')).toHaveClass('investment-tree__status-icon--inactive')
  })

  it('clicking asset node sets selectedNode in context', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    const expandBtn = screen.getAllByLabelText('Expand')[0]
    fireEvent.click(expandBtn)
    fireEvent.click(screen.getByRole('button', { name: '● KLBN4' }))
    expect(screen.getByTestId('selected').textContent).toBe('Asset:XPI:Acoes:KLBN4')
  })

  it('clicking broker node sets selectedNode in context', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(screen.getByTestId('selected').textContent).toBe('Broker:XPI::')
  })

  it('clicking portfolio node sets selectedNode in context', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))
    expect(screen.getByTestId('selected').textContent).toBe('Portfolio:XPI:Acoes:')
  })

  it('asset class filter hides non-matching assets', async () => {
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [
        makeBroker('XPI', 'BRL', [
          makePortfolio('Mix', [makeAsset('KLBN4', true, 1), makeAsset('TREA3', true, 3)]),
        ]),
      ],
    }
    getNavigationTreeMock.mockResolvedValue(tree)
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    await screen.findByText('XPI (BRL)')
    const expandBtn = screen.getAllByLabelText('Expand')[0]
    fireEvent.click(expandBtn)

    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '1' } })
    expect(screen.getByRole('button', { name: '● KLBN4' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: '● TREA3' })).not.toBeInTheDocument()
  })

  it('asset class filter All restores full tree', async () => {
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [
        makeBroker('XPI', 'BRL', [
          makePortfolio('Mix', [makeAsset('KLBN4', true, 1), makeAsset('TREA3', true, 3)]),
        ]),
      ],
    }
    getNavigationTreeMock.mockResolvedValue(tree)
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    await screen.findByText('XPI (BRL)')
    const expandBtn = screen.getAllByLabelText('Expand')[0]
    fireEvent.click(expandBtn)

    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: 'all' } })
    expect(screen.getByRole('button', { name: '● KLBN4' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '● TREA3' })).toBeInTheDocument()
  })

  it('broker node is retained in tree when filter is active', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '1' } })
    expect(screen.getByText('XPI (BRL)')).toBeInTheDocument()
  })

  it('broker node hidden when filter removes all its assets', async () => {
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [
        makeBroker('XPI', 'BRL', [makePortfolio('Bonds', [makeAsset('TREA3', true, 3)])]),
      ],
    }
    getNavigationTreeMock.mockResolvedValue(tree)
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    await screen.findByText('XPI (BRL)')
    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '1' } })
    expect(screen.queryByText('XPI (BRL)')).not.toBeInTheDocument()
  })

  it('broker nodes are expanded by default on load', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
  })

  it('clicking broker chevron collapses broker', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
    fireEvent.click(screen.getAllByLabelText('Collapse')[0])
    expect(screen.queryByText('Acoes (2 assets)')).not.toBeInTheDocument()
  })

  it('clicking broker chevron again expands broker', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getAllByLabelText('Collapse')[0])
    fireEvent.click(screen.getAllByLabelText('Expand')[0])
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
  })

  it('shows error state on fetch failure', async () => {
    getNavigationTreeMock.mockRejectedValue(new Error('Network error'))
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    expect(await screen.findByRole('alert')).toHaveTextContent('Network error')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('retry button re-fetches tree', async () => {
    getNavigationTreeMock
      .mockRejectedValueOnce(new Error('Fail'))
      .mockResolvedValueOnce(stubTree)
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    await screen.findByRole('alert')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
    expect(screen.getByText('XPI (BRL)')).toBeInTheDocument()
  })
})
