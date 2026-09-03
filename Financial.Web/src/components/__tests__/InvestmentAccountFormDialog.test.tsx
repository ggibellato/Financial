import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import InvestmentAccountFormDialog from '../InvestmentAccountFormDialog'

describe('InvestmentAccountFormDialog', () => {
  it('renders in create mode with an empty name, active on, liability off', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Investment Account' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Active')).toBeChecked()
    expect(screen.getByLabelText('Liability')).not.toBeChecked()
  })

  it('renders in edit mode pre-filled with the account being edited', () => {
    render(
      <InvestmentAccountFormDialog
        investmentAccount={{
          id: 'a1',
          name: 'ChaseSave',
          isActive: false,
          isLiability: true,
          hasNonZeroInvestmentSnapshot: false,
        }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Investment Account' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('ChaseSave')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByLabelText('Liability')).toBeChecked()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<InvestmentAccountFormDialog investmentAccount={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the trimmed name and toggled flags', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<InvestmentAccountFormDialog investmentAccount={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Monzo Pot  ' } })
    fireEvent.click(screen.getByLabelText('Liability'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Monzo Pot', true, true))
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('An investment account named "ChaseSave" already exists.'))
    render(<InvestmentAccountFormDialog investmentAccount={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'ChaseSave' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('An investment account named "ChaseSave" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<InvestmentAccountFormDialog investmentAccount={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
