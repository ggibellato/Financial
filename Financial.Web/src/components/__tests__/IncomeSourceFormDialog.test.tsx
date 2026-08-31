import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import IncomeSourceFormDialog from '../IncomeSourceFormDialog'

describe('IncomeSourceFormDialog', () => {
  it('renders in create mode with an empty name, Salary group, active on, and auto-split off', () => {
    render(<IncomeSourceFormDialog incomeSource={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Income Source' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Group')).toHaveValue('Salary')
    expect(screen.getByLabelText('Active')).toBeChecked()
    expect(screen.getByLabelText('Auto-split to reserve')).not.toBeChecked()
  })

  it('renders in edit mode pre-filled with the income source being edited', () => {
    render(
      <IncomeSourceFormDialog
        incomeSource={{
          id: 's1',
          name: 'Gleison',
          isActive: false,
          group: 'NonReportable',
          autoSplitToReserve: true,
          hasReferences: false,
        }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Income Source' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Gleison')
    expect(screen.getByLabelText('Group')).toHaveValue('NonReportable')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByLabelText('Auto-split to reserve')).toBeChecked()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<IncomeSourceFormDialog incomeSource={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the trimmed name, selected group, and toggled flags', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<IncomeSourceFormDialog incomeSource={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Freelance  ' } })
    fireEvent.change(screen.getByLabelText('Group'), { target: { value: 'NonReportable' } })
    fireEvent.click(screen.getByLabelText('Auto-split to reserve'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Freelance', 'NonReportable', true, true))
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('An income source named "Gleison" already exists.'))
    render(<IncomeSourceFormDialog incomeSource={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Gleison' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('An income source named "Gleison" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<IncomeSourceFormDialog incomeSource={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
