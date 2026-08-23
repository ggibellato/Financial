import { Button, makeStyles, tokens } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { ExpenseDto } from '../api/types'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ExpensesSection.css'

const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
})

interface ExpenseRowProps {
  expense: ExpenseDto
  onEdit: (expense: ExpenseDto) => void
  onDelete: (id: string) => void
}

function ExpenseRow({ expense, onEdit, onDelete }: ExpenseRowProps) {
  return (
    <tr>
      <td>
        <Button
          appearance="subtle"
          size="small"
          icon={<EditRegular />}
          aria-label="Edit expense"
          onClick={() => onEdit(expense)}
        />
      </td>
      <td>
        <Button
          appearance="subtle"
          size="small"
          icon={<DeleteRegular />}
          aria-label="Delete expense"
          onClick={() => onDelete(expense.id)}
        />
      </td>
      <td>{formatShortDate(expense.date)}</td>
      <td>{expense.description}</td>
      <td>{expense.categoryName}</td>
      <td className="data-table__col--numeric">{formatN2(expense.value)}</td>
      <td>{expense.paymentSourceBankName}</td>
      <td>{expense.creditCardName ?? '—'}</td>
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
  const styles = useStyles()
  return (
    <section className="expenses-section">
      <div className={styles.header}>
        <Button appearance="primary" icon={<AddRegular />} onClick={onNewExpense}>
          New Expense
        </Button>
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
