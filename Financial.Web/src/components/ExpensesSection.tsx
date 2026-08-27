import { Button, makeStyles, tokens } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { ExpenseDto } from '../api/types'
import SortableColumnHeader from './grid/SortableColumnHeader'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useColumnFilters } from '../hooks/useColumnFilters'
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

const SORT_ACCESSORS: Record<string, SortAccessor<ExpenseDto>> = {
  date: (expense) => new Date(expense.date),
  description: (expense) => expense.description,
  category: (expense) => expense.categoryName,
  value: (expense) => expense.value,
  paymentSource: (expense) => expense.paymentSourceBankName,
  card: (expense) => expense.creditCardName,
}

const FILTER_ACCESSORS = {
  category: (expense: ExpenseDto) => expense.categoryName,
  card: (expense: ExpenseDto) => expense.creditCardName,
}

export default function ExpensesSection({ expenses, onEdit, onDelete, onNewExpense }: ExpensesSectionProps) {
  const styles = useStyles()
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(expenses, FILTER_ACCESSORS)
  const { sortedRows, sortState, requestSort } = useSortableRows(filteredRows, SORT_ACCESSORS)
  const hasActiveFilter = isColumnFiltered('category') || isColumnFiltered('card')

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
              <SortableColumnHeader
                label="Date"
                columnKey="date"
                sortDirection={sortState?.columnKey === 'date' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Description"
                columnKey="description"
                sortDirection={sortState?.columnKey === 'description' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Category"
                columnKey="category"
                sortDirection={sortState?.columnKey === 'category' ? sortState.direction : undefined}
                onSort={requestSort}
              >
                <ColumnFilterMenu
                  columnKey="category"
                  label="Category"
                  availableValues={availableValues.category}
                  selectedValues={selectedValues.category}
                  onToggleValue={toggleValue}
                  onToggleAll={toggleAll}
                  isFiltered={isColumnFiltered('category')}
                />
              </SortableColumnHeader>
              <SortableColumnHeader
                label="Value"
                columnKey="value"
                numeric
                sortDirection={sortState?.columnKey === 'value' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Payment Source"
                columnKey="paymentSource"
                sortDirection={sortState?.columnKey === 'paymentSource' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Card"
                columnKey="card"
                sortDirection={sortState?.columnKey === 'card' ? sortState.direction : undefined}
                onSort={requestSort}
              >
                <ColumnFilterMenu
                  columnKey="card"
                  label="Card"
                  availableValues={availableValues.card}
                  selectedValues={selectedValues.card}
                  onToggleValue={toggleValue}
                  onToggleAll={toggleAll}
                  isFiltered={isColumnFiltered('card')}
                />
              </SortableColumnHeader>
            </tr>
          </thead>
          <tbody>
            {sortedRows.length === 0 && hasActiveFilter ? (
              <tr>
                <td colSpan={8}>No rows match the current filters</td>
              </tr>
            ) : (
              sortedRows.map((expense) => (
                <ExpenseRow key={expense.id} expense={expense} onEdit={onEdit} onDelete={onDelete} />
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}
