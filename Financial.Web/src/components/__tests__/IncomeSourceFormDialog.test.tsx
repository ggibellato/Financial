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

  it('submits the trimmed name, selected group, and toggled flags', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<IncomeSourceFormDialog incomeSource={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Freelance  ' } })
    fireEvent.change(screen.getByLabelText('Group'), { target: { value: 'NonReportable' } })
    fireEvent.click(screen.getByLabelText('Auto-split to reserve'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Freelance', 'NonReportable', true, true))
  })
})
