import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CreditCardsPage from '../CreditCardsPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CreditCardDto } from '../../api/types'

const { getCreditCardsMock, createCreditCardMock, updateCreditCardMock, deleteCreditCardMock } = vi.hoisted(() => ({
  getCreditCardsMock: vi.fn<FinancialApiClient['getCreditCards']>(),
  createCreditCardMock: vi.fn<FinancialApiClient['createCreditCard']>(),
  updateCreditCardMock: vi.fn<FinancialApiClient['updateCreditCard']>(),
  deleteCreditCardMock: vi.fn<FinancialApiClient['deleteCreditCard']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCreditCards: getCreditCardsMock,
    createCreditCard: createCreditCardMock,
    updateCreditCard: updateCreditCardMock,
    deleteCreditCard: deleteCreditCardMock,
  } as Partial<FinancialApiClient>,
}))

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: '2026-09-05', latestInvoiceDate: null, hasReferences: true },
  { id: 'card-nubank', name: 'Nubank', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
]

describe('CreditCardsPage', () => {
  beforeEach(() => {
    getCreditCardsMock.mockReset()
    createCreditCardMock.mockReset()
    updateCreditCardMock.mockReset()
    deleteCreditCardMock.mockReset()
    getCreditCardsMock.mockResolvedValue(CREDIT_CARDS)
  })

  it('renders the credit card list', async () => {
    render(<CreditCardsPage />)

    await waitFor(() => expect(screen.getByText('BaAmex')).toBeInTheDocument())
    expect(screen.getByText('Nubank')).toBeInTheDocument()
    expect(screen.getByText('2026-09-05')).toBeInTheDocument()
  })

  it('shows the empty state when there are no credit cards', async () => {
    getCreditCardsMock.mockResolvedValue([])
    render(<CreditCardsPage />)

    expect(await screen.findByText('No credit cards yet — create one to get started.')).toBeInTheDocument()
  })

  it('shows an error state with retry on load failure', async () => {
    getCreditCardsMock.mockRejectedValue(new Error('Network down'))
    render(<CreditCardsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Network down')
  })

  it('creates a credit card through the Create Credit Card dialog', async () => {
    createCreditCardMock.mockResolvedValue({ id: 'card-chase', name: 'Chase', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false })
    render(<CreditCardsPage />)
    await waitFor(() => expect(screen.getByText('BaAmex')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Create Credit Card' }))
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Chase' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(createCreditCardMock).toHaveBeenCalledWith({ name: 'Chase', isActive: true }))
    await waitFor(() => expect(screen.queryByRole('heading', { name: 'Create Credit Card' })).not.toBeInTheDocument())
  })

  it('edits a credit card through its row action', async () => {
    updateCreditCardMock.mockResolvedValue({ id: 'card-nubank', name: 'Nubank Renamed', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false })
    render(<CreditCardsPage />)
    await waitFor(() => expect(screen.getByText('Nubank')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Edit Nubank' }))
    expect(screen.getByRole('heading', { name: 'Edit Credit Card' })).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Nubank Renamed' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(updateCreditCardMock).toHaveBeenCalledWith('card-nubank', {
        name: 'Nubank Renamed',
        isActive: true,
        nextInvoiceDueDate: null,
      }),
    )
  })

  it('disables delete confirmation when the credit card still has references', async () => {
    render(<CreditCardsPage />)
    await waitFor(() => expect(screen.getByText('BaAmex')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete BaAmex' }))

    expect(screen.getByText(/still referenced by a statement or expense and cannot be deleted/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    expect(confirmButtons[confirmButtons.length - 1]).toBeDisabled()
  })

  it('deletes a credit card with no references', async () => {
    deleteCreditCardMock.mockResolvedValue(undefined)
    render(<CreditCardsPage />)
    await waitFor(() => expect(screen.getByText('Nubank')).toBeInTheDocument())

    fireEvent.click(screen.getByRole('button', { name: 'Delete Nubank' }))

    expect(screen.getByText(/will be permanently removed/)).toBeInTheDocument()
    const confirmButtons = screen.getAllByRole('button', { name: 'Delete' })
    fireEvent.click(confirmButtons[confirmButtons.length - 1])

    await waitFor(() => expect(deleteCreditCardMock).toHaveBeenCalledWith('card-nubank'))
  })
})
