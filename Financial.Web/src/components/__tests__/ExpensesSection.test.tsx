import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../test/renderWithFluent'
import ExpensesSection from '../ExpensesSection'
import type { ExpenseDto } from '../../api/types'

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: '2026-07-05',
    description: 'Lidl UK',
    value: 42.5,
    categoryId: 'category-mercado',
    categoryName: 'Mercado',
    paymentSourceBankId: 'bank-barclays',
    paymentSourceBankName: 'Barclays',
    creditCardId: null,
    creditCardName: null,
    chargeDate: null,
    invoiceDate: null,
    paymentStatus: 'ImmediatePayment',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
  },
  {
    id: 'e2',
    date: '2026-07-06',
    description: 'Amazon',
    value: 9.99,
    categoryId: 'category-extras',
    categoryName: 'Extras',
    paymentSourceBankId: null,
    paymentSourceBankName: null,
    creditCardId: 'card-barclays8003',
    creditCardName: 'BarclaysPlatinumVisa8003',
    chargeDate: '2026-07-06',
    invoiceDate: '2026-07-01',
    paymentStatus: 'CreditCardCharge',
    roundUpAmount: null,
    suggestedRoundUpAmount: null,
    countsAsTithe: true,
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

  it('renders the category name, not its id', () => {
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    expect(screen.getByText('Mercado')).toBeInTheDocument()
    expect(screen.getByText('Extras')).toBeInTheDocument()
    expect(screen.queryByText('category-mercado')).not.toBeInTheDocument()
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

  it('sorts rows by Value when the Value column header is clicked', () => {
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Value' }))

    const descriptionCells = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelectorAll('td')[1].textContent)
    expect(descriptionCells).toEqual(['Amazon', 'Lidl UK'])

    fireEvent.click(screen.getByRole('button', { name: 'Value' }))

    const descriptionCellsDescending = screen
      .getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelectorAll('td')[1].textContent)
    expect(descriptionCellsDescending).toEqual(['Lidl UK', 'Amazon'])
  })

  it('filters rows by Category via the header checklist', () => {
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Extras' }))

    expect(screen.queryByText('Amazon')).not.toBeInTheDocument()
    expect(screen.getByText('Lidl UK')).toBeInTheDocument()
  })

  it('combines Category and Card filters with AND', () => {
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    // Keep only Mercado (excludes Amazon) and only BarclaysPlatinumVisa8003 (excludes Lidl UK,
    // whose card is null) — the two filters combined should leave zero rows.
    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Extras' }))
    fireEvent.click(screen.getByRole('button', { name: 'Filter by Card' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'BarclaysPlatinumVisa8003' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
  })

  it('the Category filter icon and the Category sort control coexist in the same header cell without interfering', () => {
    render(<ExpensesSection expenses={EXPENSES} onEdit={vi.fn()} onDelete={vi.fn()} onNewExpense={vi.fn()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Value' }))
    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Extras' }))

    // Sort (by Value, ascending) still applied, and the filter (excluding Amazon/Extras) still applied.
    expect(screen.queryByText('Amazon')).not.toBeInTheDocument()
    const dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Lidl UK')).toBeInTheDocument()
  })
})
