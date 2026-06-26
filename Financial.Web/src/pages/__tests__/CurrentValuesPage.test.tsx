import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CurrentValuesPage from '../CurrentValuesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AssetPriceDto, BrokerNodeDto } from '../../api/types'

const getBrokersMock = vi.fn()
const getCurrentPriceMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getBrokers: getBrokersMock,
    getCurrentPrice: getCurrentPriceMock,
  } satisfies Partial<FinancialApiClient>),
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
                country: 'Unknown',
                localTypeCode: '',
                class: 'Unknown',
                isin: 'TEST',
                quantity: 10,
                averagePrice: 100,
                isActive: true,
                transactionCount: 0,
                creditCount: 0,
              },
            ],
          },
        ],
      },
    ] satisfies BrokerNodeDto[])
    getCurrentPriceMock.mockResolvedValue({
      exchange: 'BVMF',
      ticker: 'BCIA11',
      name: 'Sample Asset',
      price: 10.5,
      asOf: '2024-02-01T00:00:00Z',
    } satisfies AssetPriceDto)

    render(<CurrentValuesPage />)

    expect(await screen.findByRole('heading', { name: 'Read Assets Current Values' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Check Prices' }))

    await waitFor(() => {
      expect(getCurrentPriceMock).toHaveBeenCalledWith('BVMF', 'BCIA11')
    })

    expect(await screen.findByText('Sample Asset')).toBeInTheDocument()
  })
})
