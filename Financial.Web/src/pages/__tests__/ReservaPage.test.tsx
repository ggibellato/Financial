import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReservaPage from '../ReservaPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { ReserveBucketBalanceDto, ReserveMovementDto } from '../../api/types'

const getReserveBalancesMock = vi.fn<FinancialApiClient['getReserveBalances']>()
const getReserveMovementsMock = vi.fn<FinancialApiClient['getReserveMovements']>()
const postIncomeSplitMock = vi.fn<FinancialApiClient['postIncomeSplit']>()
const postWithdrawalMock = vi.fn<FinancialApiClient['postWithdrawal']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getReserveBalances: getReserveBalancesMock,
    getReserveMovements: getReserveMovementsMock,
    postIncomeSplit: postIncomeSplitMock,
    postWithdrawal: postWithdrawalMock,
  }),
}))

const BALANCES: ReserveBucketBalanceDto[] = [
  { bucket: 'Investimento', balance: 654.33 },
  { bucket: 'HouseTreats', balance: 654.33 },
  { bucket: 'Ariana', balance: 327.17 },
  { bucket: 'Gleison', balance: 327.17 },
]

const MOVEMENTS: ReserveMovementDto[] = [
  { id: 'm1', bucket: 'Investimento', amount: 654.33, date: '2026-07-01', description: 'Monthly income split' },
]

describe('ReservaPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getReserveBalancesMock.mockResolvedValue(BALANCES)
    getReserveMovementsMock.mockResolvedValue(MOVEMENTS)
  })

  it('shows a loading state before data arrives', () => {
    render(<ReservaPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getReserveBalancesMock.mockRejectedValue(new Error('Network down'))

    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('renders all 4 bucket balances and the movement history once loaded', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByText('Bucket Balances')).toBeInTheDocument())
    for (const b of BALANCES) {
      expect(screen.getAllByText(b.bucket).length).toBeGreaterThan(0)
    }
    expect(screen.getByText('Monthly income split')).toBeInTheDocument()
  })

  it('shows the income-split form only after New Income Split is clicked', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    expect(screen.queryByText('Post Monthly Income Split')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))

    expect(screen.getByText('Post Monthly Income Split')).toBeInTheDocument()
    expect(screen.getByLabelText('Salario Gleison (gross)')).toBeInTheDocument()
    expect(screen.getByLabelText('Salario Ariana (net)')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Post Income Split' })).toBeInTheDocument()
  })

  it('shows the withdrawal form only after New Withdrawal is clicked', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Withdrawal' })).toBeInTheDocument())
    expect(screen.queryByText('Record a Withdrawal')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Withdrawal' }))

    expect(screen.getByText('Record a Withdrawal')).toBeInTheDocument()
    expect(screen.getByLabelText('Bucket')).toBeInTheDocument()
    expect(screen.getByLabelText('Amount')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Record Withdrawal' })).toBeInTheDocument()
  })
})
