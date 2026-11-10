import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import EntityFormDialog from '../EntityFormDialog'

interface RenderOptions {
  error?: string | null
  isSaving?: boolean
  canSave?: boolean
  saveLabel?: string
  onSave?: () => void
  onCancel?: () => void
  autoFocus?: boolean
}

const dialog = ({
  error = null,
  isSaving = false,
  canSave = true,
  saveLabel,
  onSave = vi.fn(),
  onCancel = vi.fn(),
  autoFocus = false,
}: RenderOptions = {}) => (
  <EntityFormDialog
    title="Create Bank"
    error={error}
    isSaving={isSaving}
    canSave={canSave}
    saveLabel={saveLabel}
    onSave={onSave}
    onCancel={onCancel}
  >
    <label htmlFor="bank-name">Name</label>
    <input id="bank-name" autoFocus={autoFocus} />
  </EntityFormDialog>
)

const renderDialog = (options: RenderOptions = {}) => render(dialog(options))

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

  it.each([
    { when: 'it closes', autoFocus: false },
    { when: 'a field took focus as it opened', autoFocus: true },
  ])('returns focus to the control that opened it when $when', ({ autoFocus }) => {
    const opener = document.createElement('button')
    document.body.appendChild(opener)
    opener.focus()
    const { unmount } = renderDialog({ autoFocus })

    unmount()

    expect(opener).toHaveFocus()
    opener.remove()
  })

  it('names the save button by the given label', () => {
    renderDialog({ saveLabel: 'Add account' })

    expect(screen.getByRole('button', { name: 'Add account' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument()
  })

  it('shows progress on the save button while saving', () => {
    renderDialog({ isSaving: true, canSave: false })

    const save = screen.getByRole('button', { name: 'Save' })
    expect(save).toBeDisabled()
    expect(save.querySelector('.fui-Spinner')).toBeInTheDocument()
  })

  it('shows the save error inside the dialog', () => {
    renderDialog({ error: 'A bank named "Barclays" already exists.' })

    expect(screen.getByRole('dialog')).toHaveTextContent('A bank named "Barclays" already exists.')
  })

  it('saves when Enter is pressed in a field', async () => {
    const onSave = vi.fn()
    renderDialog({ onSave })

    await userEvent.type(screen.getByLabelText('Name'), 'Barclays{Enter}')

    expect(onSave).toHaveBeenCalledTimes(1)
  })

  it('does not save on Enter while the form cannot be saved', async () => {
    const onSave = vi.fn()
    renderDialog({ onSave, canSave: false })

    await userEvent.type(screen.getByLabelText('Name'), '{Enter}')

    expect(onSave).not.toHaveBeenCalled()
  })

  it('makes Save the submit button and leaves Cancel as a plain button', () => {
    renderDialog()

    expect(screen.getByRole('button', { name: 'Save' })).toHaveAttribute('type', 'submit')
    expect(screen.getByRole('button', { name: 'Cancel' })).toHaveAttribute('type', 'button')
  })

  it('moves focus to the error summary when a save fails', async () => {
    const { rerender } = renderDialog()

    rerender(dialog({ error: 'The bank could not be saved.' }))

    await waitFor(() => expect(screen.getByRole('alert')).toHaveFocus())
    expect(screen.getByRole('alert')).toHaveTextContent('The bank could not be saved.')
  })
})
