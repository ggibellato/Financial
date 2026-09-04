import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import InvestmentTree from '../InvestmentTree'
import { SelectedNodeProvider, useSelectedNode } from '../../context/SelectedNodeContext'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PositionType, TreeNodeDto } from '../../api/types'

const { getNavigationTreeMock } = vi.hoisted(() => ({
  getNavigationTreeMock: vi.fn(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getNavigationTree: getNavigationTreeMock,
  } as Partial<FinancialApiClient>,
}))

function makeAsset(
  name: string,
  isActive: boolean,
  assetClass: number,
  positionType: PositionType = isActive ? 'Long' : 'Flat',
  // null omits the key entirely; undefined would just trigger this default.
  quantity: number | null = isActive ? 8 : 0,
): TreeNodeDto {
  return {
    nodeType: 'Asset',
    displayName: name,
    metadata: {
      AssetName: name,
      Ticker: name,
      Exchange: 'BVMF',
      PositionType: positionType,
      GlobalAssetClass: assetClass,
      ...(quantity === null ? {} : { Quantity: quantity }),
    },
    children: [],
  }
}

function makePortfolio(name: string, assets: TreeNodeDto[], carryCount = true): TreeNodeDto {
  return {
    nodeType: 'Portfolio',
    displayName: `${name} (${assets.length} assets)`,
    metadata: carryCount
      ? { PortfolioName: name, AssetCount: assets.length }
      : { PortfolioName: name },
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
    <>
      <div data-testid="selected">
        {selectedNode.nodeType}:{selectedNode.brokerName}:{selectedNode.portfolioName ?? ''}:{selectedNode.assetName ?? ''}
      </div>
      <div data-testid="selected-quantity">{selectedNode.quantity ?? 'absent'}</div>
      <div data-testid="selected-asset-count">{selectedNode.assetCount ?? 'absent'}</div>
    </>
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

  it('renders with the tree and treeitem accessibility roles', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(screen.getByRole('tree', { name: 'Investments' })).toBeInTheDocument()
    const brokerItem = screen.getByText('XPI (BRL)').closest('[role="treeitem"]') as HTMLElement
    expect(brokerItem).toBeInTheDocument()
    expect(brokerItem).toHaveAttribute('aria-expanded', 'true')
  })

  it('requests the tree with active scope by default', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(getNavigationTreeMock).toHaveBeenCalledWith('active')
  })

  it('requests the tree with the scope from context', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    render(
      <SelectedNodeProvider scope="historic">
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    await screen.findByText('XPI (BRL)')
    expect(getNavigationTreeMock).toHaveBeenCalledWith('historic')
  })

  it('renders portfolio nodes under broker', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
  })

  it('gives the status dot an accessible name naming the position type', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))
    expect(screen.getByRole('treeitem', { name: 'Long KLBN4' })).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: 'Flat TRPL4' })).toBeInTheDocument()
  })

  it('renders Long/Flat/Short status icons with the matching color class', async () => {
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [
        makeBroker('XPI', 'BRL', [
          makePortfolio('Mix', [
            makeAsset('LONGASSET', true, 1, 'Long'),
            makeAsset('FLATASSET', false, 1, 'Flat'),
            makeAsset('SHORTASSET', true, 1, 'Short'),
          ]),
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
    fireEvent.click(screen.getByText('Mix (3 assets)'))

    expect(
      screen.getByRole('treeitem', { name: 'Long LONGASSET' }).querySelector('.investment-tree__status-icon'),
    ).toHaveClass('investment-tree__status-icon--long')
    expect(
      screen.getByRole('treeitem', { name: 'Flat FLATASSET' }).querySelector('.investment-tree__status-icon'),
    ).toHaveClass('investment-tree__status-icon--flat')
    expect(
      screen.getByRole('treeitem', { name: 'Short SHORTASSET' }).querySelector('.investment-tree__status-icon'),
    ).toHaveClass('investment-tree__status-icon--short')
  })

  it('clicking asset node sets selectedNode in context', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))
    fireEvent.click(screen.getByText('KLBN4'))
    expect(screen.getByTestId('selected').textContent).toBe('Asset:XPI:Acoes:KLBN4')
  })

  /** A DataTransfer stand-in; jsdom does not provide one. */
  function dataTransfer() {
    return { setData: vi.fn(), getData: vi.fn(), effectAllowed: '', dropEffect: '' }
  }

  /**
   * fireEvent returns false when the handler called preventDefault. For a drop target that is the
   * whole signal: preventDefault means "I will take this", its absence means the drop is refused.
   */
  const accepts = (row: HTMLElement) => !fireEvent.dragOver(row, { dataTransfer: dataTransfer() })

  /** The TreeItemLayout element carries the drop handlers; the label sits inside it. */
  const rowOf = (label: string) => screen.getByText(label).closest('.investment-tree__node') as HTMLElement

  const twoBrokerTree: TreeNodeDto = {
    nodeType: 'Investments',
    displayName: 'Investments',
    metadata: {},
    children: [
      makeBroker('XPI', 'BRL', [
        makePortfolio('Acoes', [makeAsset('KLBN4', true, 1)]),
        makePortfolio('FII', []),
      ]),
      makeBroker('Coinbase', 'GBP', [makePortfolio('Default', [])]),
    ],
  }

  async function startDraggingKLBN4() {
    renderTree(twoBrokerTree)
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (1 assets)'))
    const asset = screen.getByRole('treeitem', { name: 'Long KLBN4' })
    fireEvent.dragStart(asset, { dataTransfer: dataTransfer() })
  }

  it('a sibling portfolio of the same broker accepts the drop', async () => {
    await startDraggingKLBN4()

    expect(accepts(rowOf('FII (0 assets)'))).toBe(true)
  })

  it('the broker accepts the drop, because that means a new portfolio', async () => {
    await startDraggingKLBN4()

    expect(accepts(rowOf('XPI (BRL)'))).toBe(true)
  })

  it('the portfolio the asset is already in refuses the drop', async () => {
    await startDraggingKLBN4()

    expect(accepts(rowOf('Acoes (1 assets)'))).toBe(false)
  })

  it('another broker and its portfolios refuse the drop', async () => {
    await startDraggingKLBN4()

    expect(accepts(rowOf('Coinbase (GBP)'))).toBe(false)
    expect(accepts(rowOf('Default (0 assets)'))).toBe(false)
  })

  it('nothing accepts a drop when no drag is in progress', async () => {
    renderTree(twoBrokerTree)
    await screen.findByText('XPI (BRL)')

    expect(accepts(rowOf('FII (0 assets)'))).toBe(false)
  })

  it('highlights only the row the drag is over, and clears it on leave', async () => {
    await startDraggingKLBN4()

    accepts(rowOf('XPI (BRL)'))
    expect(rowOf('XPI (BRL)')).toHaveClass('investment-tree__row--drop-target')

    fireEvent.dragLeave(rowOf('XPI (BRL)'))
    expect(rowOf('XPI (BRL)')).not.toHaveClass('investment-tree__row--drop-target')
  })

  it('dropping on a portfolio moves straight there, without asking anything', async () => {
    await startDraggingKLBN4()

    fireEvent.drop(rowOf('FII (0 assets)'), { dataTransfer: dataTransfer() })

    // The dialog opens already resolved: there is nothing left to ask.
    await waitFor(() => expect(screen.getByText('Moving…')).toBeInTheDocument())
  })

  it('dropping on the broker asks for a name for the new portfolio', async () => {
    await startDraggingKLBN4()

    fireEvent.drop(rowOf('XPI (BRL)'), { dataTransfer: dataTransfer() })

    await waitFor(() => expect(screen.getByRole('textbox', { name: 'New portfolio name' })).toBeEnabled())
    expect(screen.queryByText('Moving…')).not.toBeInTheDocument()
  })

  it('dropping on a refusing target does nothing at all', async () => {
    await startDraggingKLBN4()

    fireEvent.drop(rowOf('Coinbase (GBP)'), { dataTransfer: dataTransfer() })

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('clicking an asset carries its quantity onto the selection', async () => {
    // The detail panel decides from this whether archiving is offered, and it only ever gets it by
    // a click - so the click is what the test has to make.
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))

    fireEvent.click(screen.getByText('KLBN4'))

    expect(screen.getByTestId('selected-quantity').textContent).toBe('8')
  })

  it('carries a zero quantity as zero, not as absent', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))

    fireEvent.click(screen.getByText('TRPL4'))

    expect(screen.getByTestId('selected-quantity').textContent).toBe('0')
  })

  it('reports a missing quantity as -1 so it never reads as a closed position', async () => {
    // getMetaNumber's sentinel. A default of 0 here would offer archiving for every open position.
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [
        makeBroker('XPI', 'BRL', [makePortfolio('Acoes', [makeAsset('NOQTY', true, 1, 'Long', null)])]),
      ],
    }
    renderTree(tree)
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (1 assets)'))

    fireEvent.click(screen.getByText('NOQTY'))

    expect(screen.getByTestId('selected-quantity').textContent).toBe('-1')
  })

  it('clicking broker node sets selectedNode in context', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(screen.getByTestId('selected').textContent).toBe('Broker:XPI::')
  })

  it('clicking a portfolio carries its asset count onto the selection', async () => {
    // The detail panel decides from this whether the portfolio can be deleted.
    renderTree()
    await screen.findByText('XPI (BRL)')

    fireEvent.click(screen.getByText('Acoes (2 assets)'))

    expect(screen.getByTestId('selected-asset-count').textContent).toBe('2')
  })

  it('reports a missing asset count as -1 so an unknown portfolio is never offered for deletion', async () => {
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [makeBroker('XPI', 'BRL', [makePortfolio('Acoes', [], false)])],
    }
    renderTree(tree)
    await screen.findByText('XPI (BRL)')

    fireEvent.click(screen.getByText('Acoes (0 assets)'))

    expect(screen.getByTestId('selected-asset-count').textContent).toBe('-1')
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
    fireEvent.click(screen.getByText('Mix (2 assets)'))

    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '1' } })
    expect(screen.getByRole('treeitem', { name: 'Long KLBN4' })).toBeInTheDocument()
    expect(screen.queryByRole('treeitem', { name: 'Long TREA3' })).not.toBeInTheDocument()
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
    fireEvent.click(screen.getByText('Mix (2 assets)'))

    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '1' } })
    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: 'all' } })
    expect(screen.getByRole('treeitem', { name: 'Long KLBN4' })).toBeInTheDocument()
    expect(screen.getByRole('treeitem', { name: 'Long TREA3' })).toBeInTheDocument()
  })

  it('asset class filter shows Cryptocurrency option', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    const option = screen.getByRole('option', { name: 'Cryptocurrency' }) as HTMLOptionElement
    expect(option.value).toBe('9')
  })

  it('asset class filter hides non-matching assets when Cryptocurrency selected', async () => {
    const tree: TreeNodeDto = {
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [
        makeBroker('Coinbase', 'GBP', [
          makePortfolio('Cryptocurrency', [makeAsset('BTC', true, 9), makeAsset('TREA3', true, 3)]),
        ]),
      ],
    }
    getNavigationTreeMock.mockResolvedValue(tree)
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    await screen.findByText('Coinbase (GBP)')
    fireEvent.click(screen.getByText('Cryptocurrency (2 assets)'))

    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '9' } })
    expect(screen.getByRole('treeitem', { name: 'Long BTC' })).toBeInTheDocument()
    expect(screen.queryByRole('treeitem', { name: 'Long TREA3' })).not.toBeInTheDocument()
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

  it('clicking a broker node collapses it', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(screen.queryByText('Acoes (2 assets)')).not.toBeInTheDocument()
  })

  it('clicking a broker node again expands it', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('XPI (BRL)'))
    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(screen.getByText('Acoes (2 assets)')).toBeInTheDocument()
  })

  it('does not use selectionMode="single" (verified structurally: no radio role rendered)', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
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
