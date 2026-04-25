import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import AssetDetailPage from './pages/AssetDetailPage'

const getNavigationTreeMock = vi.fn()
const getAssetDetailsMock = vi.fn()

vi.mock('./api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getNavigationTree: getNavigationTreeMock,
    getAssetDetails: getAssetDetailsMock,
  }),
}))

describe('App', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
    getAssetDetailsMock.mockReset()
    getNavigationTreeMock.mockResolvedValue({
      nodeType: 'Investments',
      displayName: 'All Investments',
      metadata: {},
      children: [],
    })
  })

  it('renders the app header', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<App />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: 'Portfolio Dashboard' })).toBeInTheDocument()
  })

  it('navigates from tree to asset detail', async () => {
    getNavigationTreeMock.mockResolvedValue({
      nodeType: 'Investments',
      displayName: 'All Investments',
      metadata: {},
      children: [
        {
          nodeType: 'Broker',
          displayName: 'XPI (BRL)',
          metadata: { BrokerName: 'XPI', Currency: 'BRL', PortfolioCount: 1, TotalAssets: 1 },
          children: [
            {
              nodeType: 'Portfolio',
              displayName: 'Default (1 assets)',
              metadata: { PortfolioName: 'Default', AssetCount: 1, ActiveAssetCount: 1 },
              children: [
                {
                  nodeType: 'Asset',
                  displayName: 'BCIA11',
                  metadata: {
                    AssetName: 'BCIA11',
                    Ticker: 'BCIA11',
                    Exchange: 'BVMF',
                    ISIN: 'TEST',
                    Country: 0,
                    LocalTypeCode: '',
                    GlobalAssetClass: 0,
                    Quantity: 10,
                    AveragePrice: 100,
                    IsActive: true,
                    OperationCount: 0,
                    CreditCount: 0,
                  },
                  children: [],
                },
              ],
            },
          ],
        },
      ],
    })
    getAssetDetailsMock.mockResolvedValue({
      name: 'BCIA11',
      brokerName: 'XPI',
      portfolioName: 'Default',
      ticker: 'BCIA11',
      isin: 'TEST',
      exchange: 'BVMF',
      country: 0,
      localTypeCode: '',
      class: 0,
      quantity: 10,
      averagePrice: 100,
      isActive: true,
      totalBought: 1000,
      totalSold: 0,
      totalCredits: 0,
      operations: [],
      credits: [],
    })

    render(
      <MemoryRouter initialEntries={['/brokers']}>
        <Routes>
          <Route path="/" element={<App />}>
            <Route path="brokers" element={<div>Broker list</div>} />
            <Route path="assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    const assetLink = await screen.findByRole('link', { name: /BCIA11/ })
    fireEvent.click(assetLink)

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()
  })
})
