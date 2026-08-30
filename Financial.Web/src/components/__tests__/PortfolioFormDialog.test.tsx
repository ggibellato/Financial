import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import PortfolioFormDialog from '../PortfolioFormDialog'
import type { BrokerDto } from '../../api/types'

const ACTIVE_BROKERS: BrokerDto[] = [
  { name: 'XPI', currency: 'BRL', status: 'Active', portfolioCount: 1 },
  { name: 'Avenue', currency: 'USD', status: 'Active', portfolioCount: 0 },
]

describe('PortfolioFormDialog', () => {
  it('renders in create mode with an empty name and the first active broker selected', () => {
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Portfolio' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText(/^Broker/)).toHaveValue('XPI')
  })

  it('lists only the given active brokers in the picker', () => {
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    const options = screen.getAllByRole('option').map((o) => o.textContent)
    expect(options).toEqual(['XPI', 'Avenue'])
  })

  it('renders in edit mode pre-filled with a read-only broker and editable name', () => {
    render(
      <PortfolioFormDialog
        portfolio={{ name: 'Default', brokerName: 'XPI', brokerStatus: 'Active', assetCount: 2 }}
        activeBrokers={ACTIVE_BROKERS}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Portfolio' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Default')
    expect(screen.getByLabelText(/^Broker/)).toHaveValue('XPI')
    expect(screen.getByLabelText(/^Broker/)).toBeDisabled()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the selected broker and trimmed name', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Broker/), { target: { value: 'Avenue' } })
    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Growth  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Avenue', 'Growth'))
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('Broker "XPI" already has a portfolio named "Default".'))
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Default' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Broker "XPI" already has a portfolio named "Default".')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<PortfolioFormDialog portfolio={null} activeBrokers={ACTIVE_BROKERS} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
