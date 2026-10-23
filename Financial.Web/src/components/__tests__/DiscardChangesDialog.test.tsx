import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import DiscardChangesDialog from '../DiscardChangesDialog'

describe('DiscardChangesDialog', () => {
  it('asks to discard unsaved changes', () => {
    render(<DiscardChangesDialog open onKeepEditing={vi.fn()} onDiscard={vi.fn()} />)

    expect(screen.getByRole('dialog', { name: 'Discard unsaved changes?' })).toBeInTheDocument()
    expect(screen.getByText('Your changes to this form will be lost.')).toBeInTheDocument()
  })

  it('reports Discard', () => {
    const onDiscard = vi.fn()
    render(<DiscardChangesDialog open onKeepEditing={vi.fn()} onDiscard={onDiscard} />)

    fireEvent.click(screen.getByRole('button', { name: 'Discard' }))

    expect(onDiscard).toHaveBeenCalledOnce()
  })

  it('reports Keep editing, and Escape counts as keep editing', () => {
    const onKeepEditing = vi.fn()
    render(<DiscardChangesDialog open onKeepEditing={onKeepEditing} onDiscard={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Keep editing' }))
    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(onKeepEditing).toHaveBeenCalledTimes(2)
  })

  it('renders nothing while closed', () => {
    render(<DiscardChangesDialog open={false} onKeepEditing={vi.fn()} onDiscard={vi.fn()} />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
