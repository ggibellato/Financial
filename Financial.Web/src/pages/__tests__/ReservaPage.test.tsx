import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ReservaPage from '../ReservaPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { ReserveBucketBalanceDto, ReserveMovementDto } from '../../api/types'

const getReserveBalancesMock = vi.fn<FinancialApiClient['getReserveBalances']>()
const getReserveMovementsMock = vi.fn<FinancialApiClient['getReserveMovements']>()
const postIncomeSplitMock = vi.fn<FinancialApiClient['postIncomeSplit']>()
const postWithdrawalMock = vi.fn<FinancialApiClient['postWithdrawal']>()
const updateReserveMovementMock = vi.fn<FinancialApiClient['updateReserveMovement']>()
const deleteReserveMovementMock = vi.fn<FinancialApiClient['deleteReserveMovement']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getReserveBalances: getReserveBalancesMock,
    getReserveMovements: getReserveMovementsMock,
    postIncomeSplit: postIncomeSplitMock,
    postWithdrawal: postWithdrawalMock,
    updateReserveMovement: updateReserveMovementMock,
    deleteReserveMovement: deleteReserveMovementMock,
  }),
}))

const BALANCES: ReserveBucketBalanceDto[] = [
  { bucket: 'Investimento', balance: 654.33 },
  { bucket: 'HouseTreats', balance: 654.33 },
  { bucket: 'Ariana', balance: 327.17 },
  { bucket: 'Gleison', balance: 327.17 },
]

const MOVEMENTS: ReserveMovementDto[] = [
  { id: 'm1', bucket: 'Investimento', amount: 654.33, date: '2026-07-17', description: 'Ramsay' },
  { id: 'm2', bucket: 'HouseTreats', amount: 654.33, date: '2026-07-17', description: 'Ramsay' },
  { id: 'm3', bucket: 'Ariana', amount: 327.17, date: '2026-07-17', description: 'Ramsay' },
  { id: 'm4', bucket: 'Gleison', amount: 327.17, date: '2026-07-17', description: 'Ramsay' },
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
    expect(screen.getAllByText('Ramsay').length).toBe(4)
  })

  it('shows a group total after the last movement of a same date+description split in history', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByText('Movement History')).toBeInTheDocument())
    const movementSection = screen.getByText('Movement History').closest('section') as HTMLElement
    const rows = within(movementSection).getAllByRole('row')
    // header + 4 movement rows + 1 group-total row
    expect(rows).toHaveLength(6)
    expect(within(rows[5]).getByText('Total split for Ramsay')).toBeInTheDocument()
    expect(within(rows[5]).getByText('1,963.00')).toBeInTheDocument()
  })

  it('renders the total balance across all buckets, bold and always visible', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByText('Bucket Balances')).toBeInTheDocument())
    const balancesSection = screen.getByText('Bucket Balances').closest('section') as HTMLElement
    // 654.33 + 654.33 + 327.17 + 327.17 = 1963.00
    expect(within(balancesSection).getByText('1,963.00')).toBeInTheDocument()
  })

  it('shows the income-split form only after New Income Split is clicked', async () => {
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    expect(screen.queryByText('Post Monthly Income Split')).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))

    expect(screen.getByText('Post Monthly Income Split')).toBeInTheDocument()
    expect(screen.getByLabelText('Amount to Split')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Post Income Split' })).toBeInTheDocument()
  })

  it('shows the posted split breakdown and total after a successful submission', async () => {
    postIncomeSplitMock.mockResolvedValue({
      investimento: 654.33,
      houseTreats: 654.33,
      ariana: 327.17,
      gleison: 327.17,
      total: 1963,
    })
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: 'New Income Split' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'New Income Split' }))
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-01' } })
    fireEvent.change(screen.getByLabelText('Amount to Split'), { target: { value: '1963' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Ramsay' } })
    fireEvent.click(screen.getByRole('button', { name: 'Post Income Split' }))

    await waitFor(() => expect(screen.getByText('Income Split Posted')).toBeInTheDocument())
    const resultPanel = screen.getByText('Income Split Posted').closest('.reserva-page__form-panel') as HTMLElement
    expect(within(resultPanel).getAllByText('654.33')).toHaveLength(2)
    expect(within(resultPanel).getByText('1,963.00')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss' }))
    expect(screen.queryByText('Income Split Posted')).not.toBeInTheDocument()
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

  it('edits a movement via the toggled panel and saves, updating the displayed row', async () => {
    updateReserveMovementMock.mockResolvedValue({ ...MOVEMENTS[0], amount: 700 })
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit movement' })[0])
    expect(screen.getByText('Edit Movement')).toBeInTheDocument()
    const amountInput = screen.getByDisplayValue('654.33')
    fireEvent.change(amountInput, { target: { value: '700' } })

    getReserveMovementsMock.mockResolvedValue([{ ...MOVEMENTS[0], amount: 700 }, ...MOVEMENTS.slice(1)])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateReserveMovementMock).toHaveBeenCalledWith('m1', {
        bucket: 'Investimento',
        amount: 700,
        date: '2026-07-17',
        description: 'Ramsay',
      }),
    )
    await waitFor(() => expect(screen.getByText('700.00')).toBeInTheDocument())
  })

  it('warns that deleting a split movement removes all 4 lines, and deletes on confirm', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    deleteReserveMovementMock.mockResolvedValue(undefined)
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    getReserveMovementsMock.mockResolvedValue([])
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete movement' })[0])

    expect(window.confirm).toHaveBeenCalledWith(
      expect.stringContaining('part of a split and will delete all 4 lines'),
    )
    await waitFor(() => expect(deleteReserveMovementMock).toHaveBeenCalledWith('m1'))
    await waitFor(() => expect(screen.queryAllByText('Ramsay').length).toBe(0))
  })

  it('does not delete a movement when the confirmation prompt is declined', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<ReservaPage />)

    await waitFor(() => expect(screen.getAllByText('Ramsay').length).toBe(4))

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete movement' })[0])

    expect(deleteReserveMovementMock).not.toHaveBeenCalled()
  })
})
