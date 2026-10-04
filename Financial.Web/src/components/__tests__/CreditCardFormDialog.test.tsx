import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import CreditCardFormDialog from '../CreditCardFormDialog'

describe('CreditCardFormDialog', () => {
  it('renders in create mode with an empty name, active on, and no due date', () => {
    render(<CreditCardFormDialog creditCard={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Credit Card' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Active')).toBeChecked()
    expect(screen.getByLabelText('Next Invoice Due Date')).toHaveValue('')
  })

  it('renders in edit mode pre-filled with the credit card being edited', () => {
    render(
      <CreditCardFormDialog
        creditCard={{ id: 'c1', name: 'BaAmex', isActive: false, nextInvoiceDueDate: '2026-09-05', latestInvoiceDate: null, hasReferences: true }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Credit Card' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('BaAmex')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByLabelText('Next Invoice Due Date')).toHaveValue('2026-09-05')
  })

  it('submits the trimmed name, active flag, and due date', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CreditCardFormDialog creditCard={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Nubank  ' } })
    fireEvent.change(screen.getByLabelText('Next Invoice Due Date'), { target: { value: '2026-10-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Nubank', true, '2026-10-01'))
  })

  it('submits null when the due date is left blank', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CreditCardFormDialog creditCard={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Nubank' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Nubank', true, null))
  })
})
