import { fireEvent, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import FormSaveActions from '../FormSaveActions'

function renderInForm(props: { isSaving: boolean; isSaveDisabled?: boolean }, onSubmit = vi.fn()) {
  const onCancel = vi.fn()
  const view = render(
    <form
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit()
      }}
    >
      <FormSaveActions saveLabel="Add Expense" onCancel={onCancel} {...props} />
    </form>,
  )
  return { ...view, onSubmit, onCancel }
}

describe('FormSaveActions', () => {
  it('submits the form and cancels when idle', async () => {
    const user = userEvent.setup()
    const { onSubmit, onCancel } = renderInForm({ isSaving: false })

    await user.click(screen.getByRole('button', { name: 'Add Expense' }))
    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onSubmit).toHaveBeenCalledTimes(1)
    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('keeps Save focusable but marked unavailable while saving, and disables Cancel', () => {
    renderInForm({ isSaving: true })

    const save = screen.getByRole('button', { name: 'Saving...' })
    expect(save).not.toBeDisabled()
    expect(save).toHaveAttribute('aria-disabled', 'true')
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('disables Save and Cancel while another action of the form runs', () => {
    render(<FormSaveActions saveLabel="Save due date" isSaving={false} isBusy onCancel={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Save due date' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('keeps focus on Save from the click through saving to a failed save', () => {
    const props = { saveLabel: 'Add Expense', onCancel: vi.fn() }
    const { rerender } = render(<FormSaveActions {...props} isSaving={false} />)
    screen.getByRole('button', { name: 'Add Expense' }).focus()

    rerender(<FormSaveActions {...props} isSaving />)
    expect(screen.getByRole('button', { name: 'Saving...' })).toHaveFocus()

    rerender(<FormSaveActions {...props} isSaving={false} />)
    expect(screen.getByRole('button', { name: 'Add Expense' })).toHaveFocus()
  })

  it('does not submit again while saving, by click or by keyboard', async () => {
    const user = userEvent.setup()
    const { onSubmit } = renderInForm({ isSaving: true })
    const save = screen.getByRole('button', { name: 'Saving...' })

    await user.click(save)
    save.focus()
    await user.keyboard('{Enter}')
    await user.keyboard(' ')
    fireEvent.click(save)

    expect(onSubmit).not.toHaveBeenCalled()
    expect(save).toHaveFocus()
  })

  it('takes focus on submit from Enter in a field, so the field disabling while saving does not drop it', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn((event: { preventDefault: () => void }) => event.preventDefault())
    render(
      <form onSubmit={onSubmit}>
        <input aria-label="Description" />
        <FormSaveActions saveLabel="Add Expense" isSaving={false} onCancel={vi.fn()} />
      </form>,
    )

    await user.type(screen.getByLabelText('Description'), 'Rent{Enter}')

    expect(onSubmit).toHaveBeenCalledTimes(1)
    expect(screen.getByRole('button', { name: 'Add Expense' })).toHaveFocus()
  })

  it('disables Save outright when the form cannot be saved yet', () => {
    renderInForm({ isSaving: false, isSaveDisabled: true })

    expect(screen.getByRole('button', { name: 'Add Expense' })).toBeDisabled()
  })

  it('announces Saving... in a polite status region only while saving', () => {
    const { rerender } = renderInForm({ isSaving: false })
    const status = screen.getByRole('status')
    expect(status.textContent).toBe('')

    rerender(
      <form>
        <FormSaveActions saveLabel="Add Expense" onCancel={vi.fn()} isSaving />
      </form>,
    )
    expect(screen.getByRole('status')).toHaveTextContent('Saving...')
  })
})
