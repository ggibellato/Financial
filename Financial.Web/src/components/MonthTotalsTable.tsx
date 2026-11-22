import { Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import DataTableCell from './grid/DataTableCell'
import { formatN2 } from '../utils/formatters'
import { hasMonthValue, type MonthTotalsColumn } from '../utils/monthTotals'
import './MonthTotalsTable.css'

interface MonthTotalsTableProps<T extends { month: string }> {
  title: string
  rows: T[]
  columns: MonthTotalsColumn<T>[]
}

function sumOf<T>(rows: T[], column: MonthTotalsColumn<T>): number {
  return rows.reduce((sum, row) => sum + column.value(row), 0)
}

export default function MonthTotalsTable<T extends { month: string }>({ title, rows, columns }: MonthTotalsTableProps<T>) {
  const monthsWithValues = rows.filter((row) => hasMonthValue(row, columns))

  return (
    <div className="month-totals-table" role="region" aria-label={`${title}, monthly totals`} tabIndex={0}>
      <Table className="data-table" aria-label={title}>
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Month</TableHeaderCell>
            {columns.map((column) => (
              <TableHeaderCell key={column.label} className="data-table__col--numeric">
                {column.label}
              </TableHeaderCell>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {monthsWithValues.map((row) => (
            <TableRow key={row.month}>
              <DataTableCell label="Month">{row.month}</DataTableCell>
              {columns.map((column) => (
                <DataTableCell key={column.label} label={column.label} className="data-table__col--numeric">
                  {column.isTotal ? <strong>{formatN2(column.value(row))}</strong> : formatN2(column.value(row))}
                </DataTableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
        <tfoot>
          <TableRow>
            <DataTableCell label="Total">Period total</DataTableCell>
            {columns.map((column) => (
              <DataTableCell key={column.label} label={column.label} className="data-table__col--numeric">
                {formatN2(sumOf(monthsWithValues, column))}
              </DataTableCell>
            ))}
          </TableRow>
        </tfoot>
      </Table>
    </div>
  )
}
