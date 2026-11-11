import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, within } from '../../test/renderWithFluent'
import EditMovementForm from '../EditMovementForm'

const POTS = [
  { id: 'p1', name: 'Home', isActive: true },
  { id: 'p2', name: 'Old car', isActive: false },
]

function renderForm(props: Partial<Parameters<typeof EditMovementForm>[0]> = {}) {
  return render(
    <EditMovementForm
      bucketId="p1"
      amount="25"
      date="2026-10-05"
      description="Bonus"
      currency="GBP"
      buckets={POTS}
      isSaving={false}
      error={null}
      errorFields={{}}
      onFieldChange={vi.fn()}
      onSave={vi.fn()}
      onCancel={vi.fn()}
      {...props}
    />,
  )
}

describe('EditMovementForm', () => {
  it('keeps the Reserva wording by default', () => {
    renderForm()

    expect(screen.getByRole('heading', { name: 'Edit Movement' })).toBeInTheDocument()
    expect(screen.getByLabelText(/^Bucket/)).toHaveValue('p1')
  })

  it('takes the wording of the screen it is on and marks an inactive pot', () => {
    renderForm({ title: 'Edit pot transaction', potLabel: 'Pot' })

    const form = screen.getByRole('form', { name: 'Edit pot transaction' })
    expect(within(form).getByLabelText(/^Pot/)).toHaveValue('p1')
    expect(within(form).getByRole('option', { name: 'Old car (inactive)' })).toBeInTheDocument()
  })

  it('shows the amount with the pot account currency', () => {
    renderForm()

    expect(screen.getByLabelText(/^Amount \(GBP\)/)).toHaveValue(25)
  })

  it('submits with Enter through the form', () => {
    const onSave = vi.fn()
    renderForm({ onSave })

    fireEvent.submit(screen.getByTestId('edit-movement-form-panel'))

    expect(onSave).toHaveBeenCalledOnce()
  })

  it('focuses the first invalid field and shows its message', () => {
    renderForm({ description: '', errorFields: { editMovementDescription: 'Description is required' }, error: 'Description is required' })

    expect(screen.getByLabelText(/^Description/)).toHaveFocus()
    expect(screen.getAllByText('Description is required')).toHaveLength(1)
  })

  it('announces a failed save without a field and moves focus to it', () => {
    renderForm({ error: 'Failed to update' })

    const alert = screen.getByRole('alert')
    expect(alert).toHaveTextContent('Failed to update')
    expect(alert).toHaveFocus()
  })

  it('disables the fields and Cancel while saving, keeping Save focusable', () => {
    renderForm({ isSaving: true })

    expect(screen.getByLabelText(/^Description/)).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Saving...' })).toHaveAttribute('aria-disabled', 'true')
  })
})
