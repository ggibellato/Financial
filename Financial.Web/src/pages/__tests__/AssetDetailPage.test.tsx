import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AssetDetailPage from '../AssetDetailPage'

const getAssetDetailsMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getAssetDetails: getAssetDetailsMock,
  }),
}))

describe('AssetDetailPage', () => {
  beforeEach(() => {
    getAssetDetailsMock.mockReset()
  })

  it('renders asset details', async () => {
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
      totalCredits: 11,
      operations: [],
      credits: [],
    })

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'BCIA11' })).toBeInTheDocument()
    expect(screen.getByText(/Ticker/)).toBeInTheDocument()
  })

  it('shows error state when request fails', async () => {
    getAssetDetailsMock.mockRejectedValue(new Error('Boom'))

    render(
      <MemoryRouter initialEntries={['/assets/XPI/Default/BCIA11']}>
        <Routes>
          <Route path="/assets/:brokerName/:portfolioName/:assetName" element={<AssetDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
  })
})
