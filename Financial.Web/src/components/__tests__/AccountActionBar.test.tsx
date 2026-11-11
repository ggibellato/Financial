import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import AccountActionBar from '../AccountActionBar'

describe('AccountActionBar', () => {
  it('renders only the actions it knows, in the order given', () => {
    render(
      <AccountActionBar
        allowedActions={['AddIncome', 'SomeFutureAction', 'AddExpense']}
        activeAction={null}
        formAreaId="form"
        onAction={vi.fn()}
      />,
    )

    expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual(['New income', 'New expense'])
  })

  it('renders every bank action, in the order given', () => {
    render(
      <AccountActionBar
        allowedActions={['AddExpense', 'AddIncome', 'TransferTo', 'TransferFrom', 'PayRecurringBill', 'Reconcile']}
        activeAction={null}
        formAreaId="form"
        onAction={vi.fn()}
      />,
    )

    expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual([
      'New expense',
      'New income',
      'Transfer to',
      'Transfer from',
      'Pay recurring bill',
      'Adjust balance',
    ])
  })

  it('renders nothing when no allowed action is known', () => {
    render(<AccountActionBar allowedActions={['SomeFutureAction']} activeAction={null} formAreaId="form" onAction={vi.fn()} />)

    expect(screen.queryByRole('group', { name: 'Account actions' })).not.toBeInTheDocument()
  })

  it('[AC P60-F12-settlement-screen-07] keeps a disabled action focusable, does not run it, and describes it with its visible reason', () => {
    const onAction = vi.fn()
    render(<AccountActionBar allowedActions={['AddEntry', 'Settle']} activeAction={null} formAreaId="form" disabledReasons={{ Settle: 'Nothing to settle.' }} onAction={onAction} />)

    const settle = screen.getByRole('button', { name: 'Settle' })
    expect(settle).toHaveAttribute('aria-disabled', 'true')
    expect(settle).not.toBeDisabled()
    expect(settle).toHaveAccessibleDescription('Nothing to settle.')
    expect(screen.getByText('Nothing to settle.')).toBeVisible()
    fireEvent.click(settle)
    expect(onAction).not.toHaveBeenCalled()
  })

  it('leaves actions without a reason enabled and undescribed', () => {
    render(<AccountActionBar allowedActions={['AddEntry', 'Settle']} activeAction={null} formAreaId="form" disabledReasons={{ Settle: 'Nothing to settle.' }} onAction={vi.fn()} />)

    const addEntry = screen.getByRole('button', { name: 'New entry' })
    expect(addEntry).toBeEnabled()
    expect(addEntry).not.toHaveAttribute('aria-describedby')
  })

  it('reports the action and the button that opened it', () => {
    const onAction = vi.fn()
    render(<AccountActionBar allowedActions={['AddExpense']} activeAction={null} formAreaId="form" onAction={onAction} />)

    const button = screen.getByRole('button', { name: 'New expense' })
    fireEvent.click(button)

    expect(onAction).toHaveBeenCalledWith('AddExpense', button)
  })

  it('marks the open action as expanded and points at the form area', () => {
    render(
      <AccountActionBar
        allowedActions={['AddExpense', 'AddIncome']}
        activeAction="AddIncome"
        formAreaId="form"
        onAction={vi.fn()}
      />,
    )

    expect(screen.getByRole('button', { name: 'New income' })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByRole('button', { name: 'New expense' })).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getByRole('button', { name: 'New expense' })).toHaveAttribute('aria-controls', 'form')
  })

  it('disables only the actions the caller marks as disabled', () => {
    render(
      <AccountActionBar
        allowedActions={['AddExpense', 'TransferTo']}
        activeAction={null}
        formAreaId="form"
        isActionDisabled={(action) => action === 'TransferTo'}
        onAction={vi.fn()}
      />,
    )

    expect(screen.getByRole('button', { name: 'New expense' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Transfer to' })).toBeDisabled()
  })
})
