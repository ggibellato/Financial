import type { ReactElement } from 'react'
import { act, fireEvent, render as rtlRender, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, useLocation, useNavigationType } from 'react-router-dom'
import { deferred, never } from '../../test-utils/deferred'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import InvestmentTree from '../InvestmentTree'
import { SelectedNodeProvider, useSelectedNode } from '../../context/SelectedNodeContext'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PositionType, TreeNodeDto } from '../../api/types'

const { getNavigationTreeMock, moveAssetMock } = vi.hoisted(() => ({
  getNavigationTreeMock: vi.fn(),
  moveAssetMock: vi.fn(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getNavigationTree: getNavigationTreeMock,
    moveAsset: moveAssetMock,
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

// The tree reads router state to apply a pending click-through selection, so every render needs a
// router around it.
function render(ui: ReactElement) {
  return rtlRender(<MemoryRouter>{ui}</MemoryRouter>)
}

function RouterStateDisplay() {
  const { state } = useLocation()
  return (
    <>
      <div data-testid="router-state">{state === null || state === undefined ? 'none' : JSON.stringify(state)}</div>
      <div data-testid="navigation-type">{useNavigationType()}</div>
    </>
  )
}

function SelectedNodeDisplay() {
  const { selectedNode } = useSelectedNode()
  if (!selectedNode) return <div data-testid="selected">none</div>
  return (
    <>
      <div data-testid="selected">
        {selectedNode.nodeType}:{selectedNode.brokerName}:{selectedNode.portfolioName ?? ''}:{selectedNode.assetName ?? ''}
      </div>
      <div data-testid="selected-asset-class">{selectedNode.assetClass ?? 'absent'}</div>
      <div data-testid="selected-asset-count">{selectedNode.assetCount ?? 'absent'}</div>
      <div data-testid="selected-pending-corporate-action-id">{selectedNode.pendingCorporateActionId ?? 'absent'}</div>
      <div data-testid="selected-currency">{selectedNode.currency ?? 'absent'}</div>
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

function renderWithPendingSelection(
  pendingSelection: { brokerName: string; portfolioName: string; assetName: string },
  pendingCorporateActionId?: string,
) {
  return rtlRender(
    <MemoryRouter
      initialEntries={[
        {
          pathname: '/investments/active-investments',
          state: { pendingSelection, ...(pendingCorporateActionId ? { pendingCorporateActionId } : {}) },
        },
      ]}
    >
      <SelectedNodeProvider>
        <InvestmentTree />
        <SelectedNodeDisplay />
        <RouterStateDisplay />
      </SelectedNodeProvider>
    </MemoryRouter>,
  )
}

describe('InvestmentTree', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    moveAssetMock.mockReset()
    Element.prototype.scrollIntoView = vi.fn()
  })

  it('shows loading state on mount', () => {
    getNavigationTreeMock.mockReturnValue(never())
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    expect(screen.getByText('Loading investments…')).toBeInTheDocument()
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
    const transfer = dataTransfer()
    fireEvent.dragStart(asset, { dataTransfer: transfer })
    return { asset, transfer }
  }

  async function dropOnBroker() {
    fireEvent.drop(rowOf('XPI (BRL)'), { dataTransfer: dataTransfer() })
    await waitFor(() => expect(screen.getByRole('textbox', { name: 'New portfolio name' })).toBeEnabled())
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

  it.each(['XPI (BRL)', 'FII (0 assets)'])('highlights %s only while the drag is over it', async (label) => {
    await startDraggingKLBN4()
    expect(rowOf(label)).not.toHaveClass('investment-tree__row--drop-target')

    accepts(rowOf(label))
    expect(rowOf(label)).toHaveClass('investment-tree__row--drop-target')

    fireEvent.dragLeave(rowOf(label))
    expect(rowOf(label)).not.toHaveClass('investment-tree__row--drop-target')
  })

  it('dropping on a portfolio moves straight there, without asking anything', async () => {
    await startDraggingKLBN4()

    fireEvent.drop(rowOf('FII (0 assets)'), { dataTransfer: dataTransfer() })

    // The dialog opens already resolved: there is nothing left to ask.
    await waitFor(() => expect(screen.getByText('Moving…')).toBeInTheDocument())
  })

  it('dropping on the broker asks for a name for the new portfolio, offering no existing one', async () => {
    await startDraggingKLBN4()

    await dropOnBroker()

    expect(screen.queryByText('Moving…')).not.toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'FII' })).not.toBeInTheDocument()
  })

  it.each(['FII (0 assets)', 'XPI (BRL)'])('takes a drop on %s and clears its highlight', async (label) => {
    await startDraggingKLBN4()
    accepts(rowOf(label))

    const notTaken = fireEvent.drop(rowOf(label), { dataTransfer: dataTransfer() })

    expect(notTaken).toBe(false)
    expect(rowOf(label)).not.toHaveClass('investment-tree__row--drop-target')
  })

  it('puts the asset name on the drag, and ends the drag when it is let go', async () => {
    const { asset, transfer } = await startDraggingKLBN4()

    fireEvent.dragEnd(asset)

    expect(transfer.setData).toHaveBeenCalledWith(expect.any(String), 'KLBN4')
    expect(accepts(rowOf('FII (0 assets)'))).toBe(false)
  })

  it('closes the move dialog on Cancel', async () => {
    await startDraggingKLBN4()
    await dropOnBroker()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    await waitFor(() => expect(screen.queryByText('Move asset')).not.toBeInTheDocument())
  })

  it('does not offer to archive a closed asset dropped on its broker', async () => {
    await startDraggingKLBN4()
    getNavigationTreeMock.mockResolvedValue({
      ...twoBrokerTree,
      children: [makeBroker('XPI', 'BRL', [makePortfolio('Acoes', [makeAsset('KLBN4', false, 1)])])],
    })

    await dropOnBroker()

    expect(screen.queryByText('Archive to historic investments')).not.toBeInTheDocument()
  })

  it('selects the moved asset in its new portfolio and reloads the tree once a drop has moved it', async () => {
    moveAssetMock.mockResolvedValue({ brokerName: 'XPI', portfolioName: 'FII', assetName: 'KLBN4' })
    await startDraggingKLBN4()
    getNavigationTreeMock.mockResolvedValue({
      ...twoBrokerTree,
      children: [
        makeBroker('XPI', 'BRL', [
          makePortfolio('Acoes', [makeAsset('VALE3', true, 1)]),
          makePortfolio('FII', [makeAsset('KLBN4', true, 1)]),
        ]),
      ],
    })

    fireEvent.drop(rowOf('FII (0 assets)'), { dataTransfer: dataTransfer() })

    await waitFor(() => expect(screen.getByTestId('selected').textContent).toBe('Asset:XPI:FII:KLBN4'))
    expect(screen.queryByText('Moving…')).not.toBeInTheDocument()
    expect(await screen.findByText('FII (1 assets)')).toBeInTheDocument()
  })

  it('dropping on a refusing target does nothing at all', async () => {
    await startDraggingKLBN4()

    fireEvent.drop(rowOf('Coinbase (GBP)'), { dataTransfer: dataTransfer() })

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  const repeatedNamesTree: TreeNodeDto = {
    nodeType: 'Investments',
    displayName: 'Investments',
    metadata: {},
    children: [
      makeBroker('XPI', 'BRL', [
        makePortfolio('Acoes', [makeAsset('KLBN4', true, 1), makeAsset('VALE3', true, 1)]),
        makePortfolio('Extra', [makeAsset('KLBN4', true, 1)]),
      ]),
      makeBroker('Rico', 'BRL', [makePortfolio('Acoes', [makeAsset('KLBN4', true, 1)])]),
    ],
  }

  const treeItemOf = (element: HTMLElement) => element.closest('[role="treeitem"]')
  const selectedItems = () => screen.queryAllByRole('treeitem').filter((item) => item.getAttribute('aria-selected') === 'true')

  it('marks only the portfolio or broker that is selected, even where names repeat', async () => {
    renderTree(repeatedNamesTree)
    await screen.findByText('XPI (BRL)')
    expect(selectedItems()).toEqual([])

    fireEvent.click(screen.getByText('Acoes (2 assets)'))
    expect(selectedItems()).toEqual([treeItemOf(screen.getByText('Acoes (2 assets)'))])

    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(selectedItems()).toEqual([treeItemOf(screen.getByText('XPI (BRL)'))])
  })

  it('marks only the selected asset, not another of its portfolio or one of the same name elsewhere', async () => {
    renderTree(repeatedNamesTree)
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Acoes (2 assets)'))
    fireEvent.click(screen.getByText('Extra (1 assets)'))

    fireEvent.click(screen.getAllByText('KLBN4')[0])

    expect(selectedItems()).toEqual([treeItemOf(screen.getAllByText('KLBN4')[0])])
  })

  it('clicking broker node sets selectedNode in context', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(screen.getByTestId('selected').textContent).toBe('Broker:XPI::')
    expect(screen.getByTestId('selected-currency').textContent).toBe('BRL')
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

  it('asset class filter shows Private credit option', async () => {
    renderTree()
    await screen.findByText('XPI (BRL)')
    const option = screen.getByRole('option', { name: 'Private credit' }) as HTMLOptionElement
    expect(option.value).toBe('10')
  })

  it.each([
    [2, 'RealEstate'],
    [10, 'PrivateCredit'],
  ])('selecting an asset of class %i passes the server class name %s', async (assetClass, className) => {
    renderTree({
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [makeBroker('XPI', 'BRL', [makePortfolio('Mix', [makeAsset('HGLG11', true, assetClass)])])],
    })
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('Mix (1 assets)'))
    fireEvent.click(screen.getByText('HGLG11'))
    expect(screen.getByTestId('selected-asset-class').textContent).toBe(className)
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

  it('shows a broker with no portfolios, and an empty portfolio, as leaves while every class is shown', async () => {
    renderTree({
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [makeBroker('XPI', 'BRL', [makePortfolio('Empty', [])]), makeBroker('Coinbase', 'GBP', [])],
    })
    await screen.findByText('XPI (BRL)')

    expect(screen.getByText('Coinbase (GBP)').closest('[role="treeitem"]')).not.toHaveAttribute('aria-expanded')
    expect(screen.getByText('Empty (0 assets)').closest('[role="treeitem"]')).not.toHaveAttribute('aria-expanded')
  })

  it('asset class filter hides a portfolio with nothing of that class, under a broker that keeps another', async () => {
    renderTree({
      nodeType: 'Investments',
      displayName: 'Investments',
      metadata: {},
      children: [makeBroker('XPI', 'BRL', [makePortfolio('Acoes', [makeAsset('KLBN4', true, 1)]), makePortfolio('Crypto', [makeAsset('BTC', true, 9)])])],
    })
    await screen.findByText('XPI (BRL)')

    fireEvent.change(screen.getByLabelText('Asset class'), { target: { value: '9' } })

    expect(screen.getByText('Crypto (1 assets)')).toBeInTheDocument()
    expect(screen.queryByText('Acoes (1 assets)')).not.toBeInTheDocument()
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

  it('shows loading again on retry and drops the error, then the tree', async () => {
    const retried = deferred<TreeNodeDto>()
    getNavigationTreeMock.mockRejectedValueOnce(new Error('Network down')).mockReturnValueOnce(retried.promise)
    render(
      <SelectedNodeProvider>
        <InvestmentTree />
      </SelectedNodeProvider>,
    )
    fireEvent.click(await screen.findByRole('button', { name: 'Try again' }))

    expect(screen.getByText('Loading investments…')).toBeInTheDocument()

    retried.resolve(stubTree)
    expect(await screen.findByText('XPI (BRL)')).toBeInTheDocument()
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

  it('a pendingSelection in router state selects the matching node once the tree loads', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderWithPendingSelection({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' })

    await waitFor(() => expect(screen.getByTestId('selected')).toHaveTextContent('Asset:XPI:Acoes:KLBN4'))
    const portfolioItem = screen.getByText('Acoes (2 assets)').closest('[role="treeitem"]') as HTMLElement
    expect(portfolioItem).toHaveAttribute('aria-expanded', 'true')
  })

  it('a pendingCorporateActionId alongside pendingSelection carries through to the resolved SelectedNode', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderWithPendingSelection({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' }, 'ca-123')

    await waitFor(() =>
      expect(screen.getByTestId('selected-pending-corporate-action-id')).toHaveTextContent('ca-123'),
    )
  })

  it('moves focus to the selected treeitem, not just scrolling it into view', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderWithPendingSelection({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' })

    await waitFor(() => expect(screen.getByTestId('selected')).toHaveTextContent('Asset:XPI:Acoes:KLBN4'))
    await waitFor(() => expect(screen.getByText('KLBN4').closest('[role="treeitem"]')).toHaveFocus())
  })

  it('leaves the history alone when there is no pendingSelection', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    rtlRender(
      <MemoryRouter initialEntries={['/investments/active-investments']}>
        <SelectedNodeProvider>
          <InvestmentTree />
          <RouterStateDisplay />
        </SelectedNodeProvider>
      </MemoryRouter>,
    )

    await screen.findByText('XPI (BRL)')
    await act(async () => {})
    expect(screen.getByTestId('navigation-type').textContent).toBe('POP')
  })

  it('a pendingSelection for a node not present in the loaded tree is a no-op', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderWithPendingSelection({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'MISSING' })

    await screen.findByText('XPI (BRL)')
    expect(screen.getByTestId('selected')).toHaveTextContent('none')
  })

  it('router state is cleared after the pending selection is applied, replacing its history entry', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderWithPendingSelection({ brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' })

    await waitFor(() => expect(screen.getByTestId('selected')).toHaveTextContent('Asset:XPI:Acoes:KLBN4'))
    await waitFor(() => expect(screen.getByTestId('router-state')).toHaveTextContent('none'))
    expect(screen.getByTestId('navigation-type').textContent).toBe('REPLACE')
    expect(Element.prototype.scrollIntoView).toHaveBeenCalledWith({ block: 'nearest' })
  })

  it('does not apply a consumed pending selection again when the tree reloads', async () => {
    function ReloadButton() {
      const { reload } = useSelectedNode()
      return <button onClick={reload}>Reload tree</button>
    }
    getNavigationTreeMock.mockResolvedValue(stubTree)
    rtlRender(
      <MemoryRouter
        initialEntries={[
          { pathname: '/investments/active-investments', state: { pendingSelection: { brokerName: 'XPI', portfolioName: 'Acoes', assetName: 'KLBN4' } } },
        ]}
      >
        <SelectedNodeProvider>
          <InvestmentTree />
          <SelectedNodeDisplay />
          <RouterStateDisplay />
          <ReloadButton />
        </SelectedNodeProvider>
      </MemoryRouter>,
    )
    await waitFor(() => expect(screen.getByTestId('router-state')).toHaveTextContent('none'))
    fireEvent.click(screen.getByText('XPI (BRL)'))

    fireEvent.click(screen.getByRole('button', { name: 'Reload tree' }))

    await waitFor(() => expect(getNavigationTreeMock).toHaveBeenCalledTimes(2))
    await screen.findByText('XPI (BRL)')
    expect(screen.getByTestId('selected').textContent).toBe('Broker:XPI::')
  })
})
