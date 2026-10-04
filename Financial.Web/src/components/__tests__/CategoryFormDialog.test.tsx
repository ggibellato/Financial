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

  it('submits the trimmed name and the toggled flags', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined)
    render(<CategoryFormDialog category={null} onCancel={vi.fn()} onSubmit={onSubmit} />)

    fireEvent.change(screen.getByLabelText(/^Name/), { target: { value: '  Lazer  ' } })
    fireEvent.click(screen.getByLabelText('Investment'))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith('Lazer', true, true, false))
  })
})
