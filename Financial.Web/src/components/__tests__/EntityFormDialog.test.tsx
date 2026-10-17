import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import EntityFormDialog from '../EntityFormDialog'

interface RenderOptions {
  error?: string | null
  isSaving?: boolean
  canSave?: boolean
  onSave?: () => void
  onCancel?: () => void
}

const renderDialog = ({
  error = null,
  isSaving = false,
  canSave = true,
  onSave = vi.fn(),
  onCancel = vi.fn(),
}: RenderOptions = {}) =>
  render(
    <EntityFormDialog
      title="Create Bank"
      error={error}
      isSaving={isSaving}
      canSave={canSave}
      onSave={onSave}
      onCancel={onCancel}
    >
      <label htmlFor="bank-name">Name</label>
      <input id="bank-name" />
    </EntityFormDialog>,
  )

describe('EntityFormDialog', () => {
  it('renders the title as the dialog heading with its fields inside the dialog', () => {
    renderDialog()

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Create Bank' })).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toBeInTheDocument()
  })

  it('calls onSave when Save is clicked', () => {
    const onSave = vi.fn()
    renderDialog({ onSave })

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(onSave).toHaveBeenCalledTimes(1)
  })

  it('disables Save when the form cannot be saved but keeps Cancel enabled', () => {
    renderDialog({ canSave: false })

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).not.toBeDisabled()
  })

  it('disables Cancel while saving', () => {
    renderDialog({ isSaving: true, canSave: false })

    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel })

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('calls onCancel when the dialog is dismissed with Escape', () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel })

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onCancel).toHaveBeenCalled()
  })

  it('shows no error message bar when there is no error', () => {
    renderDialog()

    expect(screen.queryByText('A bank named "Barclays" already exists.')).not.toBeInTheDocument()
  })

  it('shows the save error inside the dialog', () => {
    renderDialog({ error: 'A bank named "Barclays" already exists.' })

    expect(screen.getByRole('dialog')).toHaveTextContent('A bank named "Barclays" already exists.')
  })
})
