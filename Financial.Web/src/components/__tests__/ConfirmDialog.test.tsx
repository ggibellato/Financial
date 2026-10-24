import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import ConfirmDialog, { type ConfirmDialogProps } from '../ConfirmDialog'

function renderDialog(overrides: Partial<ConfirmDialogProps> = {}) {
  const onConfirm = vi.fn()
  const onCancel = vi.fn()
  render(
    <ConfirmDialog
      open
      title="Delete Expense"
      message="Delete this expense? This cannot be undone."
      confirmLabel="Delete"
      cancelLabel="Cancel"
      onConfirm={onConfirm}
      onCancel={onCancel}
      {...overrides}
    />,
  )
  return { onConfirm, onCancel }
}

describe('ConfirmDialog', () => {
  it('names the dialog by its title and describes it by its message', () => {
    renderDialog()

    const dialog = screen.getByRole('dialog', { name: 'Delete Expense' })
    expect(dialog).toHaveAccessibleDescription('Delete this expense? This cannot be undone.')
  })

  it('shows only the cancel button when there is no confirm label', () => {
    const { onCancel } = renderDialog({ confirmLabel: undefined, cancelLabel: 'Close' })

    expect(screen.queryByRole('button', { name: 'Delete' })).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Close' }))
    expect(onCancel).toHaveBeenCalledOnce()
  })

  it('reports confirm and cancel, and Escape counts as cancel', () => {
    const { onConfirm, onCancel } = renderDialog()

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onConfirm).toHaveBeenCalledOnce()
    expect(onCancel).toHaveBeenCalledTimes(2)
  })

  it('shows an error inside the dialog and can disable the confirm button', () => {
    renderDialog({ error: 'Could not delete. Check the connection and try again.', confirmDisabled: true })

    expect(screen.getByText('Could not delete. Check the connection and try again.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
  })

  it('disables both buttons while busy and ignores Escape', () => {
    const { onCancel } = renderDialog({ isBusy: true })

    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })
    expect(onCancel).not.toHaveBeenCalled()
  })

  it('renders nothing while closed', () => {
    renderDialog({ open: false })

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
