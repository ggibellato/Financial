import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import CategoryFormDialog from '../CategoryFormDialog'

describe('CategoryFormDialog', () => {
  it('renders in create mode with an empty name, active on, and both classification flags off', () => {
    render(<CategoryFormDialog category={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Create Category' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('')
    expect(screen.getByLabelText('Active')).toBeChecked()
    expect(screen.getByLabelText('Investment')).not.toBeChecked()
    expect(screen.getByLabelText('Tithe')).not.toBeChecked()
  })

  it('renders in edit mode pre-filled with the category being edited', () => {
    render(
      <CategoryFormDialog
        category={{
          id: 'c1',
          name: 'Mercado',
          active: false,
          isInvestment: true,
          isTithe: true,
          hasReferences: false,
        }}
        onCancel={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('heading', { name: 'Edit Category' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Name/)).toHaveValue('Mercado')
    expect(screen.getByLabelText('Active')).not.toBeChecked()
    expect(screen.getByLabelText('Investment')).toBeChecked()
    expect(screen.getByLabelText('Tithe')).toBeChecked()
  })

  it('disables Save and shows a validation message when the name is blank', () => {
    render(<CategoryFormDialog category={null} onCancel={vi.fn()} onSubmit={vi.fn()} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '   ' } })

    expect(screen.getByText('Name is required.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('submits the trimmed name and the toggled flags', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CategoryFormDialog category={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Lazer  ' } })
    fireEvent.click(screen.getByLabelText('Investment'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Lazer', true, true, false))
  })

  it('shows a server error and re-enables Save when the submit rejects', async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('A category named "Mercado" already exists.'))
    render(<CategoryFormDialog category={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: 'Mercado' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('A category named "Mercado" already exists.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Save' })).not.toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    render(<CategoryFormDialog category={null} onCancel={onCancel} onSubmit={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
  })
})
