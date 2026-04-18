import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import BrokerDetailPage from '../BrokerDetailPage'

const getBrokersMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getBrokers: getBrokersMock,
  }),
}))

describe('BrokerDetailPage', () => {
  beforeEach(() => {
    getBrokersMock.mockReset()
  })

  it('renders broker details', async () => {
    getBrokersMock.mockResolvedValue([
      {
        name: 'XPI',
        currency: 'BRL',
        portfolioCount: 1,
        totalAssets: 2,
        portfolios: [
          {
            name: 'Default',
            assetCount: 2,
            activeAssetCount: 2,
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
                operationCount: 1,
                creditCount: 1,
              },
            ],
          },
        ],
      },
    ])

    render(
      <MemoryRouter initialEntries={['/brokers/XPI']}>
        <Routes>
          <Route path="/brokers/:brokerName" element={<BrokerDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'XPI' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Portfolios' })).toBeInTheDocument()
    expect(screen.getByText(/Default/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'BCIA11' })).toHaveAttribute(
      'href',
      '/assets/XPI/Default/BCIA11',
    )
  })

  it('shows error state when broker is missing', async () => {
    getBrokersMock.mockResolvedValue([])

    render(
      <MemoryRouter initialEntries={['/brokers/NOPE']}>
        <Routes>
          <Route path="/brokers/:brokerName" element={<BrokerDetailPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Broker not found.')
  })
})
