import type { ReactNode } from 'react'
import { Skeleton, SkeletonItem, Table, TableBody, TableCell, TableHeader, TableRow } from '@fluentui/react-components'
import type { AccountTransactionDto } from '../api/types'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'

const KIND_LABELS: Record<string, string> = {
  CardCharge: 'Card charge',
  TransferIn: 'Transfer in',
  TransferOut: 'Transfer out',
}

function kindLabel(kind: string): string {
  return KIND_LABELS[kind] ?? kind
}

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
            <TableRow>
              <TableCell colSpan={COLUMNS.length}>
                <Skeleton aria-label="Loading transactions" className="account-screen__grid-skeleton">
                  <SkeletonItem />
                  <SkeletonItem />
                  <SkeletonItem />
                </Skeleton>
              </TableCell>
            </TableRow>
          ) : sortedRows.length === 0 ? (
            <TableRow>
              <TableCell className="data-table__cell--message" colSpan={COLUMNS.length}>
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            sortedRows.map((row) => (
              <TableRow key={`${row.kind}-${row.id}`}>
                {COLUMNS.map((column) => (
                  <DataTableCell
                    key={column.key}
                    label={column.header}
                    className={column.numeric ? 'data-table__col--numeric' : undefined}
                  >
                    {column.render(row)}
                  </DataTableCell>
                ))}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  )
}
