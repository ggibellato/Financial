import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import BanksPage from '../BanksPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { BankDto } from '../../api/types'

const { getBanksMock, createBankMock, updateBankMock, deleteBankMock } = vi.hoisted(() => ({
  getBanksMock: vi.fn<FinancialApiClient['getBanks']>(),
  createBankMock: vi.fn<FinancialApiClient['createBank']>(),
  updateBankMock: vi.fn<FinancialApiClient['updateBank']>(),
  deleteBankMock: vi.fn<FinancialApiClient['deleteBank']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getBanks: getBanksMock,
    createBank: createBankMock,
    updateBank: updateBankMock,
    deleteBank: deleteBankMock,
  } as Partial<FinancialApiClient>,
}))

const BANKS: BankDto[] = [
  { id: 'b1', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: true },
  { id: 'b2', name: 'Chase', roundUpEnabled: true, openingBalance: 100, openingBalanceDate: '2026-01-01', hasReferences: false },
]

describe('BanksPage', () => {
  beforeEach(() => {
    getBanksMock.mockReset()
    createBankMock.mockReset()
    updateBankMock.mockReset()
    deleteBankMock.mockReset()
    getBanksMock.mockResolvedValue(BANKS)
  })

  it('renders the bank list once loaded', async () => {
    render(<BanksPage />)

    await waitFor(() => expect(screen.getByText('Barclays')).toBeInTheDocument())
    expect(screen.getByText('Chase')).toBeInTheDocument()
  })

  it('shows the empty state when there are no banks', async () => {
    getBanksMock.mockResolvedValue([])
    render(<BanksPage />)

    expect(await screen.findByText('No banks yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getBanksMock.mockRejectedValue(new Error('Network down'))
    render(<BanksPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates a bank through the Create Bank dialog', async () => {
    createBankMock.mockResolvedValue({
      id: 'b3',
      name: 'Monzo',
      roundUpEnabled: false,
      openingBalance: 0,
      openingBalanceDate: '2026-01-01',
      hasReferences: false,
    })
    render(<BanksPage />)
    await waitFor(() => expect(screen.getByText('Barclays')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Bank' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Monzo' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(createBankMock).toHaveBeenCalledWith({ name: 'Monzo', roundUpEnabled: false }))
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Bank' })).not.toBeInTheDocument())
  })

  it('edits a bank through its row action', async () => {
    updateBankMock.mockResolvedValue({
      id: 'b1',
      name: 'Barclays Renamed',
      roundUpEnabled: false,
      openingBalance: 0,
      openingBalanceDate: '2026-01-01',
      hasReferences: true,
    })
    render(<BanksPage />)
    await waitFor(() => expect(screen.getByText('Barclays')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Barclays' }))
    expect(screen.getByRole('heading', { name: 'Edit Bank' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Barclays Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateBankMock).toHaveBeenCalledWith('b1', { name: 'Barclays Renamed', roundUpEnabled: false }),
    )
  })

  it('disables delete confirmation when the bank still has references', async () => {
    render(<BanksPage />)
    await waitFor(() => expect(screen.getByText('Barclays')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Barclays' }))

    expect(screen.getByText(/still has balance history or transactions and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes a bank with no references', async () => {
    deleteBankMock.mockResolvedValue(undefined)
    render(<BanksPage />)
    await waitFor(() => expect(screen.getByText('Chase')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Chase' }))

    expect(screen.getByText(/will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteBankMock).toHaveBeenCalledWith('b2'))
  })
})
