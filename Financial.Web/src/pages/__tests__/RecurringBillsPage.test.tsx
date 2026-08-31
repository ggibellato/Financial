import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import RecurringBillsPage from '../RecurringBillsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { RecurringBillDto } from '../../api/types'

const { getMensaisBillsMock, createMensaisBillMock, updateMensaisBillMock, deleteMensaisBillMock } = vi.hoisted(() => ({
  getMensaisBillsMock: vi.fn<FinancialApiClient['getMensaisBills']>(),
  createMensaisBillMock: vi.fn<FinancialApiClient['createMensaisBill']>(),
  updateMensaisBillMock: vi.fn<FinancialApiClient['updateMensaisBill']>(),
  deleteMensaisBillMock: vi.fn<FinancialApiClient['deleteMensaisBill']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getMensaisBills: getMensaisBillsMock,
    createMensaisBill: createMensaisBillMock,
    updateMensaisBill: updateMensaisBillMock,
    deleteMensaisBill: deleteMensaisBillMock,
  } as Partial<FinancialApiClient>,
}))

const BILLS: RecurringBillDto[] = [
  { id: 'b1', dueDay: 10, description: 'INSS', value: 850, area: 'Brasil', note: '', nitNumber: null, minimumWageValue: null, status: 'Unset' },
  { id: 'b2', dueDay: 5, description: 'Rent', value: 1500, area: 'UK', note: '', nitNumber: null, minimumWageValue: null, status: 'Unset' },
]

describe('RecurringBillsPage', () => {
  beforeEach(() => {
    getMensaisBillsMock.mockReset()
    createMensaisBillMock.mockReset()
    updateMensaisBillMock.mockReset()
    deleteMensaisBillMock.mockReset()
    getMensaisBillsMock.mockResolvedValue(BILLS)
  })

  it('renders every recurring bill', async () => {
    render(<RecurringBillsPage />)

    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())
    expect(screen.getByText('Rent')).toBeInTheDocument()
  })

  it('shows the empty state when there are no recurring bills', async () => {
    getMensaisBillsMock.mockResolvedValue([])
    render(<RecurringBillsPage />)

    expect(await screen.findByText('No recurring bills yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getMensaisBillsMock.mockRejectedValue(new Error('Network down'))
    render(<RecurringBillsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('sorts by Due Day when the column header is clicked', async () => {
    render(<RecurringBillsPage />)
    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Due Day' }))

    const rows = screen.getAllByRole('row').slice(1)
    expect(rows[0]).toHaveTextContent('Rent')
    expect(rows[1]).toHaveTextContent('INSS')
  })

  it('creates a recurring bill through the Create Recurring Bill dialog', async () => {
    createMensaisBillMock.mockResolvedValue({
      id: 'b3',
      dueDay: 20,
      description: 'Utilities',
      value: 300,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Unset',
    })
    render(<RecurringBillsPage />)
    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Recurring Bill' }))
    fireEvent.change(screen.getByLabelText(/^Due Day/), { target: { value: '20' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Utilities' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '300' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createMensaisBillMock).toHaveBeenCalledWith({
        dueDay: 20,
        description: 'Utilities',
        value: 300,
        area: 'Brasil',
        note: '',
      }),
    )
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Create Recurring Bill' })).not.toBeInTheDocument(),
    )
  })

  it('edits a recurring bill through its row action', async () => {
    updateMensaisBillMock.mockResolvedValue({
      id: 'b1',
      dueDay: 10,
      description: 'INSS Renamed',
      value: 850,
      area: 'Brasil',
      note: '',
      nitNumber: null,
      minimumWageValue: null,
      status: 'Paid',
    })
    render(<RecurringBillsPage />)
    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit INSS' }))
    expect(screen.getByRole('heading', { name: 'Edit Recurring Bill' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'INSS Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateMensaisBillMock).toHaveBeenCalledWith('b1', {
        dueDay: 10,
        description: 'INSS Renamed',
        value: 850,
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
        status: 'Unset',
      }),
    )
  })

  it('deletes a recurring bill after confirmation', async () => {
    deleteMensaisBillMock.mockResolvedValue(undefined)
    render(<RecurringBillsPage />)
    await waitFor(() => expect(screen.getByText('INSS')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete INSS' }))
    expect(screen.getByText(/will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteMensaisBillMock).toHaveBeenCalledWith('b1'))
  })
})
