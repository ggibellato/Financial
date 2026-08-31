import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ReserveBucketFormDialog from '../ReserveBucketFormDialog'

describe('ReserveBucketFormDialog', () => {
  it('renders in create mode with an empty name, empty split, active on', () => {
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Reserve Bucket' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText(/^Split Percentage/)).toHaveValue(null)
    expect(screen.getByLabelText('Active')).toBeChecked()
  })

  it('renders in edit mode pre-filled with the bucket being edited', () => {
    render(
      <ReserveBucketFormDialog
        reserveBucket={{ id: 'b1', name: 'Investimento', isActive: false, splitPercentage: 33.33, warning: null }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Reserve Bucket' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Investimento')
    expect(screen.getByLabelText(/^Split Percentage/)).toHaveValue(33.33)
    expect(screen.getByLabelText('Active')).not.toBeChecked()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Split Percentage/), { target: { value: '50' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('disables Save and shows a validation message when the split percentage is out of range', () => {
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Ferias' } })
    fireEvent.change(screen.getByLabelText(/^Split Percentage/), { target: { value: '101' } })

    expect(screen.getByText('Split percentage must be between 0 and 100.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the trimmed name, parsed split, and active flag, and closes with no warning', async () => {
    const onSubmit = vi.fn().mockResolvedValue({ id: 'b1', name: 'Ferias', isActive: true, splitPercentage: 20, warning: null })
    const onCancel = vi.fn()
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={onCancel} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Ferias  ' } })
    fireEvent.change(screen.getByLabelText(/^Split Percentage/), { target: { value: '20' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Ferias', 20, true))
    await waitFor(() => expect(onCancel).toHaveBeenCalled())
  })

  it('shows the returned warning inline without closing the dialog', async () => {
    const onSubmit = vi.fn().mockResolvedValue({
      id: 'b1',
      name: 'Ferias',
      isActive: true,
      splitPercentage: 20,
      warning: 'Active buckets currently sum to 110% — review your split percentages',
    })
    const onCancel = vi.fn()
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={onCancel} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Ferias' } })
    fireEvent.change(screen.getByLabelText(/^Split Percentage/), { target: { value: '20' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Active buckets currently sum to 110% — review your split percentages')).toBeInTheDocument()
    expect(onCancel).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Close' })).toBeInTheDocument()
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('A reserve bucket named "Ferias" already exists.'))
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Ferias' } })
    fireEvent.change(screen.getByLabelText(/^Split Percentage/), { target: { value: '20' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('A reserve bucket named "Ferias" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<ReserveBucketFormDialog reserveBucket={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
