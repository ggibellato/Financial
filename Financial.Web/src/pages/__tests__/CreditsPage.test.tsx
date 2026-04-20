import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CreditsPage from '../CreditsPage'

const getCreditsByBrokerMock = vi.fn()
const getCreditsByPortfolioMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getCreditsByBroker: getCreditsByBrokerMock,
    getCreditsByPortfolio: getCreditsByPortfolioMock,
  }),
}))

describe('CreditsPage', () => {
  beforeEach(() => {
    getCreditsByBrokerMock.mockReset()
    getCreditsByPortfolioMock.mockReset()
  })

  it('renders broker credits', async () => {
    getCreditsByBrokerMock.mockResolvedValue([
      {
        id: '1',
        date: '2024-01-01',
        type: 'Dividend',
        value: 12.5,
      },
    ])

    render(
      <MemoryRouter initialEntries={['/credits/XPI']}>
        <Routes>
          <Route path="/credits/:brokerName" element={<CreditsPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Credits' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '← Back to broker' })).toHaveAttribute('href', '/brokers/XPI')
    expect(screen.getByText(/Dividend/)).toBeInTheDocument()
  })

  it('renders portfolio credits', async () => {
    getCreditsByPortfolioMock.mockResolvedValue([
      {
        id: '2',
        date: '2024-02-01',
        type: 'Fee Refund',
        value: 4.2,
      },
    ])

    render(
      <MemoryRouter initialEntries={['/credits/XPI/Default']}>
        <Routes>
          <Route path="/credits/:brokerName/:portfolioName" element={<CreditsPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Credits' })).toBeInTheDocument()
    expect(getCreditsByPortfolioMock).toHaveBeenCalledWith('XPI', 'Default')
    expect(screen.getByText(/Fee Refund/)).toBeInTheDocument()
    expect(screen.getByText(/Portfolio:/)).toBeInTheDocument()
  })

  it('shows error state when request fails', async () => {
    getCreditsByBrokerMock.mockRejectedValue(new Error('Boom'))

    render(
      <MemoryRouter initialEntries={['/credits/XPI']}>
        <Routes>
          <Route path="/credits/:brokerName" element={<CreditsPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
  })
})
