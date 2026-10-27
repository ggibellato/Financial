import type { ReactNode } from 'react'
import { Skeleton, SkeletonItem, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import type { AccountTransactionDto } from '../api/types'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import DataTableCell from './grid/DataTableCell'
import RowActionsCell from './grid/RowActionsCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'
import { hasRowActions, kindLabel, recordNoun, rowKey } from './accountActions'

interface TransactionColumn {
  key: string
  header: string
  numeric?: boolean
  accessor: SortAccessor<AccountTransactionDto>
  render: (row: AccountTransactionDto) => ReactNode
}

const COLUMNS: TransactionColumn[] = [
  { key: 'date', header: 'Date', accessor: (row) => row.date, render: (row) => formatShortDateUtc(row.date) },
  {
    key: 'description',
    header: 'Description',
    accessor: (row) => row.description,
    render: (row) => <TruncatedText text={row.description} />,
  },
  { key: 'kind', header: 'Type', accessor: (row) => kindLabel(row.kind), render: (row) => kindLabel(row.kind) },
  { key: 'category', header: 'Category', accessor: (row) => row.categoryName, render: (row) => <TruncatedText text={row.categoryName} /> },
  { key: 'counterpart', header: 'Counterpart', accessor: (row) => row.counterpart, render: (row) => <TruncatedText text={row.counterpart} /> },
  {
    key: 'amount',
    header: 'Amount',
    numeric: true,
    accessor: (row) => row.amount,
    render: (row) => formatSignedN2(row.amount),
  },
  {
    key: 'roundUp',
    header: 'Round-up',
    numeric: true,
    accessor: (row) => row.roundUp,
    render: (row) => (row.roundUp == null ? '' : formatSignedN2(row.roundUp)),
  },
]

const ACCESSORS = Object.fromEntries(COLUMNS.map((column) => [column.key, column.accessor]))

const COLUMN_COUNT = COLUMNS.length + 1

interface AccountTransactionsGridProps {
  transactions: AccountTransactionDto[]
  isLoading?: boolean
  emptyMessage: string
  dateHeader?: string
  isEditDisabled?: boolean
  onEdit: (row: AccountTransactionDto, button: HTMLButtonElement) => void
  onDelete: (row: AccountTransactionDto) => void
}

export default function AccountTransactionsGrid({
  transactions,
  isLoading = false,
  emptyMessage,
  dateHeader = 'Date',
  isEditDisabled = false,
  onEdit,
  onDelete,
}: AccountTransactionsGridProps) {
  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, ACCESSORS, DATE_DESC_SORT)
  const headerOf = (column: TransactionColumn) => (column.key === 'date' ? dateHeader : column.header)

  return (
    <div className="account-screen__grid-scroll">
      <Table className="data-table" aria-label="Transactions">
        <TableHeader>
          <TableRow>
            {COLUMNS.map((column) => (
              <SortableColumnHeader
                key={column.key}
                label={headerOf(column)}
                columnKey={column.key}
                numeric={column.numeric}
                sortDirection={sortState?.columnKey === column.key ? sortState.direction : undefined}
                onSort={requestSort}
              />
            ))}
            <TableHeaderCell className="data-table__col--action" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading ? (
            <TableRow>
              <TableCell colSpan={COLUMN_COUNT}>
                <Skeleton aria-label="Loading transactions" className="account-screen__grid-skeleton">
                  <SkeletonItem />
                  <SkeletonItem />
                  <SkeletonItem />
                </Skeleton>
              </TableCell>
            </TableRow>
          ) : sortedRows.length === 0 ? (
            <TableRow>
              <TableCell className="data-table__cell--message" colSpan={COLUMN_COUNT}>
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            sortedRows.map((row) => (
              <TableRow key={rowKey(row)}>
                {COLUMNS.map((column) => (
                  <DataTableCell
                    key={column.key}
                    label={headerOf(column)}
                    className={column.numeric ? 'data-table__col--numeric' : undefined}
                  >
                    {column.render(row)}
                  </DataTableCell>
                ))}
                {hasRowActions(row) ? (
                  <RowActionsCell
                    itemName={recordNoun(row.kind)}
                    editDisabled={isEditDisabled}
                    onEdit={(button) => onEdit(row, button)}
                    onDelete={() => onDelete(row)}
                  />
                ) : (
                  <TableCell className="data-table__col--action" />
                )}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  )
}
