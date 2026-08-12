import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import CreditCardsGrid from '../CreditCardsGrid'
import type { CreditCardDto } from '../../api/types'

const CREDIT_CARDS: CreditCardDto[] = [
  { id: 'card-baamex', name: 'BaAmex', isActive: true, nextInvoiceDueDate: '2026-09-05' },
  { id: 'card-paypal', name: 'PaypalCredit', isActive: false, nextInvoiceDueDate: null },
]

describe('CreditCardsGrid', () => {
  it('renders a row per card with its name, due date, and active state', () => {
    render(
      <CreditCardsGrid creditCards={CREDIT_CARDS} updatingCardId={null} updateError={null} onUpdate={vi.fn()} />,
    )

    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
    expect(screen.getByLabelText('Next invoice due date for BaAmex')).toHaveValue('2026-09-05')
    expect(screen.getByLabelText('Active for BaAmex')).toBeChecked()
    expect(screen.getByLabelText('Next invoice due date for PaypalCredit')).toHaveValue('')
    expect(screen.getByLabelText('Active for PaypalCredit')).not.toBeChecked()
  })

  it('changing the due date calls onUpdate with the new date and the current active flag', () => {
    const onUpdate = vi.fn()
    render(
      <CreditCardsGrid creditCards={CREDIT_CARDS} updatingCardId={null} updateError={null} onUpdate={onUpdate} />,
    )

    fireEvent.change(screen.getByLabelText('Next invoice due date for BaAmex'), { target: { value: '2026-10-01' } })

    expect(onUpdate).toHaveBeenCalledWith('card-baamex', { nextInvoiceDueDate: '2026-10-01', isActive: true })
  })

  it('clearing the due date sends null', () => {
    const onUpdate = vi.fn()
    render(
      <CreditCardsGrid creditCards={CREDIT_CARDS} updatingCardId={null} updateError={null} onUpdate={onUpdate} />,
    )

    fireEvent.change(screen.getByLabelText('Next invoice due date for BaAmex'), { target: { value: '' } })

    expect(onUpdate).toHaveBeenCalledWith('card-baamex', { nextInvoiceDueDate: null, isActive: true })
  })

  it('toggling active calls onUpdate with the flipped flag and the current due date', () => {
    const onUpdate = vi.fn()
    render(
      <CreditCardsGrid creditCards={CREDIT_CARDS} updatingCardId={null} updateError={null} onUpdate={onUpdate} />,
    )

    fireEvent.click(screen.getByLabelText('Active for BaAmex'))

    expect(onUpdate).toHaveBeenCalledWith('card-baamex', { nextInvoiceDueDate: '2026-09-05', isActive: false })
  })

  it('disables the row being updated', () => {
    render(
      <CreditCardsGrid creditCards={CREDIT_CARDS} updatingCardId="card-baamex" updateError={null} onUpdate={vi.fn()} />,
    )

    expect(screen.getByLabelText('Next invoice due date for BaAmex')).toBeDisabled()
    expect(screen.getByLabelText('Active for BaAmex')).toBeDisabled()
    expect(screen.getByLabelText('Next invoice due date for PaypalCredit')).not.toBeDisabled()
  })

  it('shows an update error when present', () => {
    render(
      <CreditCardsGrid creditCards={CREDIT_CARDS} updatingCardId={null} updateError="Credit card was not found." onUpdate={vi.fn()} />,
    )

    expect(screen.getByText('Credit card was not found.')).toBeInTheDocument()
  })
})
