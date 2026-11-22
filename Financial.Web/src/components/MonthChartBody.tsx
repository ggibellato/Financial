import type { ReactNode } from 'react'
import EmptyState from './EmptyState'
import MonthTotalsTable from './MonthTotalsTable'
import { hasMonthValues, type MonthTotalsColumn } from '../utils/monthTotals'
import './MonthChartBody.css'

interface MonthChartBodyProps<T extends { month: string }> {
  title: string
  rows: T[]
  columns: MonthTotalsColumn<T>[]
  showTable: boolean
  children: ReactNode
}

export default function MonthChartBody<T extends { month: string }>({ title, rows, columns, showTable, children }: MonthChartBodyProps<T>) {
  if (!hasMonthValues(rows, columns)) {
    return <EmptyState className="month-chart-body__empty">No values in this period.</EmptyState>
  }
  if (showTable) {
    return <MonthTotalsTable title={title} rows={rows} columns={columns} />
  }
  return children
}
