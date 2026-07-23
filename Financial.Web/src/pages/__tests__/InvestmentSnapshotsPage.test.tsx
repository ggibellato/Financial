import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import InvestmentSnapshotsPage from '../InvestmentSnapshotsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { InvestmentSnapshotDto } from '../../api/types'

const getInvestmentSnapshotsMock = vi.fn<FinancialApiClient['getInvestmentSnapshots']>()
const updateInvestmentSnapshotValueMock = vi.fn<FinancialApiClient['updateInvestmentSnapshotValue']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getInvestmentSnapshots: getInvestmentSnapshotsMock,
    updateInvestmentSnapshotValue: updateInvestmentSnapshotValueMock,
  }),
}))

const SNAPSHOTS: InvestmentSnapshotDto[] = Array.from({ length: 11 }, (_, i) => ({
  id: `s${i}`,
  account: `Account${i}`,
  isLiability: i === 1,
  year: 2026,
  month: 7,
  value: 100 * i,
}))

describe('InvestmentSnapshotsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getInvestmentSnapshotsMock.mockResolvedValue(SNAPSHOTS)
  })

  it('shows a loading state before data arrives', () => {
    render(<InvestmentSnapshotsPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getInvestmentSnapshotsMock.mockRejectedValue(new Error('Network down'))

    render(<InvestmentSnapshotsPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('renders all 11 accounts with their values once loaded', async () => {
    render(<InvestmentSnapshotsPage />)

    await waitFor(() => expect(screen.getByText('Account0')).toBeInTheDocument())
    expect(screen.getAllByRole('row')).toHaveLength(13) // header + 11 accounts + totals row
    expect(screen.getByText('Account1 (liability)')).toBeInTheDocument()
  })

  it('renders the total value net of liability accounts', async () => {
    render(<InvestmentSnapshotsPage />)

    await waitFor(() => expect(screen.getByText('Account0')).toBeInTheDocument())
    // values 0..1000 step 100 sum to 5500; Account1 (value 100) is a liability, so it is subtracted twice: 5500 - 200 = 5300
    expect(screen.getByText('5,300.00')).toBeInTheDocument()
  })

  it('edits a row value and saves, updating the displayed row', async () => {
    updateInvestmentSnapshotValueMock.mockResolvedValue({ ...SNAPSHOTS[0], value: 999 })
    render(<InvestmentSnapshotsPage />)

    await waitFor(() => expect(screen.getByText('Account0')).toBeInTheDocument())

    const editButtons = screen.getAllByRole('button', { name: 'Edit' })
    fireEvent.click(editButtons[0])

    expect(screen.getByText('Edit Snapshot')).toBeInTheDocument()
    const valueInput = screen.getByDisplayValue('0')
    fireEvent.change(valueInput, { target: { value: '999' } })

    getInvestmentSnapshotsMock.mockResolvedValue([{ ...SNAPSHOTS[0], value: 999 }, ...SNAPSHOTS.slice(1)])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateInvestmentSnapshotValueMock).toHaveBeenCalledWith('s0', { value: 999 }))
    await waitFor(() => expect(screen.getByText('999.00')).toBeInTheDocument())
  })
})
