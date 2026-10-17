import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ConfirmDeleteDialog from '../ConfirmDeleteDialog'

function renderDialog(overrides: Partial<Parameters<typeof ConfirmDeleteDialog>[0]> = {}) {
  const onConfirm = vi.fn()
  const onCancel = vi.fn()
  render(
    <ConfirmDeleteDialog
      entityLabel="Bank"
      message="“Chase” will be permanently removed."
      onConfirm={onConfirm}
      onCancel={onCancel}
      {...overrides}
    />,
  )
  return { onConfirm, onCancel }
}

describe('ConfirmDeleteDialog', () => {
  it('titles the dialog with the entity label and shows the message', () => {
    renderDialog()

    expect(screen.getByRole('dialog', { name: 'Delete Bank' })).toBeInTheDocument()
    expect(screen.getByText('“Chase” will be permanently removed.')).toBeInTheDocument()
  })

  it('calls onConfirm when Delete is clicked', () => {
    const { onConfirm, onCancel } = renderDialog()

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    expect(onConfirm).toHaveBeenCalledOnce()
    expect(onCancel).not.toHaveBeenCalled()
  })

  it('calls onCancel when Cancel is clicked', () => {
    const { onConfirm, onCancel } = renderDialog()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalledOnce()
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('calls onCancel when Escape dismisses the dialog', () => {
    const { onCancel } = renderDialog()

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onCancel).toHaveBeenCalledOnce()
  })

  it('disables Delete but keeps Cancel enabled when confirmation is blocked', () => {
    renderDialog({ confirmDisabled: true })

    expect(screen.getByRole('button', { name: 'Delete' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeEnabled()
  })
})
