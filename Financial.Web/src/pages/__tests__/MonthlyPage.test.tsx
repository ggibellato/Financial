import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import MonthlyPage from '../MonthlyPage'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { CardStatementDto, CategoryTotalDto, ExpenseDto } from '../../api/types'

const getExpensesByMonthMock = vi.fn<FinancialApiClient['getExpensesByMonth']>()
const getCategoryTotalsByMonthMock = vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>()
const getCardStatementsByMonthMock = vi.fn<FinancialApiClient['getCardStatementsByMonth']>()
const createExpenseMock = vi.fn<FinancialApiClient['createExpense']>()
const updateExpenseMock = vi.fn<FinancialApiClient['updateExpense']>()
const deleteExpenseMock = vi.fn<FinancialApiClient['deleteExpense']>()
const markCardStatementPaidMock = vi.fn<FinancialApiClient['markCardStatementPaid']>()

vi.mock('../../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getExpensesByMonth: getExpensesByMonthMock,
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getCardStatementsByMonth: getCardStatementsByMonthMock,
    createExpense: createExpenseMock,
    updateExpense: updateExpenseMock,
    deleteExpense: deleteExpenseMock,
    markCardStatementPaid: markCardStatementPaidMock,
  }),
}))

const EXPENSES: ExpenseDto[] = [
  {
    id: 'e1',
    date: '2026-07-05',
    description: 'Lidl UK',
    value: 42.5,
    category: 'Mercado',
    paymentSource: 'Barclays',
    cardTag: null,
  },
]

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const CARD_STATEMENTS: CardStatementDto[] = [
  { id: 'c1', card: 'BaAmex', year: 2026, month: 7, isPaid: false, outstandingTotal: 100 },
  { id: 'c2', card: 'ChaseMaster4023', year: 2026, month: 7, isPaid: true, outstandingTotal: 0 },
]

describe('MonthlyPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getExpensesByMonthMock.mockResolvedValue(EXPENSES)
    getCategoryTotalsByMonthMock.mockResolvedValue(CATEGORY_TOTALS)
    getCardStatementsByMonthMock.mockResolvedValue(CARD_STATEMENTS)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('shows a loading state before data arrives', () => {
    render(<MonthlyPage />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('shows an error state with retry when the fetch fails', async () => {
    getExpensesByMonthMock.mockRejectedValue(new Error('Network down'))

    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())
    expect(screen.getByText('Network down')).toBeInTheDocument()
  })

  it('renders category totals, card statements with the combined adjustment figure, and the expense list together', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    expect(screen.getByText('Category Totals')).toBeInTheDocument()
    expect(screen.getAllByText('Mercado').length).toBeGreaterThan(0)
    expect(screen.getByText('Cards')).toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument()
    expect(screen.getByText(/Combined adjustment figure/)).toBeInTheDocument()
    expect(screen.getAllByText('100.00').length).toBeGreaterThan(0)
  })

  it('only shows Mark Paid for unpaid cards', async () => {
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    expect(screen.getAllByRole('button', { name: 'Mark Paid' })).toHaveLength(1)
  })

  it('marks a card statement paid when clicked', async () => {
    markCardStatementPaidMock.mockResolvedValue({ ...CARD_STATEMENTS[0], isPaid: true, outstandingTotal: 0 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByRole('cell', { name: 'BaAmex' })).toBeInTheDocument())
    fireEvent.click(screen.getByRole('button', { name: 'Mark Paid' }))

    await waitFor(() => expect(markCardStatementPaidMock).toHaveBeenCalledWith('c1'))
  })

  it('renders the add-expense form and submits a new expense', async () => {
    createExpenseMock.mockResolvedValue({ ...EXPENSES[0], id: 'e2' })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('New Expense')).toBeInTheDocument())
    fireEvent.change(screen.getByLabelText('Date'), { target: { value: '2026-07-16' } })
    fireEvent.change(screen.getByLabelText('Description'), { target: { value: 'Waitrose' } })
    fireEvent.change(screen.getByLabelText('Value'), { target: { value: '15.5' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add Expense' }))

    await waitFor(() =>
      expect(createExpenseMock).toHaveBeenCalledWith(expect.objectContaining({ description: 'Waitrose', value: 15.5 })),
    )
  })

  it('edits an expense value and saves, updating the displayed row', async () => {
    updateExpenseMock.mockResolvedValue({ ...EXPENSES[0], value: 50 })
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())

    fireEvent.click(screen.getAllByRole('button', { name: 'Edit' })[0])
    const valueInput = screen.getByDisplayValue('42.5')
    fireEvent.change(valueInput, { target: { value: '50' } })

    getExpensesByMonthMock.mockResolvedValue([{ ...EXPENSES[0], value: 50 }])
    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(updateExpenseMock).toHaveBeenCalledWith('e1', expect.objectContaining({ value: 50 })))
    await waitFor(() => expect(screen.getByText('50.00')).toBeInTheDocument())
  })

  it('deletes an expense after confirmation', async () => {
    deleteExpenseMock.mockResolvedValue(undefined)
    render(<MonthlyPage />)

    await waitFor(() => expect(screen.getByText('Lidl UK')).toBeInTheDocument())
    fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0])

    await waitFor(() => expect(deleteExpenseMock).toHaveBeenCalledWith('e1'))
  })
})
