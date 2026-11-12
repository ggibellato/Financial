import type { ReactNode } from 'react'
import {
  Button,
  Skeleton,
  SkeletonItem,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableRow,
  Text,
  Tooltip,
} from '@fluentui/react-components'
import { CalendarArrowRightRegular } from '@fluentui/react-icons'
import type { AccountTransactionDto, AccountTransactionOperationDto } from '../api/types'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { formatCurrency, formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import DataTableCell from './grid/DataTableCell'
import RowActionsCell from './grid/RowActionsCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'
import { paidByLabel } from '../utils/paidBy'
import {
  BLANK_OPERATION_DESCRIPTION,
  canMoveToStatement,
  isCreditRefund,
  isOlderIncomeLine,
  kindLabel,
  operationDeleteBlockedReason,
  potOperationNoun,
  recordNoun,
  rowKey,
  STATEMENT_PAYMENT_DELETE_REASON,
  statementPath,
  type PotOperationNoun,
} from './accountActions'
import ActionsColumnHeader from './grid/ActionsColumnHeader'
import MissingValue from './MissingValue'

function rowName(row: AccountTransactionDto): string {
  const noun = row.description ? `${recordNoun(row.kind)} ${row.description}` : recordNoun(row.kind)
  return `${noun}, ${formatShortDateUtc(row.date)}`
}

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
  render: (row) => <TruncatedText text={row.description || (row.operation ? BLANK_OPERATION_DESCRIPTION : null)} />,
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

const CONVERTED_COLUMN: TransactionColumn = {
  key: 'converted',
  header: 'Converted',
  numeric: true,
  accessor: (row) => row.settlement?.convertedValue,
  render: ({ settlement }) =>
    settlement?.convertedValue != null && settlement.convertedCurrency
      ? formatCurrency(settlement.convertedValue, settlement.convertedCurrency)
      : <MissingValue />,
}

function settlementColumns(otherParticipant: string, showsConverted: boolean): TransactionColumn[] {
  const paidByOf = (row: AccountTransactionDto) => paidByLabel(row.settlement?.paidBy, otherParticipant)
  return [
    DATE_COLUMN,
    DESCRIPTION_COLUMN,
    { key: 'paidBy', header: 'Paid by', accessor: paidByOf, render: (row) => <TruncatedText text={paidByOf(row)} /> },
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
  onOpenOperation?: (operation: AccountTransactionOperationDto, button: HTMLButtonElement) => void
  onDeleteOperation?: (operation: AccountTransactionOperationDto, noun: PotOperationNoun) => void
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
  onDeleteOperation,
  ...columnSelection
}: AccountTransactionsGridProps) {
  const columns = columnsOf(columnSelection)
  const columnCount = columns.length + 1
  const accessors = Object.fromEntries(columns.map((column) => [column.key, column.accessor]))
  const { sortedRows, sortState, requestSort } = useSortableRows(transactions, accessors, DATE_DESC_SORT)
  const headerOf = (column: TransactionColumn) => (column.key === 'date' ? dateHeader : column.header)
  const actionClassName = actionColumnClass(sortedRows.some(isOlderIncomeLine), onMove !== undefined)
  const renderActions = (row: AccountTransactionDto) => {
    const name = rowName(row)
    if (row.canEdit) {
      return (
        <RowActionsCell
          className={actionClassName}
          itemName={name}
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
                aria-label={`Move ${name} to a later statement`}
                disabled={isMoveDisabled}
                onClick={(event) => onMove(row, event.currentTarget)}
              />
            </Tooltip>
          )}
        </RowActionsCell>
      )
    }
    const { statement, operation } = row
    if (statement && onOpenStatement) {
      return (
        <RowActionsCell
          className={actionClassName}
          itemName={name}
          deleteBlockedReason={STATEMENT_PAYMENT_DELETE_REASON}
          onEdit={() => onOpenStatement(statementPath(statement))}
        />
      )
    }
    if (operation && onOpenOperation) {
      return (
        <RowActionsCell
          className={actionClassName}
          itemName={name}
          editDisabled={isEditDisabled?.(row) ?? false}
          deleteBlockedReason={operationDeleteBlockedReason(operation, accountId)}
          onEdit={(button) => onOpenOperation(operation, button)}
          onDelete={onDeleteOperation && (() => onDeleteOperation(operation, potOperationNoun(row)))}
        />
      )
    }
    if (isCreditRefund(row) && canDeleteCreditRefunds) {
      return <RowActionsCell className={actionClassName} itemName={name} onDelete={() => onDelete(row)} />
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
            <ActionsColumnHeader className={actionClassName} />
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
                    {column.render(row)}
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
