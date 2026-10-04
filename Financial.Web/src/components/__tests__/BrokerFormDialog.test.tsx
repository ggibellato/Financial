import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BrokerFormDialog from '../BrokerFormDialog'

describe('BrokerFormDialog', () => {
  it('renders in create mode with an empty name, the first currency, and Average Cost selected', () => {
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Broker' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByRole('combobox', { name: /^Cost Basis Method/ })).toHaveValue('AverageCost')
  })

  it('offers contextual help explaining Cost Basis Method', () => {
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('button', { name: /Cost Basis Method information/ })).toBeInTheDocument()
  })

  it('renders in edit mode pre-filled with the broker being edited', () => {
    render(
      <BrokerFormDialog
        broker={{ name: 'XPI', currency: 'USD', status: 'Active', portfolioCount: 0, costBasisMethod: 'FIFO' }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Broker' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('XPI')
    expect(screen.getByLabelText(/^Currency/)).toHaveValue('USD')
    expect(screen.getByRole('combobox', { name: /^Cost Basis Method/ })).toHaveValue('FIFO')
  })

  it('submits the trimmed name and selected currency', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  XPI  ' } })
    fireEvent.change(screen.getByLabelText(/^Currency/), { target: { value: 'GBP' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('XPI', 'GBP', 'AverageCost'))
  })

  it('submits a changed cost basis method', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(
      <BrokerFormDialog
        broker={{ name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 0, costBasisMethod: 'AverageCost' }}
        onCancel={vi.fn()}
        onSubmit={onSubmit}
      />,
    )

    fireEvent.change(screen.getByRole('combobox', { name: /^Cost Basis Method/ }), { target: { value: 'SpecificId' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('XPI', 'BRL', 'SpecificId'))
  })
})
