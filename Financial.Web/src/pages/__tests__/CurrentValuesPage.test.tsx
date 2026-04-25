import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CurrentValuesPage from '../CurrentValuesPage'

const getBrokersMock = vi.fn()
const getCurrentPriceMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getBrokers: getBrokersMock,
    getCurrentPrice: getCurrentPriceMock,
  }),
}))

describe('CurrentValuesPage', () => {
  beforeEach(() => {
    getBrokersMock.mockReset()
    getCurrentPriceMock.mockReset()
  })

  it('fetches current prices for active assets', async () => {
    getBrokersMock.mockResolvedValue([
      {
        name: 'XPI',
        currency: 'BRL',
        portfolioCount: 1,
        totalAssets: 1,
        portfolios: [
          {
            name: 'Default',
            assetCount: 1,
            activeAssetCount: 1,
            assets: [
              {
                name: 'BCIA11',
                ticker: 'BCIA11',
                exchange: 'BVMF',
                country: 0,
                localTypeCode: '',
                class: 0,
                isin: 'TEST',
                quantity: 10,
                averagePrice: 100,
                isActive: true,
                operationCount: 0,
                creditCount: 0,
              },
            ],
          },
        ],
      },
    ])
    getCurrentPriceMock.mockResolvedValue({
      exchange: 'BVMF',
      ticker: 'BCIA11',
      name: 'Sample Asset',
      price: 10.5,
      asOf: '2024-02-01T00:00:00Z',
    })

    render(<CurrentValuesPage />)

    expect(await screen.findByRole('heading', { name: 'Read Assets Current Values' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Check Prices' }))

    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11')
    })

    expect(await screen.findByText('Sample Asset')).toBeInTheDocument()
  })
})
