import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import BrokersPage from '../BrokersPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BrokerDto } from '../../api/types'
import { confirmDelete } from '../../test-utils/confirmDelete'
import { findErrorAlert } from '../../test-utils/errorAlert'

const { getAdminBrokersMock, createBrokerMock, updateBrokerMock, setCostBasisMethodMock, deleteBrokerMock } =
  vi.hoisted(() => ({
    getAdminBrokersMock: vi.fn<FinancialApiClient['getAdminBrokers']>(),
    createBrokerMock: vi.fn<FinancialApiClient['createBroker']>(),
    updateBrokerMock: vi.fn<FinancialApiClient['updateBroker']>(),
    setCostBasisMethodMock: vi.fn<FinancialApiClient['setCostBasisMethod']>(),
    deleteBrokerMock: vi.fn<FinancialApiClient['deleteBroker']>(),
  }))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAdminBrokers: getAdminBrokersMock,
    createBroker: createBrokerMock,
    updateBroker: updateBrokerMock,
    setCostBasisMethod: setCostBasisMethodMock,
    deleteBroker: deleteBrokerMock,
  } as Partial<FinancialApiClient>,
}))

const BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 2, costBasisMethod: 'AverageCost' },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 0, costBasisMethod: 'AverageCost' },
]

describe('BrokersPage', () => {
  beforeEach(() => {
    getAdminBrokersMock.mockReset()
    createBrokerMock.mockReset()
    updateBrokerMock.mockReset()
    setCostBasisMethodMock.mockReset()
    deleteBrokerMock.mockReset()
    getAdminBrokersMock.mockResolvedValue(BROKERS)
  })

  it('renders the broker list once loaded', async () => {
    render(<BrokersPage />)

    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())
    expect(screen.getByText('Avenue')).toBeInTheDocument()
  })

  it('lists an active and a historic broker of the same name as two rows without a duplicate-key warning', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})
    getAdminBrokersMock.mockResolvedValue([
      BROKERS[0],
      { ...BROKERS[0], status: 'Historic', portfolioCount: 0 },
    ])
    render(<BrokersPage />)

    await waitFor(() => expect(screen.getAllByText('XPI')).toHaveLength(2))
    expect(screen.getByText('Historic')).toBeInTheDocument()
    expect(consoleError.mock.calls.flat().join(' ')).not.toMatch(/same key/)
    consoleError.mockRestore()
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

  it('creates a broker through the New broker dialog', async () => {
    createBrokerMock.mockResolvedValue({
      name: 'New Broker',
      currency: 'BRL',
      status: 'Active',
      portfolioCount: 0,
      costBasisMethod: 'AverageCost',
    })
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'New broker' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'New Broker' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add broker' }))

    await waitFor(() =>
      expect(createBrokerMock).toHaveBeenCalledWith({
        name: 'New Broker',
        currency: 'BRL',
        costBasisMethod: 'AverageCost',
      }),
    )
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'New broker' })).not.toBeInTheDocument())
  })

  it('edits a broker through its row action', async () => {
    updateBrokerMock.mockResolvedValue({
      name: 'XPI Renamed',
      currency: 'BRL',
      status: 'Active',
      portfolioCount: 2,
      costBasisMethod: 'AverageCost',
    })
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit XPI' }))
    expect(screen.getByRole('heading', { name: 'Edit broker' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'XPI Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateBrokerMock).toHaveBeenCalledWith('XPI', { name: 'XPI Renamed', currency: 'BRL' }, 'active'),
    )
    expect(setCostBasisMethodMock).not.toHaveBeenCalled()
  })

  it('edits a historic broker through its row action, forwarding scope=historic', async () => {
    // Regression: the same broker name can have both an Active and a Historic record; without
    // forwarding the row's own status as the scope, this would resolve to the wrong (Active) one.
    getAdminBrokersMock.mockResolvedValue([
      ...BROKERS,
      { name: 'Closed Broker', currency: 'GBP', status: 'Historic', portfolioCount: 0, costBasisMethod: 'AverageCost' },
    ])
    updateBrokerMock.mockResolvedValue({
      name: 'Closed Broker Renamed',
      currency: 'GBP',
      status: 'Historic',
      portfolioCount: 0,
      costBasisMethod: 'AverageCost',
    })
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('Closed Broker')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Closed Broker' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Closed Broker Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateBrokerMock).toHaveBeenCalledWith(
        'Closed Broker',
        { name: 'Closed Broker Renamed', currency: 'GBP' },
        'historic',
      ),
    )
  })

  it('also calls setCostBasisMethod when editing changes the cost basis method', async () => {
    updateBrokerMock.mockResolvedValue({
      name: 'XPI',
      currency: 'BRL',
      status: 'Active',
      portfolioCount: 2,
      costBasisMethod: 'AverageCost',
    })
    setCostBasisMethodMock.mockResolvedValue({
      name: 'XPI',
      currency: 'BRL',
      status: 'Active',
      portfolioCount: 2,
      costBasisMethod: 'FIFO',
    })
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit XPI' }))
    fireEvent.change(screen.getByRole('combobox', { name: /^Cost basis method/ }), { target: { value: 'FIFO' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(setCostBasisMethodMock).toHaveBeenCalledWith('XPI', { method: 'FIFO' }, 'active'))
  })

  it('reports a partial-success error (not a full-failure one) when the rename commits but the method change fails', async () => {
    updateBrokerMock.mockResolvedValue({
      name: 'XPI Renamed',
      currency: 'BRL',
      status: 'Active',
      portfolioCount: 2,
      costBasisMethod: 'AverageCost',
    })
    setCostBasisMethodMock.mockRejectedValue(new Error('Regeneration failed'))
    render(<BrokersPage />)
    await waitFor(() => expect(screen.getByText('XPI')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit XPI' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'XPI Renamed' } })
    fireEvent.change(screen.getByRole('combobox', { name: /^Cost basis method/ }), { target: { value: 'FIFO' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText(/"XPI Renamed" was saved, but its cost basis method could not be changed/)).toBeInTheDocument()
    // The dialog must still be open, targeting the already-renamed broker - not the old name,
    // which no longer exists server-side after the partial success.
    expect(screen.getByRole('heading', { name: 'Edit broker' })).toBeInTheDocument()
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

  it('keeps focus on the list after a delete reloads it', async () => {
    deleteBrokerMock.mockResolvedValue(undefined)
    render(<BrokersPage />)
    const deleteAvenue = await screen.findByRole('button', { name: 'Delete Avenue' })
    getAdminBrokersMock.mockResolvedValue([BROKERS[0]])

    deleteAvenue.focus()
    fireEvent.click(deleteAvenue)
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete XPI', hidden: true })).toHaveFocus())
  })

  it('keeps focus on the deleting row, then moves it to the next row once the delete completes', async () => {
    let completeDelete: () => void = () => {}
    deleteBrokerMock.mockReturnValue(new Promise<void>((resolve) => (completeDelete = resolve)))
    getAdminBrokersMock.mockResolvedValue([...BROKERS, INTER])
    render(<BrokersPage />)
    await confirmDelete('Avenue', 'broker')

    await waitFor(() => expect(screen.getByRole('button', { name: 'Deleting Avenue', hidden: true })).toHaveFocus())

    getAdminBrokersMock.mockResolvedValue([BROKERS[0], INTER])
    completeDelete()

    await waitFor(() => expect(screen.getByRole('button', { name: 'Delete Inter' })).toHaveFocus())
  })

  it('moves focus to the announced error when a delete fails', async () => {
    deleteBrokerMock.mockRejectedValue(new Error('Avenue could not be deleted.'))
    render(<BrokersPage />)
    await confirmDelete('Avenue', 'broker')

    const alert = await findErrorAlert({ hidden: true })
    expect(alert).toHaveTextContent('Avenue could not be deleted.')
    await waitFor(() => expect(alert).toHaveFocus())
  })
})

const INTER: BrokerDto = { name: 'Inter', currency: 'BRL', status: 'Active', portfolioCount: 0, costBasisMethod: 'AverageCost' }
