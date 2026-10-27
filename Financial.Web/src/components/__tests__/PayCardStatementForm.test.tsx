import { describe, expect, it, vi } from 'vitest'
import type { PayCardStatementForm as PayCardStatementFormState } from '../../hooks/usePayCardStatementForm'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import PayCardStatementForm from '../PayCardStatementForm'

const BARCLAYS = { id: 'barclays', name: 'Barclays' }

function formState(overrides: Partial<PayCardStatementFormState> = {}): PayCardStatementFormState {
  return {
    isOpen: true,
    cards: [{ id: 'amex', name: 'Amex' }],
    statements: [{ id: 'amex-oct', creditCardId: 'amex', creditCardName: 'Amex', year: 2026, month: 10, total: 75, currency: 'GBP' }],
    statement: null,
    values: { cardId: 'amex', statementId: '', date: '2026-10-15' },
    isLoading: false,
    loadError: null,
    retry: vi.fn(),
    hasNothingToPay: false,
    isSaving: false,
    saveError: null,
    fieldErrors: {},
    isDirty: true,
    open: vi.fn(),
    cancel: vi.fn(),
    setField: vi.fn(),
    submit: vi.fn(() => null),
    ...overrides,
  }
}

describe('PayCardStatementForm', () => {
  it('shows the paying bank as text and the amount as a dash until a statement is chosen', () => {
    render(<PayCardStatementForm payForm={formState()} lockedBank={BARCLAYS} onCancel={vi.fn()} />)

    expect(screen.getByText('Barclays')).toBeInTheDocument()
    expect(screen.getByLabelText(/^Amount/)).toHaveValue('\u2014')
  })

  it('keeps the pickers focusable and busy while the statements load', () => {
    render(<PayCardStatementForm payForm={formState({ isLoading: true, cards: [], values: { cardId: '', statementId: '', date: '2026-10-15' } })} lockedBank={BARCLAYS} onCancel={vi.fn()} />)

    const card = screen.getByRole('combobox', { name: /^Card/ })
    expect(card).toBeEnabled()
    expect(card).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('option', { name: 'Loading statements\u2026' })).toBeInTheDocument()
  })

  it('disables the form and shows Saving... while the payment saves', () => {
    render(<PayCardStatementForm payForm={formState({ isSaving: true })} lockedBank={BARCLAYS} onCancel={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByRole('combobox', { name: /^Card/ })).toBeDisabled()
  })

  it('focuses the first invalid field the submit reports', () => {
    render(<PayCardStatementForm payForm={formState({ submit: vi.fn(() => 'statementId' as const) })} lockedBank={BARCLAYS} onCancel={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Pay statement' }))

    expect(screen.getByRole('combobox', { name: /^Statement/ })).toHaveFocus()
  })

  it('shows the server message', () => {
    render(<PayCardStatementForm payForm={formState({ saveError: 'There is nothing to pay on this statement.' })} lockedBank={BARCLAYS} onCancel={vi.fn()} />)

    expect(screen.getByText('There is nothing to pay on this statement.')).toBeInTheDocument()
  })
})
