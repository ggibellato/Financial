import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BrokerFormDialog from '../BrokerFormDialog'

describe('BrokerFormDialog', () => {
  it('renders in create mode with an empty name and the first currency selected', () => {
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Broker' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
  })

  it('renders in edit mode pre-filled with the broker being edited', () => {
    render(
      <BrokerFormDialog
        broker={{ name: 'XPI', currency: 'USD', status: 'Active', portfolioCount: 0 }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Broker' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('XPI')
    expect(screen.getByLabelText(/^Currency/)).toHaveValue('USD')
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the trimmed name and selected currency', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  XPI  ' } })
    fireEvent.change(screen.getByLabelText(/^Currency/), { target: { value: 'GBP' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('XPI', 'GBP'))
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('A broker named "XPI" already exists.'))
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'XPI' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('A broker named "XPI" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<BrokerFormDialog broker={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
