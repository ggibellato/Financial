import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import RecurringBillFormDialog from '../RecurringBillFormDialog'

describe('RecurringBillFormDialog', () => {
  it('renders in create mode with empty fields and no Status field', () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'New recurring bill' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Due day/)).toHaveValue(null)
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

    expect(screen.getByRole('heading', { name: 'Edit recurring bill' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Due day/)).toHaveValue(10)
    expect(screen.getByLabelText(/^Description/)).toHaveValue('INSS')
    expect(screen.getByLabelText(/^Value/)).toHaveValue(850)
    expect(screen.getByLabelText('Area')).toHaveValue('Brasil')
    expect(screen.getByLabelText('NIT number')).toHaveValue('12345678901')
    expect(screen.getByLabelText('Status')).toHaveValue('Scheduled')
  })

  it('shows the Description message after Save when it is blank', async () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Due day/), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '100' } })

    expect(screen.queryByText('Description is required.')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Add recurring bill' }))

    expect(await screen.findByText('Description is required.')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByLabelText(/^Description/)).toHaveFocus())
  })

  it('says Value is required when it is blank, and a number when it is not one', async () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)
    fireEvent.change(screen.getByLabelText(/^Due day/), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Rent' } })

    fireEvent.click(screen.getByRole('button', { name: 'Add recurring bill' }))

    expect(await screen.findByText('Value is required.')).toBeInTheDocument()
    expect(screen.queryByText('Value must be a number.')).not.toBeInTheDocument()
  })

  it('lists every invalid field in an error summary that takes focus when several fail on Save', async () => {
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Add recurring bill' }))

    const summary = await screen.findByRole('group', { name: 'Fix 3 fields before saving' })
    await waitFor(() => expect(summary).toHaveFocus())
    expect(within(summary).getAllByRole('button').map((link) => link.textContent)).toEqual([
      'Due day must be between 1 and 31.',
      'Description is required.',
      'Value is required.',
    ])
    fireEvent.click(within(summary).getByRole('button', { name: 'Description is required.' }))
    expect(screen.getByLabelText(/^Description/)).toHaveFocus()
  })

  it('submits the create form with the entered values', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<RecurringBillFormDialog recurringBill={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Due day/), { target: { value: '10' } })
    fireEvent.change(screen.getByLabelText(/^Description/), { target: { value: 'Rent' } })
    fireEvent.change(screen.getByLabelText(/^Value/), { target: { value: '1500' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add recurring bill' }))

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
})
