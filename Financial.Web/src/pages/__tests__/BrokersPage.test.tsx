import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import BrokersPage from '../BrokersPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerDto } from '../../api/types'

const { getAdminBrokersMock, createBrokerMock, updateBrokerMock, deleteBrokerMock } = vi.hoisted(() => ({
  getAdminBrokersMock: vi.fn<FinancialApiClient['getAdminBrokers']>(),
  createBrokerMock: vi.fn<FinancialApiClient['createBroker']>(),
  updateBrokerMock: vi.fn<FinancialApiClient['updateBroker']>(),
  deleteBrokerMock: vi.fn<FinancialApiClient['deleteBroker']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminBrokers: getAdminBrokersMock,
    createBroker: createBrokerMock,
    updateBroker: updateBrokerMock,
    deleteBroker: deleteBrokerMock,
  } as Partial<FinancialApiClient>,
}))

const BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 2 },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 0 },
]

describe('BrokersPage', () => {
  beforeEach(() => {
    getAdminBrokersMock.mockReset()
    createBrokerMock.mockReset()
    updateBrokerMock.mockReset()
    deleteBrokerMock.mockReset()
    getAdminBrokersMock.mockResolvedValue(BROKERS)
  })

  it('renders the broker list once loaded', async () => {
    render(<BrokersPage />)

    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())
    expect(screen.getByText('Avenue')).toBeInTheDocument()
  })

  it('shows the empty state when there are no brokers', async () => {
    getAdminBrokersMock.mockResolvedValue([])
    render(<BrokersPage />)

    expect(await screen.findByText('No brokers yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getAdminBrokersMock.mockRejectedValue(new Error('Network down'))
    render(<BrokersPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates a broker through the Create Broker dialog', async () => {
    createBrokerMock.mockResolvedValue({ name: 'New Broker', currency: 'BRL', status: 'Active', portfolioCount: 0 })
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Broker' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'New Broker' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(createBrokerMock).toHaveBeenCalledWith({ name: 'New Broker', currency: 'BRL' }))
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Broker' })).not.toBeInTheDocument())
  })

  it('edits a broker through its row action', async () => {
    updateBrokerMock.mockResolvedValue({ name: 'XPI Renamed', currency: 'BRL', status: 'Active', portfolioCount: 2 })
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit XPI' }))
    expect(screen.getByRole('heading', { name: 'Edit Broker' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'XPI Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateBrokerMock).toHaveBeenCalledWith('XPI', { name: 'XPI Renamed', currency: 'BRL' }),
    )
  })

  it('disables delete confirmation when the broker still has portfolios', async () => {
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete XPI' }))

    expect(screen.getByText(/still has 2 portfolio\(s\) and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes an empty Active broker and shows the Historic-archive wording', async () => {
    deleteBrokerMock.mockResolvedValue(undefined)
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('Avenue')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Avenue' }))

    expect(screen.getByText(/will move to the Historic list/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteBrokerMock).toHaveBeenCalledWith('Avenue'))
  })
})
