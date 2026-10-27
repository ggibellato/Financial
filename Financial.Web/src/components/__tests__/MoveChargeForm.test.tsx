import { describe, expect, it, vi } from 'vitest'
import type { ExpenseDto } from '../../api/types'
import type { MoveChargeFormState } from '../../hooks/useMoveChargeForm'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import MoveChargeForm from '../MoveChargeForm'

const CHARGE = { id: 'c1', description: 'Taxi', date: '2026-11-03', chargeDate: '2026-11-03', value: 30, invoiceDate: '2026-11-01', creditCardId: 'card-amex' } as ExpenseDto

function moveFormState(overrides: Partial<MoveChargeFormState> = {}): MoveChargeFormState {
  return {
    isOpen: true,
    form: { charge: CHARGE, target: '', isSaving: false, error: null, targetError: null },
    targets: [
      { value: '2026-12', label: 'December 2026' },
      { value: '2027-01', label: 'January 2027 (new)' },
    ],
    isLoadingTargets: false,
    hasNoTargets: false,
    targetsError: null,
    retryTargets: vi.fn(),
    isDirty: false,
    open: vi.fn(),
    cancel: vi.fn(),
    setTarget: vi.fn(),
    submit: vi.fn(() => true),
    ...overrides,
  }
}

describe('MoveChargeForm', () => {
  it('names the charge and its current statement and lists the targets', () => {
    render(<MoveChargeForm moveForm={moveFormState()} onCancel={vi.fn()} />)

    expect(screen.getByRole('heading', { name: 'Move charge to a later statement' })).toBeInTheDocument()
    expect(screen.getByText('Taxi (03/11/2026, 30.00) is on the November 2026 statement.')).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'January 2027 (new)' })).toBeInTheDocument()
  })

  it('sets the target, submits and cancels', () => {
    const state = moveFormState()
    const onCancel = vi.fn()
    render(<MoveChargeForm moveForm={state} onCancel={onCancel} />)

    fireEvent.change(screen.getByLabelText(/^Statement/), { target: { value: '2027-01' } })
    fireEvent.click(screen.getByRole('button', { name: 'Move charge' }))
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(state.setTarget).toHaveBeenCalledWith('2027-01')
    expect(state.submit).toHaveBeenCalled()
    expect(onCancel).toHaveBeenCalled()
  })

  it('keeps the statement list focusable and busy while it loads', () => {
    render(<MoveChargeForm moveForm={moveFormState({ isLoadingTargets: true, targets: [] })} onCancel={vi.fn()} />)

    expect(screen.getByLabelText(/^Statement/)).toBeEnabled()
    expect(screen.getByLabelText(/^Statement/)).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('option', { name: 'Loading statements…' })).toBeInTheDocument()
  })

  it('shows the load error with a retry, the validation message and the save error', () => {
    const state = moveFormState({
      targetsError: 'Unable to load the statements',
      form: { charge: CHARGE, target: '', isSaving: false, error: 'Charges can only move to a later, unpaid statement.', targetError: 'Select a statement.' },
    })
    render(<MoveChargeForm moveForm={state} onCancel={vi.fn()} />)

    expect(screen.getByText('Unable to load the statements')).toBeInTheDocument()
    expect(screen.getByText('Select a statement.')).toBeInTheDocument()
    expect(screen.getByText('Charges can only move to a later, unpaid statement.')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(state.retryTargets).toHaveBeenCalled()
  })

  it('focuses the statement list after a failed submit', () => {
    render(<MoveChargeForm moveForm={moveFormState({ submit: vi.fn(() => false) })} onCancel={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Move charge' }))

    expect(screen.getByLabelText(/^Statement/)).toHaveFocus()
  })

  it('says when no later month can take the charge and disables Move', () => {
    render(<MoveChargeForm moveForm={moveFormState({ hasNoTargets: true, targets: [] })} onCancel={vi.fn()} />)

    expect(screen.getByText('No unpaid statement in the next 12 months.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Move charge' })).toBeDisabled()
  })

  it('shows the saving state', () => {
    const state = moveFormState({ form: { charge: CHARGE, target: '2026-12', isSaving: true, error: null, targetError: null } })
    render(<MoveChargeForm moveForm={state} onCancel={vi.fn()} />)

    expect(screen.getByRole('button', { name: /Saving/ })).toBeDisabled()
  })

  it('renders nothing without a charge', () => {
    const { container } = render(<MoveChargeForm moveForm={moveFormState({ form: null, isOpen: false })} onCancel={vi.fn()} />)

    expect(container.querySelector('form')).toBeNull()
  })
})
