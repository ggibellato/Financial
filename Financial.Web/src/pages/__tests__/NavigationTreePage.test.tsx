import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import NavigationTreePage from '../NavigationTreePage'

const getNavigationTreeMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: () => ({
    getNavigationTree: getNavigationTreeMock,
  }),
}))

describe('NavigationTreePage', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
  })

  it('renders the navigation tree', async () => {
    getNavigationTreeMock.mockResolvedValue({
      nodeType: 'Investments',
      displayName: 'All Investments',
      metadata: {},
      children: [
        {
          nodeType: 'Broker',
          displayName: 'XPI',
          metadata: {},
          children: [],
        },
      ],
    })

    render(
      <MemoryRouter>
        <NavigationTreePage />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Navigation Tree' })).toBeInTheDocument()
    expect(screen.getByText(/All Investments/)).toBeInTheDocument()
    expect(screen.getByText(/XPI/)).toBeInTheDocument()
  })

  it('shows error state when request fails', async () => {
    getNavigationTreeMock.mockRejectedValue(new Error('Boom'))

    render(
      <MemoryRouter>
        <NavigationTreePage />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('alert')).toHaveTextContent('Boom')
  })
})
