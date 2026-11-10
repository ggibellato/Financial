import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import InvestmentAccountsPage from '../InvestmentAccountsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { InvestmentAccountDto } from '../../api/types'
import { account, cardAccount, potsAccount } from '../../test/account'

const {
  getInvestmentAccountsMock,
  createInvestmentAccountMock,
  updateInvestmentAccountMock,
  deleteInvestmentAccountMock,
  getAccountsMock,
} = vi.hoisted(() => ({
  getInvestmentAccountsMock: vi.fn<FinancialApiClient['getInvestmentAccounts']>(),
  createInvestmentAccountMock: vi.fn<FinancialApiClient['createInvestmentAccount']>(),
  updateInvestmentAccountMock: vi.fn<FinancialApiClient['updateInvestmentAccount']>(),
  deleteInvestmentAccountMock: vi.fn<FinancialApiClient['deleteInvestmentAccount']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getInvestmentAccounts: getInvestmentAccountsMock,
    createInvestmentAccount: createInvestmentAccountMock,
    updateInvestmentAccount: updateInvestmentAccountMock,
    deleteInvestmentAccount: deleteInvestmentAccountMock,
    getAccounts: getAccountsMock,
  } as Partial<FinancialApiClient>,
}))

const INVESTMENT_ACCOUNTS: InvestmentAccountDto[] = [
  { id: 'a1', name: 'ChaseSave', isActive: true, isLiability: false, hasNonZeroInvestmentSnapshot: false, source: 'None', creditCardId: null, potsAccountId: null },
  { id: 'a2', name: 'PlatinumVisa8003', isActive: true, isLiability: true, hasNonZeroInvestmentSnapshot: true, source: 'CreditCard', creditCardId: 'cc1', potsAccountId: null },
  { id: 'a3', name: 'Reservas pessoais', isActive: true, isLiability: false, hasNonZeroInvestmentSnapshot: false, source: 'PotsAccountTotal', creditCardId: null, potsAccountId: 'p1' },
]

describe('InvestmentAccountsPage', () => {
  beforeEach(() => {
    getInvestmentAccountsMock.mockReset()
    createInvestmentAccountMock.mockReset()
    updateInvestmentAccountMock.mockReset()
    deleteInvestmentAccountMock.mockReset()
    getAccountsMock.mockReset()
    getInvestmentAccountsMock.mockResolvedValue(INVESTMENT_ACCOUNTS)
    getAccountsMock.mockResolvedValue([
      potsAccount({ id: 'p1', name: 'Reserva' }),
      cardAccount({ id: 'cc1', name: 'Platinum Visa 8003' }),
      account({ id: 'b1', name: 'Barclays' }),
    ])
  })

  it('renders every investment account', async () => {
    render(<InvestmentAccountsPage />)

    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())
    expect(screen.getByText('PlatinumVisa8003')).toBeInTheDocument()
  })

  it('offers only the accounts with statements in the credit card picker', async () => {
    render(<InvestmentAccountsPage />)
    await waitFor(() => expect(screen.getByText('Platinum Visa 8003')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Investment Account' }))
    fireEvent.change(await screen.findByLabelText('Source'), { target: { value: 'CreditCard' } })
    const picker = (await screen.findByLabelText(/^Credit Card/)) as HTMLSelectElement

    expect(Array.from(picker.options).map((option) => option.textContent)).toEqual(['Select card…', 'Platinum Visa 8003'])
  })

  it('still names an inactive linked card and offers it when editing its account', async () => {
    getAccountsMock.mockResolvedValue([cardAccount({ id: 'cc1', name: 'Platinum Visa 8003', isActive: false })])
    render(<InvestmentAccountsPage />)
    const row = (await screen.findByText('Platinum Visa 8003')).closest('tr') as HTMLElement

    fireEvent.click(within(row).getAllByRole('button')[0])
    const picker = (await screen.findByLabelText(/^Credit Card/)) as HTMLSelectElement

    expect(Array.from(picker.options).map((option) => option.textContent)).toEqual(['Select card…', 'Platinum Visa 8003'])
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

  it('shows a warning and keeps the account list usable when accounts fail to load', async () => {
    getAccountsMock.mockRejectedValue(new Error('Unable to load accounts'))
    render(<InvestmentAccountsPage />)

    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())
    expect(screen.getByText(/couldn.t load accounts/i)).toBeInTheDocument()
    const retryButton = screen.getByRole('button', { name: 'Retry loading accounts' })
    expect(retryButton).toHaveTextContent('Retry')
    fireEvent.click(retryButton)
    await waitFor(() => expect(getAccountsMock).toHaveBeenCalledTimes(2))
  })

  it('[AC P60-F09-pots-template-and-reserva-migration-03] shows the dash, card name, or Pots account total per account source', async () => {
    render(<InvestmentAccountsPage />)

    await waitFor(() => expect(screen.getByText('ChaseSave')).toBeInTheDocument())
    const rows = screen.getAllByRole('row')
    const chaseSaveRow = rows.find((r) => r.textContent?.includes('ChaseSave'))
    const platinumRow = rows.find((r) => r.textContent?.includes('PlatinumVisa8003'))
    const reservasRow = rows.find((r) => r.textContent?.includes('Reservas pessoais'))

    expect(chaseSaveRow?.textContent).toContain('—')
    expect(platinumRow?.textContent).toContain('Platinum Visa 8003')
    expect(reservasRow?.textContent).toContain('Reserva total')
  })

  it('creates an investment account through the Create Investment Account dialog', async () => {
    createInvestmentAccountMock.mockResolvedValue({
      id: 'a4',
      name: 'Monzo Pot',
      isActive: true,
      isLiability: false,
      hasNonZeroInvestmentSnapshot: false,
      source: 'None',
      creditCardId: null,
      potsAccountId: null,
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
        source: 'None',
        creditCardId: null,
        potsAccountId: null,
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
      source: 'None',
      creditCardId: null,
      potsAccountId: null,
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
        source: 'None',
        creditCardId: null,
        potsAccountId: null,
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
