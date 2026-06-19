import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import NavigationTreePanel from '../NavigationTreePanel'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { TreeNodeDto } from '../../api/types'

const getNavigationTreeMock = vi.fn()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getNavigationTree: getNavigationTreeMock,
  }),
}))

const stubTree: TreeNodeDto = {
  nodeType: 'Investments',
  displayName: 'All Investments',
  metadata: {},
  children: [
    {
      nodeType: 'Broker',
      displayName: 'XPI (BRL)',
      metadata: { BrokerName: 'XPI', Currency: 'BRL', PortfolioCount: 1, TotalAssets: 1 },
      children: [],
    },
  ],
}

const renderPanel = (props?: { title?: string }) =>
  render(
    <MemoryRouter>
      <NavigationTreePanel {...props} />
    </MemoryRouter>,
  )

describe('NavigationTreePanel', () => {
  beforeEach(() => {
    getNavigationTreeMock.mockReset()
  })

  it('renders tree nodes after the API resolves', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)

    renderPanel()

    expect(await screen.findByText(/All Investments/)).toBeInTheDocument()
    expect(screen.getByText(/XPI/)).toBeInTheDocument()
  })

  it('shows loading state while the API call is in flight', () => {
    getNavigationTreeMock.mockReturnValue(new Promise(() => {}))

    renderPanel()

    expect(screen.getByText('Loading navigation tree...')).toBeInTheDocument()
  })

  it('shows error state when the API call fails', async () => {
    getNavigationTreeMock.mockRejectedValue(new Error('Network error'))

    renderPanel()

    expect(await screen.findByRole('alert')).toHaveTextContent('Network error')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders the title prop as a heading', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)

    renderPanel({ title: 'My Portfolio' })

    expect(await screen.findByRole('heading', { name: 'My Portfolio' })).toBeInTheDocument()
  })

  it('renders broker link with correct href', async () => {
    getNavigationTreeMock.mockResolvedValue(stubTree)

    renderPanel()

    await screen.findByText(/All Investments/)
    expect(screen.getByRole('link', { name: /XPI/ })).toHaveAttribute('href', '/brokers/XPI')
  })

  it('retries the API call when the retry button is clicked', async () => {
    getNavigationTreeMock
      .mockRejectedValueOnce(new Error('First failure'))
      .mockResolvedValueOnce(stubTree)

    renderPanel()

    await screen.findByRole('alert')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
    expect(screen.getByText(/All Investments/)).toBeInTheDocument()
    expect(getNavigationTreeMock).toHaveBeenCalledTimes(2)
  })
})
