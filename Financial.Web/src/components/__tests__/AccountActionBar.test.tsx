import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import AccountActionBar from '../AccountActionBar'

describe('AccountActionBar', () => {
  it('renders only the actions it knows, in the order given', () => {
    render(
      <AccountActionBar
        allowedActions={['AddIncome', 'Settle', 'AddExpense']}
        activeAction={null}
        formAreaId="form"
        onAction={vi.fn()}
      />,
    )

    expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual(['Add income', 'Add expense'])
  })

  it('renders every bank action this stage has a form for, skipping Pay recurring bill', () => {
    render(
      <AccountActionBar
        allowedActions={['AddExpense', 'AddIncome', 'TransferTo', 'TransferFrom', 'PayRecurringBill', 'Reconcile']}
        activeAction={null}
        formAreaId="form"
        onAction={vi.fn()}
      />,
    )

    expect(screen.getAllByRole('button').map((button) => button.textContent)).toEqual([
      'Add expense',
      'Add income',
      'Transfer to',
      'Transfer from',
      'Reconcile',
    ])
  })

  it('renders nothing when no allowed action is known', () => {
    render(<AccountActionBar allowedActions={['Settle']} activeAction={null} formAreaId="form" onAction={vi.fn()} />)

    expect(screen.queryByRole('group', { name: 'Account actions' })).not.toBeInTheDocument()
  })

  it('reports the action and the button that opened it', () => {
    const onAction = vi.fn()
    render(<AccountActionBar allowedActions={['AddExpense']} activeAction={null} formAreaId="form" onAction={onAction} />)

    const button = screen.getByRole('button', { name: 'Add expense' })
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

    expect(screen.getByRole('button', { name: 'Add income' })).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByRole('button', { name: 'Add expense' })).toHaveAttribute('aria-expanded', 'false')
    expect(screen.getByRole('button', { name: 'Add expense' })).toHaveAttribute('aria-controls', 'form')
  })

  it('disables every action while disabled', () => {
    render(
      <AccountActionBar allowedActions={['AddExpense', 'AddIncome']} activeAction={null} formAreaId="form" disabled onAction={vi.fn()} />,
    )

    expect(screen.getByRole('button', { name: 'Add expense' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Add income' })).toBeDisabled()
  })
})
