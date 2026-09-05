import {
  Button,
  MessageBar,
  MessageBarBody,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { IncomeDto } from '../api/types'
import SortableColumnHeader from './grid/SortableColumnHeader'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useColumnFilters } from '../hooks/useColumnFilters'
import { formatN2, formatShortDate } from '../utils/formatters'
import './IncomeSection.css'

// Grid create/new actions: left-aligned primary button with an add icon,
// matching ExpensesSection.tsx (docs/ui/forms-data-and-visualisations.md).
const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
})

interface IncomeRowProps {
  income: IncomeDto
  onEdit: (income: IncomeDto) => void
  onDelete: (id: string) => void
}

function IncomeRow({ income, onEdit, onDelete }: IncomeRowProps) {
  return (
    <TableRow>
      <TableCell>{formatShortDate(income.date)}</TableCell>
      <TableCell>{income.incomeSourceName}</TableCell>
      <TableCell className="data-table__col--numeric">
        {income.grossValue != null ? formatN2(income.grossValue) : '—'}
      </TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(income.netValue)}</TableCell>
      <TableCell>{income.bankName ?? '—'}</TableCell>
      <TableCell>{income.description ?? ''}</TableCell>
      <TableCell className="data-table__col--action">
        <div className="data-table__actions-cell">
          <Button
            appearance="subtle"
            size="small"
            icon={<EditRegular />}
            aria-label="Edit income"
            onClick={() => onEdit(income)}
          />
          <Button
            appearance="subtle"
            size="small"
            icon={<DeleteRegular />}
            aria-label="Delete income"
            onClick={() => onDelete(income.id)}
          />
        </div>
      </TableCell>
    </TableRow>
  )
}

interface IncomeSectionProps {
  incomes: IncomeDto[]
  onEdit: (income: IncomeDto) => void
  onDelete: (id: string) => void
  onNewIncome: () => void
  splitConfirmationMessage?: string | null
}

const SORT_ACCESSORS: Record<string, SortAccessor<IncomeDto>> = {
  date: (income) => new Date(income.date),
  source: (income) => income.incomeSourceName,
  gross: (income) => income.grossValue,
  net: (income) => income.netValue,
  bank: (income) => income.bankName,
  description: (income) => income.description,
}

const FILTER_ACCESSORS = {
  bank: (income: IncomeDto) => income.bankName,
}

export default function IncomeSection({
  incomes,
  onEdit,
  onDelete,
  onNewIncome,
  splitConfirmationMessage,
}: IncomeSectionProps) {
  const styles = useStyles()
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(incomes, FILTER_ACCESSORS)
  const { sortedRows, sortState, requestSort } = useSortableRows(filteredRows, SORT_ACCESSORS)

  return (
    <section className="income-section">
      <div className={styles.header}>
        <Button appearance="primary" icon={<AddRegular />} onClick={onNewIncome}>
          New Income
        </Button>
      </div>
      {splitConfirmationMessage && (
        <MessageBar intent="success">
          <MessageBarBody>{splitConfirmationMessage}</MessageBarBody>
        </MessageBar>
      )}
      <div className="income-section__table-wrapper">
        <Table className="income-section__table data-table">
          <TableHeader>
            <TableRow>
              <SortableColumnHeader
                label="Date"
                columnKey="date"
                sortDirection={sortState?.columnKey === 'date' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Source"
                columnKey="source"
                sortDirection={sortState?.columnKey === 'source' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Gross"
                columnKey="gross"
                numeric
                sortDirection={sortState?.columnKey === 'gross' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Net"
                columnKey="net"
                numeric
                sortDirection={sortState?.columnKey === 'net' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <SortableColumnHeader
                label="Bank"
                columnKey="bank"
                sortDirection={sortState?.columnKey === 'bank' ? sortState.direction : undefined}
                onSort={requestSort}
              >
                <ColumnFilterMenu
                  columnKey="bank"
                  label="Bank"
                  availableValues={availableValues.bank}
                  selectedValues={selectedValues.bank}
                  onToggleValue={toggleValue}
                  onToggleAll={toggleAll}
                  isFiltered={isColumnFiltered('bank')}
                />
              </SortableColumnHeader>
              <SortableColumnHeader
                label="Description"
                columnKey="description"
                sortDirection={sortState?.columnKey === 'description' ? sortState.direction : undefined}
                onSort={requestSort}
              />
              <TableHeaderCell className="data-table__col--action" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {sortedRows.length === 0 && isColumnFiltered('bank') ? (
              <TableRow>
                <TableCell colSpan={7}>No rows match the current filters</TableCell>
              </TableRow>
            ) : (
              sortedRows.map((income) => (
                <IncomeRow key={income.id} income={income} onEdit={onEdit} onDelete={onDelete} />
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </section>
  )
}
