import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import ExpenseForm from '../ExpenseForm'
import type { BankDto, CategoryDto, CreditCardDto } from '../../api/types'

const BANKS: BankDto[] = [
  { id: 'bank-barclays', name: 'Barclays', roundUpEnabled: false, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
  { id: 'bank-trading212', name: 'Trading212', roundUpEnabled: true, openingBalance: 0, openingBalanceDate: '2026-01-01', hasReferences: false },
]

const CATEGORIES: CategoryDto[] = [
  { id: 'category-mercado', name: 'Mercado', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-casa', name: 'Casa', active: true, isInvestment: false, isTithe: false, hasReferences: false },
  { id: 'category-dizimo', name: 'Dizimo', active: true, isInvestment: false, isTithe: true, hasReferences: false },
]

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
  { id: 'card-chase', name: 'ChaseMaster4023', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: null, hasReferences: false },
]

const baseProps = {
  isEditing: false,
  date: '',
  description: '',
  value: '',
  categoryId: 'category-mercado',
  paymentSource: 'bank-barclays',
  creditCardId: '',
  creditCardName: '',
  invoiceDate: '',
  roundUpAmount: '',
  countsAsTithe: true,
  paymentMode: 'bank' as const,
  banks: BANKS,
  categories: CATEGORIES,
  creditCards: CREDIT_CARDS,
  isSettled: false,
  isSaving: false,
  saveError: null,
  saveErrorField: null,
  onFieldChange: vi.fn(),
  onSave: vi.fn(),
  onCancel: vi.fn(),
}

describe('ExpenseForm', () => {
  it('renders the create form with empty date/description/value fields', () => {
    render(<ExpenseForm {...baseProps} />)

    expect(screen.getByText('New Expense')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Date/)).toHaveValue('')
    expect(screen.getByLabelText(/^Description/)).toHaveValue('')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(null)
    expect(screen.getByRole('button', { name: 'Add Expense' })).toBeInTheDocument()
  })

  it('shows the settlement note and hides payment fields when settled', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="bank-trading212"
        creditCardId="card-baamex"
        creditCardName="BaAmex"
      />,
    )

    expect(screen.getByText(/Settled via its card statement/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Payment Source')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Card')).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows the bank picker and no toggle in bank mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="bank" />)
    expect(screen.getByLabelText(/^Payment Source/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/^Card/)).not.toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('shows the card picker and no toggle in card mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" />)
    expect(screen.queryByLabelText(/^Payment Source/)).not.toBeInTheDocument()
    expect(screen.getByLabelText(/^Card/)).toBeInTheDocument()
    expect(screen.queryByRole('radio')).not.toBeInTheDocument()
  })

  it('lists exactly the categories passed in via the categories prop, by name', () => {
    render(<ExpenseForm {...baseProps} />)

    const categorySelect = screen.getByLabelText(/^Category/)
    expect(within(categorySelect).getByRole('option', { name: 'Mercado' })).toBeInTheDocument()
    expect(within(categorySelect).getByRole('option', { name: 'Casa' })).toBeInTheDocument()
    expect(within(categorySelect).getAllByRole('option')).toHaveLength(CATEGORIES.length)
  })

  it('submits the selected category id, not its name', () => {
    const onFieldChange = vi.fn()
    render(<ExpenseForm {...baseProps} onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText(/^Category/), { target: { value: 'category-casa' } })

    expect(onFieldChange).toHaveBeenCalledWith('categoryId', 'category-casa')
  })

  it('lists exactly the cards passed in via the creditCards prop, by name', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" />)

    const cardSelect = screen.getByLabelText(/^Card/)
    expect(within(cardSelect).getByRole('option', { name: 'BaAmex' })).toBeInTheDocument()
    expect(within(cardSelect).getByRole('option', { name: 'ChaseMaster4023' })).toBeInTheDocument()
    expect(within(cardSelect).getAllByRole('option')).toHaveLength(CREDIT_CARDS.length + 1)
  })

  it('submits the selected card id, not its name', () => {
    const onFieldChange = vi.fn()
    render(<ExpenseForm {...baseProps} paymentMode="card" onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText(/^Card/), { target: { value: 'card-chase' } })

    expect(onFieldChange).toHaveBeenCalledWith('creditCardId', 'card-chase')
  })

  it('shows an editable invoice month field pre-filled from the date when card mode is selected', () => {
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" />)

    const invoiceField = screen.getByLabelText('Invoice Month')
    expect(invoiceField).toBeInTheDocument()
    expect(invoiceField).not.toBeDisabled()
    expect(invoiceField).toHaveValue('2026-07')
  })

  it('persists a changed invoice month while unpaid', () => {
    const onFieldChange = vi.fn()
    render(<ExpenseForm {...baseProps} paymentMode="card" date="2026-07-15" onFieldChange={onFieldChange} />)

    fireEvent.change(screen.getByLabelText('Invoice Month'), { target: { value: '2026-08' } })

    expect(onFieldChange).toHaveBeenCalledWith('invoiceDate', '2026-08')
  })

  it('shows the invoice month field disabled once settled', () => {
    render(
      <ExpenseForm
        {...baseProps}
        isEditing
        isSettled
        paymentSource="bank-trading212"
        creditCardId="card-baamex"
        creditCardName="BaAmex"
        invoiceDate="2026-07"
      />,
    )

    const invoiceField = screen.getByLabelText('Invoice Month')
    expect(invoiceField).toBeInTheDocument()
    expect(invoiceField).toBeDisabled()
    expect(invoiceField).toHaveValue('2026-07')
  })

  it("defaults the invoice month to the selected card's latest invoice month when it's ahead of the date", () => {
    const cardsWithFutureInvoice: CreditCardDto[] = [
      { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: '2026-09-01', hasReferences: false },
    ]
    render(
      <ExpenseForm
        {...baseProps}
        paymentMode="card"
        date="2026-07-15"
        creditCardId="card-baamex"
        creditCards={cardsWithFutureInvoice}
      />,
    )

    expect(screen.getByLabelText('Invoice Month')).toHaveValue('2026-09')
  })

  it("falls back to the date-derived default when the selected card's latest invoice month is not ahead of the date", () => {
    const cardsWithPastInvoice: CreditCardDto[] = [
      { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: null, latestInvoiceDate: '2026-06-01', hasReferences: false },
    ]
    render(
      <ExpenseForm
        {...baseProps}
        paymentMode="card"
        date="2026-07-15"
        creditCardId="card-baamex"
        creditCards={cardsWithPastInvoice}
      />,
    )

    expect(screen.getByLabelText('Invoice Month')).toHaveValue('2026-07')
  })

  it('hides the invoice month field in bank mode', () => {
    render(<ExpenseForm {...baseProps} paymentMode="bank" />)

    expect(screen.queryByLabelText('Invoice Month')).not.toBeInTheDocument()
  })

  it('shows the round-up field only for a round-up-enabled bank in bank mode', () => {
    const { rerender } = render(<ExpenseForm {...baseProps} paymentMode="bank" paymentSource="bank-barclays" />)
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="bank" paymentSource="bank-trading212" />)
    expect(screen.getByLabelText('Round-Up')).toBeInTheDocument()

    rerender(<ExpenseForm {...baseProps} paymentMode="card" paymentSource="bank-trading212" />)
    expect(screen.queryByLabelText('Round-Up')).not.toBeInTheDocument()
  })

  it('hides the counts-toward-tithe checkbox for a non-tithe category', () => {
    render(<ExpenseForm {...baseProps} categoryId="category-mercado" />)

    expect(screen.queryByLabelText('Counts toward tithe')).not.toBeInTheDocument()
  })

  it('shows the counts-toward-tithe checkbox, checked by default, for the tithe category', () => {
    render(<ExpenseForm {...baseProps} categoryId="category-dizimo" countsAsTithe />)

    expect(screen.getByLabelText('Counts toward tithe')).toBeChecked()
  })

  it('reports unchecking the counts-toward-tithe checkbox', () => {
    const onFieldChange = vi.fn()
    render(
      <ExpenseForm {...baseProps} categoryId="category-dizimo" countsAsTithe onFieldChange={onFieldChange} />,
    )

    fireEvent.click(screen.getByLabelText('Counts toward tithe'))

    expect(onFieldChange).toHaveBeenCalledWith('countsAsTithe', 'false')
  })

  it('calls onSave and onCancel', () => {
    const onSave = vi.fn()
    const onCancel = vi.fn()
    render(<ExpenseForm {...baseProps} onSave={onSave} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))
    expect(onSave).toHaveBeenCalled()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalled()
  })
})
