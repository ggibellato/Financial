import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import RecurringBillFormDialog from '../RecurringBillFormDialog'

describe('RecurringBillFormDialog', () => {
  it('renders in create mode with empty fields and no Status field', () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Recurring Bill' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Due Day/)).toHaveValue(null)
    expect(screen.getByLabelText(/^Description/)).toHaveValue('')
    expect(screen.queryByLabelText('Status')).not.toBeInTheDocument()
  })

  it('renders in edit mode pre-filled with the bill being edited, including Status', () => {
    render(
      <RecurringBillFormDialog
        recurringBill={{
          id: 'b1',
          dueDay: 10,
          description: 'INSS',
          value: 850,
          area: 'Brasil',
          note: 'Direct debit',
          nitNumber: '12345678901',
          minimumWageValue: 1621,
          status: 'Scheduled',
        }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Recurring Bill' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Due Day/)).toHaveValue(10)
    expect(screen.getByLabelText(/^Description/)).toHaveValue('INSS')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(850)
    expect(screen.getByLabelText('Area')).toHaveValue('Brasil')
    expect(screen.getByLabelText('NIT Number')).toHaveValue('12345678901')
    expect(screen.getByLabelText('Status')).toHaveValue('Scheduled')
  })

  it('disables Save and shows a validation message when Due Day is out of range', () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Due Day/), { target: { value: '32' } })

    expect(screen.getByText('Due day must be between 1 and 31.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('disables Save and shows a validation message when Description is blank', () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Due Day/), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '100' } })

    expect(screen.getByText('Description is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the create form with the entered values', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Due Day/), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Rent' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '1500' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith({
        dueDay: 10,
        description: 'Rent',
        value: 1500,
        area: 'Brasil',
        note: '',
        nitNumber: null,
        minimumWageValue: null,
        status: 'Unset',
      }),
    )
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('Due day must be between 1 and 31.'))
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Due Day/), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Rent' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '1500' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Due day must be between 1 and 31.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<RecurringBillFormDialog recurringBill={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
