import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import PortfolioNavigatorPage from '../PortfolioNavigatorPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TreeNodeDto } from '../../api/types'

const getNavigationTreeMock = vi.fn()
const getAssetDetailsMock = vi.fn()
const getCurrentPriceMock = vi.fn()
const getSummaryByBrokerMock = vi.fn()
const getSummaryByPortfolioMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getNavigationTree: getNavigationTreeMock,
    getAssetDetails: getAssetDetailsMock,
    getCurrentPrice: getCurrentPriceMock,
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
          displayName: 'Acoes (1 assets)',
          metadata: { PortfolioName: 'Acoes', AssetCount: 1 },
          children: [
            {
              nodeType: 'Asset',
              displayName: 'KLBN4',
              metadata: {
                AssetName: 'KLBN4',
                Ticker: 'KLBN4',
                Exchange: 'BVMF',
                IsActive: true,
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
      <PortfolioNavigatorPage />
    </MemoryRouter>,
  )
}

describe('PortfolioNavigatorPage', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    getAssetDetailsMock.mockReturnValue(new Promise(() => {}))
    getCurrentPriceMock.mockReturnValue(new Promise(() => {}))
    getSummaryByBrokerMock.mockReturnValue(new Promise(() => {}))
    getSummaryByPortfolioMock.mockReturnValue(new Promise(() => {}))
  })

  it('renders tree and empty detail state on load', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderPage()
    expect(await screen.findByText('XPI (BRL)')).toBeInTheDocument()
    expect(screen.getByText('Select an item to view details')).toBeInTheDocument()
  })

  it('selecting an asset node shows asset name in right panel', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)
    renderPage()
    await screen.findByText('XPI (BRL)')
    fireEvent.click(screen.getAllByLabelText('Expand')[0])
    fireEvent.click(screen.getByRole('button', { name: '● KLBN4' }))
    expect(screen.getByText('KLBN4', { selector: '.detail-panel__name' })).toBeInTheDocument()
    expect(screen.getByText('KLBN4 · BVMF · XPI · Acoes')).toBeInTheDocument()
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
