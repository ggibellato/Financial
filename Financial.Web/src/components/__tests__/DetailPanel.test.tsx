import { act, fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DetailPanel from '../DetailPanel'
import { SelectedNodeProvider, useSelectedNode } from '../../context/SelectedNodeContext'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { SelectedNode } from '../../api/types'

const getAssetDetailsMock = vi.fn()
const getCurrentPriceMock = vi.fn()
const getSummaryByBrokerMock = vi.fn()
const getSummaryByPortfolioMock = vi.fn()
const getPortfolioAssetsSummaryMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getAssetDetails: getAssetDetailsMock,
    getCurrentPrice: getCurrentPriceMock,
    getSummaryByBroker: getSummaryByBrokerMock,
    getSummaryByPortfolio: getSummaryByPortfolioMock,
    getPortfolioAssetsSummary: getPortfolioAssetsSummaryMock,
  }),
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
}
const activeAssetNode: SelectedNode = {
  nodeType: 'Asset',
  brokerName: 'XPI',
  portfolioName: 'Acoes',
  assetName: 'KLBN4',
  ticker: 'KLBN4',
  exchange: 'BVMF',
  isActive: true,
}
const inactiveAssetNode: SelectedNode = { ...activeAssetNode, isActive: false }

describe('DetailPanel', () => {
  beforeEach(() => {
    vi.stubGlobal('navigator', {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    })
    vi.stubGlobal('alert', vi.fn())
    vi.stubGlobal('confirm', vi.fn())
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    getSummaryByBrokerMock.mockReturnValue(new Promise(() => {}))
    getSummaryByPortfolioMock.mockReturnValue(new Promise(() => {}))
    getPortfolioAssetsSummaryMock.mockReturnValue(new Promise(() => {}))
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
    expect(screen.queryByText(/Active/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Inactive/)).not.toBeInTheDocument()
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

  it('shows Active status indicator for active asset', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('● Active')).toBeInTheDocument()
  })

  it('shows Inactive status indicator for inactive asset', () => {
    renderPanel(inactiveAssetNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('○ Inactive')).toBeInTheDocument()
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

  it('renders_asset_summary_tab_for_asset_node_regression', () => {
    renderPanel(activeAssetNode)
    act(() => screen.getByTestId('setter').click())
    expect(screen.getByText('Loading...')).toBeInTheDocument()
    expect(screen.queryByText('Transactions are only available for individual assets')).not.toBeInTheDocument()
  })

  it('renders_transactions_message_for_broker_node', () => {
    renderPanel(brokerNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByRole('button', { name: 'Transactions' }))
    expect(screen.getByText('Transactions are only available for individual assets')).toBeInTheDocument()
  })

  it('renders_transactions_message_for_portfolio_node', () => {
    renderPanel(portfolioNode)
    act(() => screen.getByTestId('setter').click())
    fireEvent.click(screen.getByRole('button', { name: 'Transactions' }))
    expect(screen.getByText('Transactions are only available for individual assets')).toBeInTheDocument()
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
})
