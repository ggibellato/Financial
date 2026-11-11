import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BrokerFormDialog from '../BrokerFormDialog'
import { duplicateNameRejection, expectDuplicateUnderName, saveFromTheSaveButton } from '../../test-utils/duplicateName'

describe('BrokerFormDialog', () => {
  it('renders in create mode with an empty name, the first currency, and Average cost selected', () => {
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'New broker' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByRole('combobox', { name: /^Cost basis method/ })).toHaveValue('AverageCost')
  })

  it('offers contextual help explaining Cost basis method', () => {
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('button', { name: /Cost basis method information/ })).toBeInTheDocument()
  })

  it('renders in edit mode pre-filled with the broker being edited', () => {
    render(
      <BrokerFormDialog
        broker={{ name: 'XPI', currency: 'USD', status: 'Active', portfolioCount: 0, costBasisMethod: 'FIFO' }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit broker' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('XPI')
    expect(screen.getByLabelText(/^Currency/)).toHaveValue('USD')
    expect(screen.getByRole('combobox', { name: /^Cost basis method/ })).toHaveValue('FIFO')
  })

  it('submits the trimmed name and selected currency', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  XPI  ' } })
    fireEvent.change(screen.getByLabelText(/^Currency/), { target: { value: 'GBP' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add broker' }))

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

    fireEvent.change(screen.getByRole('combobox', { name: /^Cost basis method/ }), { target: { value: 'SpecificId' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('XPI', 'BRL', 'SpecificId'))
  })

  it('shows a duplicate name under Name and moves focus there', async () => {
    const message = 'A broker named "XPI" already exists.'
    const onSubmit = vi.fn().mockRejectedValue(duplicateNameRejection(message))
    render(<BrokerFormDialog broker={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'XPI' } })
    saveFromTheSaveButton('Add broker')

    await expectDuplicateUnderName(message)
  })
})
