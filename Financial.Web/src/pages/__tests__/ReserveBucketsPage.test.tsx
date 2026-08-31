import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReserveBucketsPage from '../ReserveBucketsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { ReserveBucketDto } from '../../api/types'

const { getReserveBucketsMock, createReserveBucketMock, updateReserveBucketMock } = vi.hoisted(() => ({
  getReserveBucketsMock: vi.fn<FinancialApiClient['getReserveBuckets']>(),
  createReserveBucketMock: vi.fn<FinancialApiClient['createReserveBucket']>(),
  updateReserveBucketMock: vi.fn<FinancialApiClient['updateReserveBucket']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getReserveBuckets: getReserveBucketsMock,
    createReserveBucket: createReserveBucketMock,
    updateReserveBucket: updateReserveBucketMock,
  } as Partial<FinancialApiClient>,
}))

const BUCKETS: ReserveBucketDto[] = [
  { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 60, warning: null },
  { id: 'b2', name: 'Ferias', isActive: true, splitPercentage: 40, warning: null },
]

describe('ReserveBucketsPage', () => {
  beforeEach(() => {
    getReserveBucketsMock.mockReset()
    createReserveBucketMock.mockReset()
    updateReserveBucketMock.mockReset()
    getReserveBucketsMock.mockResolvedValue(BUCKETS)
  })

  it('renders every reserve bucket', async () => {
    render(<ReserveBucketsPage />)

    await waitFor(() => expect(screen.getByText('Investimento')).toBeInTheDocument())
    expect(screen.getByText('Ferias')).toBeInTheDocument()
  })

  it('shows the empty state when there are no reserve buckets', async () => {
    getReserveBucketsMock.mockResolvedValue([])
    render(<ReserveBucketsPage />)

    expect(await screen.findByText('No reserve buckets yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getReserveBucketsMock.mockRejectedValue(new Error('Network down'))
    render(<ReserveBucketsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('shows a persistent warning banner when active buckets do not sum to 100', async () => {
    getReserveBucketsMock.mockResolvedValue([
      { id: 'b1', name: 'Investimento', isActive: true, splitPercentage: 60, warning: null },
    ])
    render(<ReserveBucketsPage />)

    await waitFor(() => expect(screen.getByText('Investimento')).toBeInTheDocument())
    expect(screen.getByText(/Active buckets currently sum to 60/)).toBeInTheDocument()
  })

  it('does not show a warning banner when active buckets sum to 100', async () => {
    render(<ReserveBucketsPage />)

    await waitFor(() => expect(screen.getByText('Investimento')).toBeInTheDocument())
    expect(screen.queryByText(/review your split percentages/)).not.toBeInTheDocument()
  })

  it('creates a reserve bucket through the Create Reserve Bucket dialog', async () => {
    createReserveBucketMock.mockResolvedValue({
      id: 'b3',
      name: 'Emergencia',
      isActive: true,
      splitPercentage: 10,
      warning: null,
    })
    render(<ReserveBucketsPage />)
    await waitFor(() => expect(screen.getByText('Investimento')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Reserve Bucket' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Emergencia' } })
    fireEvent.change(screen.getByLabelText(/^Split Percentage/), { target: { value: '10' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createReserveBucketMock).toHaveBeenCalledWith({ name: 'Emergencia', splitPercentage: 10, isActive: true }),
    )
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Create Reserve Bucket' })).not.toBeInTheDocument(),
    )
  })

  it('edits a reserve bucket through its row action', async () => {
    updateReserveBucketMock.mockResolvedValue({
      id: 'b1',
      name: 'InvestimentoRenamed',
      isActive: true,
      splitPercentage: 60,
      warning: null,
    })
    render(<ReserveBucketsPage />)
    await waitFor(() => expect(screen.getByText('Investimento')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Investimento' }))
    expect(screen.getByRole('heading', { name: 'Edit Reserve Bucket' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'InvestimentoRenamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateReserveBucketMock).toHaveBeenCalledWith('b1', { name: 'InvestimentoRenamed', splitPercentage: 60, isActive: true }),
    )
  })

  it('deactivates a reserve bucket after confirmation, with wording stating it is deactivated not removed', async () => {
    updateReserveBucketMock.mockResolvedValue({ ...BUCKETS[0], isActive: false })
    render(<ReserveBucketsPage />)
    await waitFor(() => expect(screen.getByText('Investimento')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Investimento' }))
    expect(screen.getByText(/will be deactivated, not removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() =>
      expect(updateReserveBucketMock).toHaveBeenCalledWith('b1', { name: 'Investimento', splitPercentage: 60, isActive: false }),
    )
  })

  it('disables the delete action for an already-inactive bucket', async () => {
    getReserveBucketsMock.mockResolvedValue([{ id: 'b1', name: 'Retired', isActive: false, splitPercentage: 0, warning: null }])
    render(<ReserveBucketsPage />)

    await waitFor(() => expect(screen.getByText('Retired')).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Delete Retired' })).toBeDisabled()
  })
})
