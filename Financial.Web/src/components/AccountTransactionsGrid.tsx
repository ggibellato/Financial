import { Skeleton, SkeletonItem, Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import type { AccountTransactionDto } from '../api/types'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'

const KIND_LABELS: Record<string, string> = {
  Expense: 'Expense',
  CardCharge: 'Card charge',
  Income: 'Income',
  TransferIn: 'Transfer in',
  TransferOut: 'Transfer out',
  Adjustment: 'Adjustment',
}

const SKELETON_ROW_KEYS = ['first', 'second', 'third']

function kindLabel(kind: string): string {
  return KIND_LABELS[kind] ?? kind
}

const COLUMNS: { key: string; header: string; numeric?: boolean; accessor: SortAccessor<AccountTransactionDto> }[] = [
  { key: 'date', header: 'Date', accessor: (row) => row.date },
  { key: 'description', header: 'Description', accessor: (row) => row.description },
  { key: 'kind', header: 'Type', accessor: (row) => kindLabel(row.kind) },
  { key: 'category', header: 'Category', accessor: (row) => row.categoryName },
  { key: 'counterpart', header: 'Counterpart', accessor: (row) => row.counterpart },
  { key: 'amount', header: 'Amount', numeric: true, accessor: (row) => row.amount },
  { key: 'roundUp', header: 'Round-up', numeric: true, accessor: (row) => row.roundUp },
]

const ACCESSORS = Object.fromEntries(COLUMNS.map((column) => [column.key, column.accessor]))

interface AccountTransactionsGridProps {
  transactions: AccountTransactionDto[]
  isLoading?: boolean
  emptyMessage: string
}

export default function AccountTransactionsGrid({ transactions, isLoading = false, emptyMessage }: AccountTransactionsGridProps) {
  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, ACCESSORS, DATE_DESC_SORT)

  return (
    <div className="account-screen__grid-scroll">
      <Table className="data-table" aria-label="Transactions">
        <TableHeader>
          <TableRow>
            {COLUMNS.map((column) => (
              <SortableColumnHeader
                key={column.key}
                label={column.header}
                columnKey={column.key}
                numeric={column.numeric}
                sortDirection={sortState?.columnKey === column.key ? sortState.direction : undefined}
                onSort={requestSort}
              />
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading ? (
            SKELETON_ROW_KEYS.map((key) => (
              <TableRow key={key}>
                <TableCell colSpan={COLUMNS.length}>
                  <Skeleton aria-label="Loading transactions">
                    <SkeletonItem />
                  </Skeleton>
                </TableCell>
              </TableRow>
            ))
          ) : sortedRows.length === 0 ? (
            <TableRow>
              <TableCell className="data-table__cell--message" colSpan={COLUMNS.length}>
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            sortedRows.map((row) => (
              <TableRow key={`${row.kind}-${row.id}`}>
                <DataTableCell label="Date">{formatShortDateUtc(row.date)}</DataTableCell>
                <DataTableCell label="Description">
                  <TruncatedText text={row.description} />
                </DataTableCell>
                <DataTableCell label="Type">{kindLabel(row.kind)}</DataTableCell>
                <DataTableCell label="Category">{row.categoryName}</DataTableCell>
                <DataTableCell label="Counterpart">{row.counterpart}</DataTableCell>
                <DataTableCell label="Amount" className="data-table__col--numeric">
                  {formatSignedN2(row.amount)}
                </DataTableCell>
                <DataTableCell label="Round-up" className="data-table__col--numeric">
                  {row.roundUp == null ? '' : formatSignedN2(row.roundUp)}
                </DataTableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  )
}
