import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import HistoricInvestmentsPage from '../HistoricInvestmentsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TreeNodeDto } from '../../api/types'

const getNavigationTreeMock = vi.fn()
const getAssetDetailsMock = vi.fn()
const getPortfolioAssetsSummaryMock = vi.fn()
const getSummaryByBrokerMock = vi.fn()
const getSummaryByPortfolioMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getNavigationTree: getNavigationTreeMock,
    getAssetDetails: getAssetDetailsMock,
    getPortfolioAssetsSummary: getPortfolioAssetsSummaryMock,
    getSummaryByBroker: getSummaryByBrokerMock,
    getSummaryByPortfolio: getSummaryByPortfolioMock,
  }),
}))

const stubTree: TreeNodeDto = {
  nodeType: 'Investments',
  displayName: 'Investments',
  metadata: {},
  children: [
    {
      nodeType: 'Broker',
      displayName: 'XPI (BRL)',
      metadata: { BrokerName: 'XPI', Currency: 'BRL' },
      children: [
        {
          nodeType: 'Portfolio',
          displayName: 'Uncategorized (1 assets)',
          metadata: { PortfolioName: 'Uncategorized', AssetCount: 1 },
          children: [
            {
              nodeType: 'Asset',
              displayName: 'CLOSEDASSET',
              metadata: {
                AssetName: 'CLOSEDASSET',
                Ticker: 'CLOSEDASSET',
                Exchange: 'BVMF',
                PositionType: 'Flat',
                GlobalAssetClass: 1,
              },
              children: [],
            },
          ],
        },
      ],
    },
  ],
}

function renderPage() {
  return render(
    <MemoryRouter>
      <HistoricInvestmentsPage />
    </MemoryRouter>,
  )
}

describe('HistoricInvestmentsPage', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getPortfolioAssetsSummaryMock.mockReturnValue(new Promise(() => {}))
    getSummaryByBrokerMock.mockReturnValue(new Promise(() => {}))
    getSummaryByPortfolioMock.mockReturnValue(new Promise(() => {}))
  })

  it('renders tree and empty detail state on load', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderPage()
    expect(await screen.findByText('XPI (BRL)')).toBeInTheDocument()
    expect(screen.getByText('Select an item to view details')).toBeInTheDocument()
  })

  it('requests the navigation tree with historic scope', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderPage()
    await screen.findByText('XPI (BRL)')
    expect(getNavigationTreeMock).toHaveBeenCalledWith('historic')
  })

  it('selecting an asset node resolves it with historic scope', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderPage()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getAllByLabelText('Expand')[0])
    fireEvent.click(screen.getByRole('button', { name: '● CLOSEDASSET' }))
    expect(screen.getByText('CLOSEDASSET', { selector: '.detail-panel__name' })).toBeInTheDocument()
    expect(getAssetDetailsMock).toHaveBeenCalledWith('XPI', 'Uncategorized', 'CLOSEDASSET', 'historic')
  })

  it('selecting a broker node shows broker name in right panel', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderPage()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getByText('XPI (BRL)'))
    expect(screen.getAllByText('XPI').length).toBeGreaterThan(0)
    expect(screen.getByRole('button', { name: 'Summary' })).toBeInTheDocument()
  })
})
