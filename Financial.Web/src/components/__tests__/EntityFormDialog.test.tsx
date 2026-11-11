import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import EntityFormDialog from '../EntityFormDialog'
import { getErrorAlert } from '../../test-utils/errorAlert'

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

  it('stays open on Escape while saving so a failed save is not lost', () => {
    const onCancel = vi.fn()
    renderDialog({ onCancel, isSaving: true, canSave: false })

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onCancel).not.toHaveBeenCalled()
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

  it('keeps Saving… focused and inert, with progress, while saving', () => {
    const { rerender } = renderDialog()
    screen.getByRole('button', { name: 'Save' }).focus()

    rerender(dialog({ isSaving: true, canSave: false }))

    const save = screen.getByRole('button', { name: 'Saving…' })
    expect(save).toHaveAttribute('aria-disabled', 'true')
    expect(save).toHaveFocus()
    expect(save.querySelector('.fui-Spinner')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('moves focus to Save when Enter in a field submits', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    renderDialog({ onSave })

    await user.click(screen.getByLabelText('Name'))
    await user.keyboard('{Enter}')

    expect(onSave).toHaveBeenCalledOnce()
    expect(screen.getByRole('button', { name: 'Save' })).toHaveFocus()
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

    await waitFor(() => expect(getErrorAlert()).toHaveFocus())
    expect(getErrorAlert()).toHaveTextContent('The bank could not be saved.')
  })

  it('focuses the first editable field in visual order as it opens, skipping read-only ones', async () => {
    render(
      <EntityFormDialog title="Edit asset" error={null} isSaving={false} canSave onSave={vi.fn()} onCancel={vi.fn()}>
        <label htmlFor="asset-broker">Broker</label>
        <input id="asset-broker" readOnly value="XPI" />
        <label htmlFor="asset-class">Class</label>
        <select id="asset-class">
          <option>Equity</option>
        </select>
        <label htmlFor="asset-name">Name</label>
        <input id="asset-name" />
      </EntityFormDialog>,
    )

    await waitFor(() => expect(screen.getByLabelText('Class')).toHaveFocus())
  })

  it('disables every field while saving and enables them again afterwards', () => {
    const { rerender } = renderDialog()
    expect(screen.getByLabelText('Name')).toBeEnabled()

    rerender(dialog({ isSaving: true, canSave: false }))
    expect(screen.getByLabelText('Name')).toBeDisabled()

    rerender(dialog())
    expect(screen.getByLabelText('Name')).toBeEnabled()
  })

  it('asks before discarding changes on Escape, and stays open on Keep editing', async () => {
    const onCancel = vi.fn()
    render(
      <EntityFormDialog title="New bank" error={null} isSaving={false} isDirty onSave={vi.fn()} onCancel={onCancel}>
        <label htmlFor="bank-name">Name</label>
        <input id="bank-name" />
      </EntityFormDialog>,
    )

    fireEvent.keyDown(screen.getByRole('dialog', { name: 'New bank' }), { key: 'Escape' })

    const prompt = await screen.findByRole('dialog', { name: 'Discard unsaved changes?', hidden: true })
    fireEvent.click(within(prompt).getByRole('button', { name: 'Keep editing', hidden: true }))
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Discard unsaved changes?', hidden: true })).not.toBeInTheDocument())
    expect(onCancel).not.toHaveBeenCalled()
    expect(screen.getByRole('dialog', { name: 'New bank', hidden: true })).toBeInTheDocument()
  })

  it('moves focus to the first invalid field after a Save the dialog rejects', async () => {
    render(
      <EntityFormDialog title="New bank" error={null} isSaving={false} onSave={vi.fn()} onCancel={vi.fn()}>
        <label htmlFor="bank-name">Name</label>
        <input id="bank-name" />
        <label htmlFor="bank-code">Code</label>
        <input id="bank-code" aria-invalid="true" />
      </EntityFormDialog>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(screen.getByLabelText('Code')).toHaveFocus())
    expect(screen.queryByText(/before saving/)).not.toBeInTheDocument()
  })

  it('focuses an error summary linking each invalid field when several fail on Save', async () => {
    render(
      <EntityFormDialog title="New bank" error={null} isSaving={false} onSave={vi.fn()} onCancel={vi.fn()}>
        <label htmlFor="bank-name">Name</label>
        <input id="bank-name" aria-invalid="true" aria-describedby="name-error" />
        <span id="name-error">Name is required.</span>
        <div role="group" aria-label="Pot Savings">
          <label htmlFor="bank-code">Code</label>
          <input id="bank-code" aria-invalid="true" aria-describedby="code-error" />
          <span id="code-error">Code must be 3 digits.</span>
        </div>
      </EntityFormDialog>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    const summary = await screen.findByRole('group', { name: 'Fix 2 fields before saving' })
    await waitFor(() => expect(summary).toHaveFocus())
    expect(summary).not.toHaveAttribute('role', 'alert')
    fireEvent.click(within(summary).getByRole('button', { name: 'Pot Savings: Code must be 3 digits.' }))
    expect(screen.getByLabelText('Code')).toHaveFocus()
  })

  it('drops the error summary once a later Save finds the fields valid', async () => {
    const invalidDialog = (invalid: boolean) => (
      <EntityFormDialog title="New bank" error={null} isSaving={false} onSave={vi.fn()} onCancel={vi.fn()}>
        <input aria-label="Name" id="bank-name" aria-invalid={invalid} />
        <input aria-label="Code" id="bank-code" aria-invalid={invalid} />
      </EntityFormDialog>
    )
    const { rerender } = render(invalidDialog(true))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText('Fix 2 fields before saving')).toBeInTheDocument()

    rerender(invalidDialog(false))
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(screen.queryByText('Fix 2 fields before saving')).not.toBeInTheDocument())
  })
})
