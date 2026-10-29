import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import IncomeSourcesPage from '../IncomeSourcesPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { IncomeSourceDto } from '../../api/types'
import { potsAccount } from '../../test/account'

const { getIncomeSourcesMock, createIncomeSourceMock, updateIncomeSourceMock, deleteIncomeSourceMock, getAccountsMock } =
  vi.hoisted(() => ({
    getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
    createIncomeSourceMock: vi.fn<FinancialApiClient['createIncomeSource']>(),
    updateIncomeSourceMock: vi.fn<FinancialApiClient['updateIncomeSource']>(),
    deleteIncomeSourceMock: vi.fn<FinancialApiClient['deleteIncomeSource']>(),
    getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  }))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getIncomeSources: getIncomeSourcesMock,
    createIncomeSource: createIncomeSourceMock,
    updateIncomeSource: updateIncomeSourceMock,
    deleteIncomeSource: deleteIncomeSourceMock,
    getAccounts: getAccountsMock,
  } as Partial<FinancialApiClient>,
}))

function rowText(name: string): string | null | undefined {
  return screen.getAllByRole('row').find((row) => row.textContent?.includes(name))?.textContent
}

const INCOME_SOURCES: IncomeSourceDto[] = [
  { id: 's1', name: 'Gleison', isActive: true, group: 'Salary', potsAccountId: null, hasReferences: true },
  { id: 's2', name: 'Ariana', isActive: true, group: 'Salary', potsAccountId: 'p1', hasReferences: false },
  { id: 's3', name: 'Lottery', isActive: true, group: 'NonReportable', potsAccountId: 'p2', hasReferences: false },
]

describe('IncomeSourcesPage', () => {
  beforeEach(() => {
    getIncomeSourcesMock.mockReset()
    createIncomeSourceMock.mockReset()
    updateIncomeSourceMock.mockReset()
    deleteIncomeSourceMock.mockReset()
    getAccountsMock.mockReset()
    getIncomeSourcesMock.mockResolvedValue(INCOME_SOURCES)
    getAccountsMock.mockResolvedValue([
      potsAccount({ id: 'p1', name: 'Reserva' }),
      potsAccount({ id: 'p2', name: 'Old pots', isActive: false }),
    ])
  })

  it('shows the Pots account each source splits to, a dash for none, and marks an inactive one', async () => {
    render(<IncomeSourcesPage />)

    await waitFor(() => expect(screen.getByRole('columnheader', { name: 'Splits to' })).toBeInTheDocument())
    await waitFor(() => expect(rowText('Ariana')).toContain('Reserva'))
    expect(rowText('Gleison')).toContain('—')
    expect(rowText('Lottery')).toContain('Old pots (inactive)')
  })

  it('shows a split source as loading, not as no split, while accounts load', async () => {
    getAccountsMock.mockReturnValue(new Promise(() => {}))
    render(<IncomeSourcesPage />)

    await waitFor(() => expect(screen.getByText('Ariana')).toBeInTheDocument())
    expect(rowText('Ariana')).toContain('Loading…')
    expect(rowText('Gleison')).toContain('—')
  })

  it('shows a warning and keeps the list usable when accounts fail to load', async () => {
    getAccountsMock.mockRejectedValue(new Error('Unable to load accounts'))
    render(<IncomeSourcesPage />)

    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())
    expect(await screen.findByText(/couldn.t load accounts/i)).toBeInTheDocument()
    expect(rowText('Ariana')).toContain('Pots account (not loaded)')
    expect(rowText('Ariana')).not.toContain('—')
    fireEvent.click(screen.getByRole('button', { name: 'Retry loading accounts' }))
    await waitFor(() => expect(getAccountsMock).toHaveBeenCalledTimes(2))
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
      potsAccountId: null,
      hasReferences: false,
    })
    render(<IncomeSourcesPage />)
    await waitFor(() => expect(screen.getByText('Gleison')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Income Source' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Freelance' } })
    fireEvent.change(screen.getByLabelText('Group'), { target: { value: 'NonReportable' } })
    await waitFor(() => expect(screen.getByRole('option', { name: 'Reserva' })).toBeInTheDocument())
    fireEvent.change(screen.getByRole('combobox', { name: /^Pots account/ }), { target: { value: 'p1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createIncomeSourceMock).toHaveBeenCalledWith({
        name: 'Freelance',
        group: 'NonReportable',
        isActive: true,
        potsAccountId: 'p1',
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
      potsAccountId: null,
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
        potsAccountId: null,
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
