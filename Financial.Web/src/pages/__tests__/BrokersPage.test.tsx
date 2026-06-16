import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import BrokersPage from '../BrokersPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerNodeDto } from '../../api/types'

const getBrokersMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getBrokers: getBrokersMock,
  } satisfies Partial<FinancialApiClient>),
}))

describe('BrokersPage', () => {
  beforeEach(() => {
    getBrokersMock.mockReset()
  })

  it('renders broker list', async () => {
    getBrokersMock.mockResolvedValue([
      {
        name: 'XPI',
        currency: 'BRL',
        portfolioCount: 1,
        totalAssets: 2,
        portfolios: [],
      },
    ] satisfies BrokerNodeDto[])

    render(
      <MemoryRouter>
        <BrokersPage />
      </MemoryRouter>,
    )

    expect(await screen.findByText('Brokers')).toBeInTheDocument()
    expect(await screen.findByText(/XPI/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /XPI/ })).toHaveAttribute('href', '/brokers/XPI')
  })

  it('shows error state when request fails', async () => {
    getBrokersMock.mockRejectedValue(new Error('Boom'))

    render(
      <MemoryRouter>
        <BrokersPage />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })
})
