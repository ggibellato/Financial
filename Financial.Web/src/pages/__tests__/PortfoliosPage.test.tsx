import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import PortfoliosPage from '../PortfoliosPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerDto, PortfolioDto } from '../../api/types'

const {
  getAdminPortfoliosMock,
  createPortfolioMock,
  updatePortfolioMock,
  deleteEmptyPortfolioMock,
  getAdminBrokersMock,
} = vi.hoisted(() => ({
  getAdminPortfoliosMock: vi.fn<FinancialApiClient['getAdminPortfolios']>(),
  createPortfolioMock: vi.fn<FinancialApiClient['createPortfolio']>(),
  updatePortfolioMock: vi.fn<FinancialApiClient['updatePortfolio']>(),
  deleteEmptyPortfolioMock: vi.fn<FinancialApiClient['deleteEmptyPortfolio']>(),
  getAdminBrokersMock: vi.fn<FinancialApiClient['getAdminBrokers']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminPortfolios: getAdminPortfoliosMock,
    createPortfolio: createPortfolioMock,
    updatePortfolio: updatePortfolioMock,
    deleteEmptyPortfolio: deleteEmptyPortfolioMock,
    getAdminBrokers: getAdminBrokersMock,
  } as Partial<FinancialApiClient>,
}))

const PORTFOLIOS: PortfolioDto[] = [
  { name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 2 },
  { name: 'Old', brokerName: 'Avenue', brokerStatus: 'Historic', assetCount: 0 },
]

const BROKERS: BrokerDto[] = [{ name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1 }]

describe('PortfoliosPage', () => {
  beforeEach(() => {
    getAdminPortfoliosMock.mockReset()
    createPortfolioMock.mockReset()
    updatePortfolioMock.mockReset()
    deleteEmptyPortfolioMock.mockReset()
    getAdminBrokersMock.mockReset()
    getAdminPortfoliosMock.mockResolvedValue(PORTFOLIOS)
    getAdminBrokersMock.mockResolvedValue(BROKERS)
  })

  it('renders the portfolio list once loaded', async () => {
    render(<PortfoliosPage />)

    await waitFor(() => expect(screen.getByText('Default')).toBeInTheDocument())
    expect(screen.getByText('Old')).toBeInTheDocument()
  })

  it('shows the empty state when there are no portfolios', async () => {
    getAdminPortfoliosMock.mockResolvedValue([])
    render(<PortfoliosPage />)

    expect(await screen.findByText('No portfolios yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getAdminPortfoliosMock.mockRejectedValue(new Error('Network down'))
    render(<PortfoliosPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates a portfolio through the Create Portfolio dialog', async () => {
    createPortfolioMock.mockResolvedValue({ name: 'New Portfolio', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 0 })
    render(<PortfoliosPage />)
    await waitFor(() => expect(screen.getByText('Default')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Portfolio' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'New Portfolio' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createPortfolioMock).toHaveBeenCalledWith({ brokerName: 'XPI', name: 'New Portfolio' }),
    )
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Portfolio' })).not.toBeInTheDocument())
  })

  it('edits a portfolio through its row action', async () => {
    updatePortfolioMock.mockResolvedValue({ name: 'Default Renamed', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 2 })
    render(<PortfoliosPage />)
    await waitFor(() => expect(screen.getByText('Default')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Default' }))
    expect(screen.getByRole('heading', { name: 'Edit Portfolio' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Default Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updatePortfolioMock).toHaveBeenCalledWith('XPI', 'Default', { name: 'Default Renamed' }),
    )
  })

  it('disables delete confirmation when the portfolio still holds assets', async () => {
    render(<PortfoliosPage />)
    await waitFor(() => expect(screen.getByText('Default')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Default' }))

    expect(screen.getByText(/still holds 2 asset\(s\) and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes an empty portfolio', async () => {
    deleteEmptyPortfolioMock.mockResolvedValue(undefined)
    render(<PortfoliosPage />)
    await waitFor(() => expect(screen.getByText('Old')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Old' }))

    expect(screen.getByText(/holds no assets and will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteEmptyPortfolioMock).toHaveBeenCalledWith('Avenue', 'Old', 'historic'))
  })
})
