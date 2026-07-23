import type { ExpenseDto } from '../api/types'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ExpensesSection.css'

interface ExpenseRowProps {
  expense: ExpenseDto
  onEdit: (expense: ExpenseDto) => void
  onDelete: (id: string) => void
}

function ExpenseRow({ expense, onEdit, onDelete }: ExpenseRowProps) {
  return (
    <tr>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Edit expense"
          onClick={() => onEdit(expense)}
        >
          ✏
        </button>
      </td>
      <td>
        <button
          className="data-table__action-btn"
          type="button"
          aria-label="Delete expense"
          onClick={() => onDelete(expense.id)}
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M20 20H7L3 16a2 2 0 0 1 0-2.83L14.59 1.58a2 2 0 0 1 2.83 0l4 4a2 2 0 0 1 0 2.83L8 20" />
            <path d="M6.5 15.5 15 7" />
          </svg>
        </button>
      </td>
      <td>{formatShortDate(expense.date)}</td>
      <td>{expense.description}</td>
      <td>{expense.category}</td>
      <td className="data-table__col--numeric">{formatN2(expense.value)}</td>
      <td>{expense.paymentSource}</td>
      <td>{expense.cardTag ?? '—'}</td>
    </tr>
  )
}

interface ExpensesSectionProps {
  expenses: ExpenseDto[]
  onEdit: (expense: ExpenseDto) => void
  onDelete: (id: string) => void
  onNewExpense: () => void
}

export default function ExpensesSection({ expenses, onEdit, onDelete, onNewExpense }: ExpensesSectionProps) {
  return (
    <section className="expenses-section">
      <div className="expenses-section__header">
        <h2>Expenses</h2>
        <button className="expenses-section__new-btn" type="button" onClick={onNewExpense}>
          New Expense
        </button>
      </div>
      <div className="expenses-section__table-wrapper">
        <table className="expenses-section__table data-table">
          <thead>
            <tr>
              <th />
              <th />
              <th>Date</th>
              <th>Description</th>
              <th>Category</th>
              <th className="data-table__col--numeric">Value</th>
              <th>Payment Source</th>
              <th>Card</th>
            </tr>
          </thead>
          <tbody>
            {expenses.map((expense) => (
              <ExpenseRow key={expense.id} expense={expense} onEdit={onEdit} onDelete={onDelete} />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}
