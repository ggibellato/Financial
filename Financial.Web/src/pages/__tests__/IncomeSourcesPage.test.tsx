import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import IncomeSourcesPage from '../IncomeSourcesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { IncomeSourceDto } from '../../api/types'

const { getIncomeSourcesMock, createIncomeSourceMock, updateIncomeSourceMock, deleteIncomeSourceMock } = vi.hoisted(() => ({
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  createIncomeSourceMock: vi.fn<FinancialApiClient['createIncomeSource']>(),
  updateIncomeSourceMock: vi.fn<FinancialApiClient['updateIncomeSource']>(),
  deleteIncomeSourceMock: vi.fn<FinancialApiClient['deleteIncomeSource']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getIncomeSources: getIncomeSourcesMock,
    createIncomeSource: createIncomeSourceMock,
    updateIncomeSource: updateIncomeSourceMock,
    deleteIncomeSource: deleteIncomeSourceMock,
  } as Partial<FinancialApiClient>,
}))

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: 's1', name: 'Gleison', isActive: true, group: 'Salary', autoSplitToReserve: false, hasReferences: true },
  { id: 's2', name: 'Ariana', isActive: true, group: 'Salary', autoSplitToReserve: true, hasReferences: false },
]

describe('IncomeSourcesPage', () => {
  beforeEach(() => {
    getIncomeSourcesMock.mockReset()
    createIncomeSourceMock.mockReset()
    updateIncomeSourceMock.mockReset()
    deleteIncomeSourceMock.mockReset()
    getIncomeSourcesMock.mockResolvedValue(INCOME_SOURCES)
  })

  it('renders every income source', async () => {
    render(<IncomeSourcesPage />)

    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())
    expect(screen.getByText('Ariana')).toBeInTheDocument()
  })

  it('shows the empty state when there are no income sources', async () => {
    getIncomeSourcesMock.mockResolvedValue([])
    render(<IncomeSourcesPage />)

    expect(await screen.findByText('No income sources yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getIncomeSourcesMock.mockRejectedValue(new Error('Network down'))
    render(<IncomeSourcesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates an income source through the Create Income Source dialog', async () => {
    createIncomeSourceMock.mockResolvedValue({
      id: 's3',
      name: 'Freelance',
      isActive: true,
      group: 'NonReportable',
      autoSplitToReserve: false,
      hasReferences: false,
    })
    render(<IncomeSourcesPage />)
    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Income Source' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Freelance' } })
    fireEvent.change(screen.getByLabelText('Group'), { target: { value: 'NonReportable' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createIncomeSourceMock).toHaveBeenCalledWith({
        name: 'Freelance',
        group: 'NonReportable',
        isActive: true,
        autoSplitToReserve: false,
      }),
    )
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Create Income Source' })).not.toBeInTheDocument(),
    )
  })

  it('edits an income source through its row action', async () => {
    updateIncomeSourceMock.mockResolvedValue({
      id: 's1',
      name: 'Gleison Renamed',
      isActive: true,
      group: 'Salary',
      autoSplitToReserve: false,
      hasReferences: true,
    })
    render(<IncomeSourcesPage />)
    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Gleison' }))
    expect(screen.getByRole('heading', { name: 'Edit Income Source' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Gleison Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateIncomeSourceMock).toHaveBeenCalledWith('s1', {
        name: 'Gleison Renamed',
        group: 'Salary',
        isActive: true,
        autoSplitToReserve: false,
      }),
    )
  })

  it('disables delete confirmation when the income source still has references', async () => {
    render(<IncomeSourcesPage />)
    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Gleison' }))

    expect(screen.getByText(/still used by an income entry and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes an income source with no references', async () => {
    deleteIncomeSourceMock.mockResolvedValue(undefined)
    render(<IncomeSourcesPage />)
    await waitFor(() => expect(screen.getByText('Ariana')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Ariana' }))

    expect(screen.getByText(/will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteIncomeSourceMock).toHaveBeenCalledWith('s2'))
  })
})
