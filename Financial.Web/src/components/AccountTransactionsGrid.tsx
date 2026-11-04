import type { MouseEvent, ReactNode } from 'react'
import {
  Button,
  Skeleton,
  SkeletonItem,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Tooltip,
} from '@fluentui/react-components'
import { CalendarArrowRightRegular } from '@fluentui/react-icons'
import type { AccountTransactionDto, AccountTransactionOperationDto } from '../api/types'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import DataTableCell from './grid/DataTableCell'
import RowActionsCell from './grid/RowActionsCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'
import { canMoveToStatement, kindLabel, operationPath, recordNoun, rowKey, statementPath } from './accountActions'

interface TransactionColumn {
  key: string
  header: string
  numeric?: boolean
  accessor: SortAccessor<AccountTransactionDto>
  render: (row: AccountTransactionDto) => ReactNode
}

const DATE_COLUMN: TransactionColumn = { key: 'date', header: 'Date', accessor: (row) => row.date, render: (row) => formatShortDateUtc(row.date) }

const DESCRIPTION_COLUMN: TransactionColumn = {
  key: 'description',
  header: 'Description',
  accessor: (row) => row.description,
  render: (row) => <TruncatedText text={row.description} />,
}

const COUNTERPART_COLUMN: TransactionColumn = {
  key: 'counterpart',
  header: 'Counterpart',
  accessor: (row) => row.counterpart,
  render: (row) => <TruncatedText text={row.counterpart} />,
}

const AMOUNT_COLUMN: TransactionColumn = {
  key: 'amount',
  header: 'Amount',
  numeric: true,
  accessor: (row) => row.amount,
  render: (row) => formatSignedN2(row.amount),
}

const ACCOUNT_COLUMNS: TransactionColumn[] = [
  DATE_COLUMN,
  DESCRIPTION_COLUMN,
  { key: 'kind', header: 'Type', accessor: (row) => kindLabel(row.kind), render: (row) => kindLabel(row.kind) },
  { key: 'category', header: 'Category', accessor: (row) => row.categoryName, render: (row) => <TruncatedText text={row.categoryName} /> },
  COUNTERPART_COLUMN,
  AMOUNT_COLUMN,
  {
    key: 'roundUp',
    header: 'Round-up',
    numeric: true,
    accessor: (row) => row.roundUp,
    render: (row) => (row.roundUp == null ? '' : formatSignedN2(row.roundUp)),
  },
]

const POT_COLUMNS: TransactionColumn[] = [DATE_COLUMN, { ...COUNTERPART_COLUMN, header: 'Pot' }, DESCRIPTION_COLUMN, AMOUNT_COLUMN]

const ACCESSORS = Object.fromEntries(ACCOUNT_COLUMNS.map((column) => [column.key, column.accessor]))

interface AccountTransactionsGridProps {
  transactions: AccountTransactionDto[]
  showsPotColumns?: boolean
  isLoading?: boolean
  emptyMessage: string
  dateHeader?: string
  isEditDisabled?: (row: AccountTransactionDto) => boolean
  onEdit: (row: AccountTransactionDto, button: HTMLButtonElement) => void
  onDelete: (row: AccountTransactionDto) => void
  onMove?: (row: AccountTransactionDto, button: HTMLButtonElement) => void
  isMoveDisabled?: boolean
  onOpenStatement?: (path: string) => void
  onOpenOperation?: (operation: AccountTransactionOperationDto, link: HTMLAnchorElement) => void
}

export default function AccountTransactionsGrid({
  transactions,
  showsPotColumns = false,
  isLoading = false,
  emptyMessage,
  dateHeader = 'Date',
  isEditDisabled,
  onEdit,
  onDelete,
  onMove,
  isMoveDisabled = false,
  onOpenStatement,
  onOpenOperation,
}: AccountTransactionsGridProps) {
  const columns = showsPotColumns ? POT_COLUMNS : ACCOUNT_COLUMNS
  const columnCount = columns.length + 1
  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, ACCESSORS, DATE_DESC_SORT)
  const headerOf = (column: TransactionColumn) => (column.key === 'date' ? dateHeader : column.header)
  const actionClassName = onMove ? 'data-table__col--action data-table__col--action-triple' : 'data-table__col--action'
  const renderCell = (column: TransactionColumn, row: AccountTransactionDto) => {
    if (column.key !== 'description') return column.render(row)
    if (row.statement) {
      const path = statementPath(row.statement)
      const open = onOpenStatement && (() => onOpenStatement(path))
      return <TruncatedText text={row.description} to={path} onClick={(event) => followLink(event, open)} />
    }
    if (row.operation && showsPotColumns) {
      const { operation } = row
      return (
        <TruncatedText
          text={row.description}
          to={operationPath(operation)}
          onClick={(event) => {
            const link = event.currentTarget
            followLink(event, onOpenOperation && (() => onOpenOperation(operation, link)))
          }}
        />
      )
    }
    return column.render(row)
  }
  const followLink = (event: MouseEvent<HTMLAnchorElement>, open: (() => void) | undefined) => {
    if (!open || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return
    event.preventDefault()
    open()
  }

  return (
    <div className="account-screen__grid-scroll">
      <Table className="data-table" aria-label="Transactions">
        <TableHeader>
          <TableRow>
            {columns.map((column) => (
              <SortableColumnHeader
                key={column.key}
                label={headerOf(column)}
                columnKey={column.key}
                numeric={column.numeric}
                sortDirection={sortState?.columnKey === column.key ? sortState.direction : undefined}
                onSort={requestSort}
              />
            ))}
            <TableHeaderCell className={actionClassName} />
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading ? (
            <TableRow>
              <TableCell colSpan={columnCount}>
                <Skeleton aria-label="Loading transactions" className="account-screen__grid-skeleton">
                  <SkeletonItem />
                  <SkeletonItem />
                  <SkeletonItem />
                </Skeleton>
              </TableCell>
            </TableRow>
          ) : sortedRows.length === 0 ? (
            <TableRow>
              <TableCell className="data-table__cell--message" colSpan={columnCount}>
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            sortedRows.map((row) => (
              <TableRow key={rowKey(row)}>
                {columns.map((column) => (
                  <DataTableCell
                    key={column.key}
                    label={headerOf(column)}
                    className={column.numeric ? 'data-table__col--numeric' : undefined}
                  >
                    {renderCell(column, row)}
                  </DataTableCell>
                ))}
                {row.canEdit ? (
                  <RowActionsCell
                    className={actionClassName}
                    itemName={recordNoun(row.kind)}
                    editDisabled={isEditDisabled?.(row) ?? false}
                    onEdit={(button) => onEdit(row, button)}
                    onDelete={() => onDelete(row)}
                  >
                    {onMove && canMoveToStatement(row) && (
                      <Tooltip content="Move to a later statement" relationship="inaccessible">
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<CalendarArrowRightRegular />}
                          aria-label={`Move ${recordNoun(row.kind)} to a later statement`}
                          disabled={isMoveDisabled}
                          onClick={(event) => onMove(row, event.currentTarget)}
                        />
                      </Tooltip>
                    )}
                  </RowActionsCell>
                ) : (
                  <TableCell className={actionClassName} />
                )}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  )
}
