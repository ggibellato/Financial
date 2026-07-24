import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ExpensesSection from '../ExpensesSection'
import type { ExpenseDto } from '../../api/types'

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: '2026-07-05',
    description: 'Lidl UK',
    value: 42.5,
    category: 'Mercado',
    paymentSource: 'Barclays',
    cardTag: null,
    settledAt: null,
    paymentStatus: 'ImmediatePayment',
  },
  {
    id: 'e2',
    date: '2026-07-06',
    description: 'Amazon',
    value: 9.99,
    category: 'Extras',
    paymentSource: null,
    cardTag: 'BarclaysPlatinumVisa8003',
    settledAt: null,
    paymentStatus: 'CreditCardCharge',
  },
]

describe('ExpensesSection', () => {
  it('renders a row per expense, including a placeholder for a null card tag', () => {
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    expect(screen.getByText('Lidl UK')).toBeInTheDocument()
    expect(screen.getByText('Amazon')).toBeInTheDocument()
    expect(screen.getByText('BarclaysPlatinumVisa8003')).toBeInTheDocument()
    expect(screen.getByText('42.50')).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it('calls onEdit with the clicked expense', () => {
    const onEdit = vi.fn()
    render(<ExpensesSection expenses={EXPENSES} onEdit={onEdit} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit expense' })[0])

    expect(onEdit).toHaveBeenCalledWith(EXPENSES[0])
  })

  it('calls onDelete with the clicked expense id', () => {
    const onDelete = vi.fn()
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={onDelete} onNewExpense={vi.fn()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Delete expense' })[1])

    expect(onDelete).toHaveBeenCalledWith('e2')
  })

  it('calls onNewExpense when the New Expense button is clicked', () => {
    const onNewExpense = vi.fn()
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={onNewExpense} />)

    fireEvent.click(screen.getByRole('button', { name: 'New Expense' }))

    expect(onNewExpense).toHaveBeenCalledOnce()
  })
})
