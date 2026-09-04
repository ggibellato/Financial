import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DetailPanel from '../DetailPanel'
import { SelectedNodeProvider, useSelectedNode } from '../../context/SelectedNodeContext'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { SelectedNode } from '../../api/types'
import { ApiError } from '../../api/apiError'

const {
  getAssetDetailsMock,
  getCurrentPriceMock,
  getSummaryByBrokerMock,
  getSummaryByPortfolioMock,
  getPortfolioAssetsSummaryMock,
  getTransactionsByBrokerMock,
  getTransactionsByPortfolioMock,
  deleteEmptyPortfolioMock,
} = vi.hoisted(() => ({
  getAssetDetailsMock: vi.fn(),
  getCurrentPriceMock: vi.fn(),
  getSummaryByBrokerMock: vi.fn(),
  getSummaryByPortfolioMock: vi.fn(),
  getPortfolioAssetsSummaryMock: vi.fn(),
  getTransactionsByBrokerMock: vi.fn().mockResolvedValue([]),
  getTransactionsByPortfolioMock: vi.fn().mockResolvedValue([]),
  deleteEmptyPortfolioMock: vi.fn(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAssetDetails: getAssetDetailsMock,
    getCurrentPrice: getCurrentPriceMock,
    getSummaryByBroker: getSummaryByBrokerMock,
    getSummaryByPortfolio: getSummaryByPortfolioMock,
    getPortfolioAssetsSummary: getPortfolioAssetsSummaryMock,
    getTransactionsByBroker: getTransactionsByBrokerMock,
    getTransactionsByPortfolio: getTransactionsByPortfolioMock,
    deleteEmptyPortfolio: deleteEmptyPortfolioMock,
  } as Partial<FinancialApiClient>,
}))

function NodeSetter({ node }: { node: SelectedNode | null }) {
  const { setSelectedNode } = useSelectedNode()
  return (
    <button data-testid="setter" onClick={() => setSelectedNode(node)}>
      set
    </button>
  )
}

function renderPanel(initial: SelectedNode | null = null) {
  return render(
    <SelectedNodeProvider>
      <NodeSetter node={initial} />
      <DetailPanel />
    </SelectedNodeProvider>,
  )
}

const brokerNode: SelectedNode = { nodeType: 'Broker', brokerName: 'XPI', currency: 'BRL' }
const portfolioNode: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetCount: 2,
}

const emptyPortfolioNode: SelectedNode = {
  nodeType: 'Portfolio',
  brokerName: 'XPI',
  portfolioName: 'Stale',
  assetCount: 0,
}
const activeAssetNode: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  positionType: 'Long',
}
const flatAssetNode: SelectedNode = { ...activeAssetNode, positionType: 'Flat' }
const shortAssetNode: SelectedNode = { ...activeAssetNode, positionType: 'Short' }

describe('DetailPanel', () => {
  beforeEach(() => {
    vi.stubGlobal('navigator', {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    })
    vi.stubGlobal('alert', vi.fn())
    vi.stubGlobal('confirm', vi.fn())
    getAssetDetailsMock.mockReset()
    getCurrentPriceMock.mockReset()
    getSummaryByBrokerMock.mockReset()
    getSummaryByPortfolioMock.mockReset()
    getPortfolioAssetsSummaryMock.mockReset()
    getTransactionsByBrokerMock.mockReset()
    getTransactionsByPortfolioMock.mockReset()
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    getSummaryByBrokerMock.mockReturnValue(new Promise(() => {}))
    getSummaryByPortfolioMock.mockReturnValue(new Promise(() => {}))
    getPortfolioAssetsSummaryMock.mockReturnValue(new Promise(() => {}))
    getTransactionsByBrokerMock.mockResolvedValue([])
    getTransactionsByPortfolioMock.mockResolvedValue([])
  })

  it('shows empty state when selectedNode is null', () => {
    renderPanel(null)
    expect(screen.getByText('Select an item to view details')).toBeInTheDocument()
  })

  it('shows broker name in header for broker node', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('XPI')).toBeInTheDocument()
  })

  it('does not show status indicator for broker node', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.queryByText(/Long/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Flat/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Short/)).not.toBeInTheDocument()
  })

  it('does not show copy icon for broker node', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.queryByLabelText('Copy name')).not.toBeInTheDocument()
  })

  it('shows portfolio name and broker breadcrumb', () => {
    renderPanel(portfolioNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('Acoes')).toBeInTheDocument()
    expect(screen.getByText('XPI')).toBeInTheDocument()
  })

  it('shows asset name with full breadcrumb', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('KLBN4')).toBeInTheDocument()
    expect(screen.getByText('KLBN4 · BVMF · XPI · Acoes')).toBeInTheDocument()
  })

  it('shows Long status indicator in green for a long position', () => {
    const { container } = renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    const status = container.querySelector('.detail-panel__status')
    expect(status).toHaveTextContent('Long')
    expect(status).toHaveClass('detail-panel__status--long')
  })

  it('shows Flat status indicator in the neutral color for a flat position', () => {
    const { container } = renderPanel(flatAssetNode)
    act(() => screen.getByTestId('setter').click())
    const status = container.querySelector('.detail-panel__status')
    expect(status).toHaveTextContent('Flat')
    expect(status).toHaveClass('detail-panel__status--flat')
  })

  it('shows Short status indicator in red for a short position', () => {
    const { container } = renderPanel(shortAssetNode)
    act(() => screen.getByTestId('setter').click())
    const status = container.querySelector('.detail-panel__status')
    expect(status).toHaveTextContent('Short')
    expect(status).toHaveClass('detail-panel__status--short')
  })

  it('copy icon calls clipboard writeText with asset name', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByLabelText('Copy name'))
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('KLBN4')
  })

  it('copy icon does not show confirmation dialog', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByLabelText('Copy name'))
    expect(window.alert).not.toHaveBeenCalled()
    expect(window.confirm).not.toHaveBeenCalled()
  })

  it('tab bar renders three tabs', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByRole('button', { name: 'Summary' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Transactions' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Credits' })).toBeInTheDocument()
  })

  it('does not show Price History tab for a broker node', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.queryByRole('button', { name: 'Price History' })).not.toBeInTheDocument()
  })

  it('does not show Price History tab for a portfolio node', () => {
    renderPanel(portfolioNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.queryByRole('button', { name: 'Price History' })).not.toBeInTheDocument()
  })

  it('shows Price History tab for an asset node', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByRole('button', { name: 'Price History' })).toBeInTheDocument()
  })

  it('Summary tab is active by default', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByRole('button', { name: 'Summary' })).toHaveClass('detail-panel__tab--active')
    expect(screen.getByRole('button', { name: 'Transactions' })).not.toHaveClass('detail-panel__tab--active')
  })

  it('clicking Transactions tab activates it', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByRole('button', { name: 'Transactions' }))
    expect(screen.getByRole('button', { name: 'Transactions' })).toHaveClass('detail-panel__tab--active')
    expect(screen.getByRole('button', { name: 'Summary' })).not.toHaveClass('detail-panel__tab--active')
  })

  it('renders_asset_summary_tab_when_asset_selected', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_aggregated_summary_tab_for_broker_node', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders_portfolio_summary_tab_for_portfolio_node', () => {
    renderPanel(portfolioNode)
    act(() => screen.getByTestId('setter').click())
    const loadingItems = screen.getAllByText('Loading...')
    expect(loadingItems.length).toBeGreaterThanOrEqual(1)
  })

  it('renders_transactions_chart_for_broker_node', async () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByRole('button', { name: 'Transactions' }))
    await waitFor(
      () => expect(screen.getByText('Net Invested by Month')).toBeInTheDocument(),
      { timeout: 5000 },
    )
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('renders_transactions_chart_for_portfolio_node', async () => {
    renderPanel(portfolioNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByRole('button', { name: 'Transactions' }))
    await waitFor(
      () => expect(screen.getByText('Net Invested by Month')).toBeInTheDocument(),
      { timeout: 5000 },
    )
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('active tab resets to Summary on selectedNode change', () => {
    function MultiSetter() {
      const { setSelectedNode } = useSelectedNode()
      return (
        <>
          <button data-testid="set-broker" onClick={() => setSelectedNode(brokerNode)}>
            broker
          </button>
          <button data-testid="set-portfolio" onClick={() => setSelectedNode(portfolioNode)}>
            portfolio
          </button>
        </>
      )
    }
    render(
      <SelectedNodeProvider>
        <MultiSetter />
        <DetailPanel />
      </SelectedNodeProvider>,
    )
    fireEvent.click(screen.getByTestId('set-broker'))
    fireEvent.click(screen.getByRole('button', { name: 'Transactions' }))
    expect(screen.getByRole('button', { name: 'Transactions' })).toHaveClass('detail-panel__tab--active')

    fireEvent.click(screen.getByTestId('set-portfolio'))
    expect(screen.getByRole('button', { name: 'Summary' })).toHaveClass('detail-panel__tab--active')
    expect(screen.getByRole('button', { name: 'Transactions' })).not.toHaveClass('detail-panel__tab--active')
  })

  it('offers to delete a portfolio that holds nothing', async () => {
    renderPanel(emptyPortfolioNode)
    fireEvent.click(screen.getByTestId('setter'))

    expect(screen.getByRole('button', { name: 'Delete Portfolio' })).toBeInTheDocument()
  })

  it('does not offer to delete a portfolio that still holds assets', async () => {
    renderPanel(portfolioNode)
    fireEvent.click(screen.getByTestId('setter'))

    expect(screen.queryByRole('button', { name: 'Delete Portfolio' })).not.toBeInTheDocument()
  })

  it('does not offer to delete when the asset count is unknown', async () => {
    // -1 is what the tree reports when the metadata is missing; it must not read as empty.
    renderPanel({ ...emptyPortfolioNode, assetCount: -1 })
    fireEvent.click(screen.getByTestId('setter'))

    expect(screen.queryByRole('button', { name: 'Delete Portfolio' })).not.toBeInTheDocument()
  })

  it('deletes the portfolio and clears the selection', async () => {
    deleteEmptyPortfolioMock.mockResolvedValue(undefined)
    renderPanel(emptyPortfolioNode)
    fireEvent.click(screen.getByTestId('setter'))

    fireEvent.click(screen.getByRole('button', { name: 'Delete Portfolio' }))

    await waitFor(() => expect(deleteEmptyPortfolioMock).toHaveBeenCalledWith('XPI', 'Stale', 'active'))
    // Nothing here describes anything once the portfolio is gone.
    await waitFor(() => expect(screen.getByText('Select an item to view details')).toBeInTheDocument())
  })

  it("shows the server's reason when a deletion is refused", async () => {
    deleteEmptyPortfolioMock.mockRejectedValue(new ApiError('Portfolio "Stale" still holds 1 asset(s).', 409))
    renderPanel(emptyPortfolioNode)
    fireEvent.click(screen.getByTestId('setter'))

    fireEvent.click(screen.getByRole('button', { name: 'Delete Portfolio' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('still holds'))
  })
})
