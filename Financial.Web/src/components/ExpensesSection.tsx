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
      <td>{formatShortDate(expense.date)}</td>
      <td>{expense.description}</td>
      <td>{expense.category}</td>
      <td className="data-table__col--numeric">{formatN2(expense.value)}</td>
      <td>{expense.paymentSource}</td>
      <td>{expense.cardTag ?? '—'}</td>
      <td>
        <button type="button" onClick={() => onEdit(expense)}>
          Edit
        </button>
        <button type="button" onClick={() => onDelete(expense.id)}>
          Delete
        </button>
      </td>
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
              <th>Date</th>
              <th>Description</th>
              <th>Category</th>
              <th className="data-table__col--numeric">Value</th>
              <th>Payment Source</th>
              <th>Card</th>
              <th />
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
