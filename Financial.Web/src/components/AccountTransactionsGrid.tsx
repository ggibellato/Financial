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
  Text,
  Tooltip,
} from '@fluentui/react-components'
import { CalendarArrowRightRegular, DeleteRegular } from '@fluentui/react-icons'
import type { AccountTransactionDto, AccountTransactionOperationDto } from '../api/types'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatCurrency, formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import DataTableCell from './grid/DataTableCell'
import RowActionsCell from './grid/RowActionsCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'
import { canMoveToStatement, isCreditRefund, isOlderIncomeLine, kindLabel, operationPath, PAID_BY_YOU, recordNoun, rowKey, statementPath } from './accountActions'

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

const NO_CONVERTED_VALUE = '—'

const CONVERTED_COLUMN: TransactionColumn = {
  key: 'converted',
  header: 'Converted',
  numeric: true,
  accessor: (row) => row.settlement?.convertedValue,
  render: ({ settlement }) =>
    settlement?.convertedValue != null && settlement.convertedCurrency
      ? formatCurrency(settlement.convertedValue, settlement.convertedCurrency)
      : NO_CONVERTED_VALUE,
}

function settlementColumns(otherParticipant: string, showsConverted: boolean): TransactionColumn[] {
  const paidByLabel = (row: AccountTransactionDto) => (row.settlement?.paidBy === PAID_BY_YOU ? PAID_BY_YOU : otherParticipant)
  return [
    DATE_COLUMN,
    DESCRIPTION_COLUMN,
    { key: 'paidBy', header: 'Paid by', accessor: paidByLabel, render: (row) => <TruncatedText text={paidByLabel(row)} /> },
    {
      ...AMOUNT_COLUMN,
      accessor: (row) => Math.abs(row.amount),
      render: (row) => formatCurrency(Math.abs(row.amount), row.currency),
    },
    ...(showsConverted ? [CONVERTED_COLUMN] : []),
    { key: 'note', header: 'Note', accessor: (row) => row.settlement?.note || null, render: (row) => <TruncatedText text={row.settlement?.note} /> },
  ]
}

type ColumnSelection = { columns?: 'account' | 'pots' } | { columns: 'settlement'; otherParticipant: string; showsConverted: boolean }

function columnsOf(selection: ColumnSelection): TransactionColumn[] {
  if (selection.columns === 'settlement') return settlementColumns(selection.otherParticipant, selection.showsConverted)
  return selection.columns === 'pots' ? POT_COLUMNS : ACCOUNT_COLUMNS
}

const BLANK_OPERATION_LINK = 'Deposit from an income'

function actionColumnClass(hasHint: boolean, hasMove: boolean): string {
  if (hasHint) return 'data-table__col--action data-table__col--action-wide'
  return hasMove ? 'data-table__col--action data-table__col--action-triple' : 'data-table__col--action'
}

type AccountTransactionsGridProps = ColumnSelection & {
  accountId: string
  transactions: AccountTransactionDto[]
  isLoading?: boolean
  emptyMessage: string
  dateHeader?: string
  isEditDisabled?: (row: AccountTransactionDto) => boolean
  onEdit: (row: AccountTransactionDto, button: HTMLButtonElement) => void
  onDelete: (row: AccountTransactionDto) => void
  canDeleteCreditRefunds?: boolean
  onMove?: (row: AccountTransactionDto, button: HTMLButtonElement) => void
  isMoveDisabled?: boolean
  onOpenStatement?: (path: string) => void
  onOpenOperation?: (operation: AccountTransactionOperationDto, link: HTMLAnchorElement) => void
}

export default function AccountTransactionsGrid({
  accountId,
  transactions,
  isLoading = false,
  emptyMessage,
  dateHeader = 'Date',
  isEditDisabled,
  onEdit,
  onDelete,
  canDeleteCreditRefunds = false,
  onMove,
  isMoveDisabled = false,
  onOpenStatement,
  onOpenOperation,
  ...columnSelection
}: AccountTransactionsGridProps) {
  const columns = columnsOf(columnSelection)
  const columnCount = columns.length + 1
  const accessors = Object.fromEntries(columns.map((column) => [column.key, column.accessor]))
  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, accessors, DATE_DESC_SORT)
  const headerOf = (column: TransactionColumn) => (column.key === 'date' ? dateHeader : column.header)
  const actionClassName = actionColumnClass(sortedRows.some(isOlderIncomeLine), onMove !== undefined)
  const renderCell = (column: TransactionColumn, row: AccountTransactionDto) => {
    if (column.key !== 'description') return column.render(row)
    if (row.statement) {
      const path = statementPath(row.statement)
      const open = onOpenStatement && (() => onOpenStatement(path))
      return <TruncatedText text={row.description} to={path} onClick={(event) => followLink(event, open)} />
    }
    if (row.operation) {
      const { operation } = row
      return (
        <TruncatedText
          text={row.description || BLANK_OPERATION_LINK}
          to={operationPath(operation, accountId)}
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
  const renderActions = (row: AccountTransactionDto) => {
    if (row.canEdit) {
      return (
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
      )
    }
    if (isCreditRefund(row) && canDeleteCreditRefunds) {
      return (
        <TableCell className={actionClassName}>
          <div className="data-table__actions-cell">
            <Button appearance="subtle" size="small" icon={<DeleteRegular />} aria-label={`Delete ${recordNoun(row.kind)}`} onClick={() => onDelete(row)} />
          </div>
        </TableCell>
      )
    }
    return <TableCell className={actionClassName}>{isOlderIncomeLine(row) && <Text size={200}>Edit from the income</Text>}</TableCell>
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
                {renderActions(row)}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  )
}
