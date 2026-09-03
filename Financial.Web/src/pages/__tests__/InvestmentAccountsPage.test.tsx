import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import InvestmentAccountsPage from '../InvestmentAccountsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { InvestmentAccountDto } from '../../api/types'

const { getInvestmentAccountsMock, createInvestmentAccountMock, updateInvestmentAccountMock, deleteInvestmentAccountMock } = vi.hoisted(() => ({
  getInvestmentAccountsMock: vi.fn<FinancialApiClient['getInvestmentAccounts']>(),
  createInvestmentAccountMock: vi.fn<FinancialApiClient['createInvestmentAccount']>(),
  updateInvestmentAccountMock: vi.fn<FinancialApiClient['updateInvestmentAccount']>(),
  deleteInvestmentAccountMock: vi.fn<FinancialApiClient['deleteInvestmentAccount']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getInvestmentAccounts: getInvestmentAccountsMock,
    createInvestmentAccount: createInvestmentAccountMock,
    updateInvestmentAccount: updateInvestmentAccountMock,
    deleteInvestmentAccount: deleteInvestmentAccountMock,
  } as Partial<FinancialApiClient>,
}))

const INVESTMENT_ACCOUNTS: InvestmentAccountDto[] = [
  { id: 'a1', name: 'ChaseSave', isActive: true, isLiability: false, hasNonZeroInvestmentSnapshot: false },
  { id: 'a2', name: 'PlatinumVisa8003', isActive: true, isLiability: true, hasNonZeroInvestmentSnapshot: true },
]

describe('InvestmentAccountsPage', () => {
  beforeEach(() => {
    getInvestmentAccountsMock.mockReset()
    createInvestmentAccountMock.mockReset()
    updateInvestmentAccountMock.mockReset()
    deleteInvestmentAccountMock.mockReset()
    getInvestmentAccountsMock.mockResolvedValue(INVESTMENT_ACCOUNTS)
  })

  it('renders every investment account', async () => {
    render(<InvestmentAccountsPage />)

    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())
    expect(screen.getByText('PlatinumVisa8003')).toBeInTheDocument()
  })

  it('shows the empty state when there are no investment accounts', async () => {
    getInvestmentAccountsMock.mockResolvedValue([])
    render(<InvestmentAccountsPage />)

    expect(await screen.findByText('No investment accounts yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getInvestmentAccountsMock.mockRejectedValue(new Error('Network down'))
    render(<InvestmentAccountsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates an investment account through the Create Investment Account dialog', async () => {
    createInvestmentAccountMock.mockResolvedValue({
      id: 'a3',
      name: 'Monzo Pot',
      isActive: true,
      isLiability: false,
      hasNonZeroInvestmentSnapshot: false,
    })
    render(<InvestmentAccountsPage />)
    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Investment Account' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Monzo Pot' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(createInvestmentAccountMock).toHaveBeenCalledWith({
        name: 'Monzo Pot',
        isActive: true,
        isLiability: false,
      }),
    )
    await waitFor(() =>
      expect(screen.queryByRole('heading', { name: 'Create Investment Account' })).not.toBeInTheDocument(),
    )
  })

  it('edits an investment account through its row action', async () => {
    updateInvestmentAccountMock.mockResolvedValue({
      id: 'a1',
      name: 'ChaseSaveRenamed',
      isActive: true,
      isLiability: false,
      hasNonZeroInvestmentSnapshot: false,
    })
    render(<InvestmentAccountsPage />)
    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit ChaseSave' }))
    expect(screen.getByRole('heading', { name: 'Edit Investment Account' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'ChaseSaveRenamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateInvestmentAccountMock).toHaveBeenCalledWith('a1', {
        name: 'ChaseSaveRenamed',
        isActive: true,
        isLiability: false,
      }),
    )
  })

  it('disables delete confirmation when the account has a non-zero investment snapshot', async () => {
    render(<InvestmentAccountsPage />)
    await waitFor(() => expect(screen.getByText('PlatinumVisa8003')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete PlatinumVisa8003' }))

    expect(screen.getByText(/has a non-zero balance and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes an investment account with no non-zero investment snapshot', async () => {
    deleteInvestmentAccountMock.mockResolvedValue(undefined)
    render(<InvestmentAccountsPage />)
    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete ChaseSave' }))

    expect(screen.getByText(/will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteInvestmentAccountMock).toHaveBeenCalledWith('a1'))
  })
})
