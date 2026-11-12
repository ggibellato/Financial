import { useRef, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../api/financialApiClient'
import { useAccountRowDelete } from '../hooks/useAccountRowDelete'
import { useConfirmedDelete } from '../hooks/useConfirmedDelete'
import { useFocusReloadedContent } from '../hooks/useFocusReloadedContent'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import { useSuccessToast } from '../hooks/useSuccessToast'
import {
  Badge,
  Button,
  Skeleton,
  SkeletonItem,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableRow,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import type { MonthTransactionDto } from '../api/types'
import { NO_FILTER_MATCH_MESSAGE, useColumnFilters } from '../hooks/useColumnFilters'
import { useMediaQuery } from '../hooks/useMediaQuery'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { MOBILE_MEDIA_QUERY } from '../styles/breakpoints'
import { deleteFailedMessage, formatLongMonthYear, formatMonthInputValue, formatN2, formatShortDateUtc, formatSignedN2 } from '../utils/formatters'
import { monthTransactionTotals } from '../utils/monthTransactionTotals'
import ConfirmDeleteDialog from './ConfirmDeleteDialog'
import ErrorState from './ErrorState'
import ActionsColumnHeader from './grid/ActionsColumnHeader'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import DataTableCell from './grid/DataTableCell'
import FilteredRowsNotice from './grid/FilteredRowsNotice'
import RowActionsCell from './grid/RowActionsCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import TruncatedText from './TruncatedText'
import {
  deleteConfirmation,
  deletedMessage,
  kindLabel,
  monthRowDeleteBlockedReason,
  monthRowEdit,
  operationDeleteConfirmation,
  operationDeletedMessage,
  operationNoun,
  recordNoun,
  shownDescription,
  transactionRowName,
  withdrawalOperationIds,
  type PotOperationNoun,
} from './accountActions'

const useStyles = makeStyles({
  toolbar: {
    display: 'flex',
    flexShrink: 0,
    justifyContent: 'flex-start',
    marginBottom: tokens.spacingVerticalM,
  },
  account: {
    display: 'inline-flex',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalS,
  },
})

interface Column {
  key: string
  header: string
  numeric?: boolean
  accessor: SortAccessor<MonthTransactionDto>
}

const NO_BANK = 'No bank'

const accountName = (row: MonthTransactionDto) => row.accountName ?? NO_BANK
const typeLabel = (row: MonthTransactionDto) => kindLabel(row.transaction.kind)
const categoryName = (row: MonthTransactionDto) => row.transaction.categoryName

const CATEGORY_HEADER = 'Category'

const COLUMNS: Column[] = [
  { key: 'date', header: 'Date', accessor: (row) => row.transaction.date },
  { key: 'account', header: 'Account', accessor: accountName },
  { key: 'kind', header: 'Type', accessor: typeLabel },
  { key: 'description', header: 'Description', accessor: (row) => row.transaction.description },
  { key: 'category', header: CATEGORY_HEADER, accessor: categoryName },
  { key: 'amount', header: 'Amount', numeric: true, accessor: (row) => row.transaction.amount },
]

const SORT_ACCESSORS = Object.fromEntries(COLUMNS.map((column) => [column.key, column.accessor]))

const FILTER_ACCESSORS = { account: accountName, kind: typeLabel, category: categoryName }

const FILTER_KEYS = Object.keys(FILTER_ACCESSORS)

const GRID_LABEL = 'All transactions'

const COLUMN_COUNT = COLUMNS.length + 1

function rowKey(row: MonthTransactionDto): string {
  return `${row.accountId ?? NO_BANK}-${row.transaction.kind}-${row.transaction.id}`
}

const withdrawalIdsOf = ({ accountId, transaction }: MonthTransactionDto) =>
  accountId ? withdrawalOperationIds([transaction], accountId) : new Set<string>()

const rowName = (row: MonthTransactionDto) => `${transactionRowName(row.transaction, withdrawalIdsOf(row))}, ${accountName(row)}`

interface OperationDeleteTarget {
  operationId: string
  noun: PotOperationNoun
}

interface MonthTransactionsGridProps {
  year: number
  month: number
  rows: MonthTransactionDto[]
  isLoading: boolean
  error: string | null
  onRetry: () => void
  onDeleted: () => void
}

export default function MonthTransactionsGrid({ year, month, rows, isLoading, error, onRetry, onDeleted }: MonthTransactionsGridProps) {
  const styles = useStyles()
  const navigate = useNavigate()
  const { showSuccess } = useSuccessToast()
  const gridRef = useRef<HTMLElement>(null)
  useFocusReloadedContent(gridRef, isLoading)
  const tableRef = useRef<HTMLTableElement>(null)
  const recordRowFocus = useRowFocusRecovery(gridRef, tableRef, isLoading)
  const notifyDeleted = (message: string) => {
    showSuccess(message)
    onDeleted()
  }
  const rowDelete = useAccountRowDelete((transaction) => notifyDeleted(deletedMessage(transaction)))
  const operationDelete = useConfirmedDelete<OperationDeleteTarget>(
    ({ operationId }) => apiClient.deletePotOperation(operationId),
    ({ noun }) => deleteFailedMessage(noun),
    ({ noun }) => notifyDeleted(operationDeletedMessage(noun)),
  )
  const isAnyDeleting = rowDelete.isDeleting || operationDelete.isDeleting
  const isDeletingRow = ({ transaction }: MonthTransactionDto) =>
    (rowDelete.isDeleting && rowDelete.row === transaction) ||
    (operationDelete.isDeleting && operationDelete.target?.operationId === transaction.operation?.operationId)
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered, isFiltered, clearFilters, filterSummary } =
    useColumnFilters(rows, FILTER_ACCESSORS)
  const { sortedRows, sortState, requestSort } = useSortableRows(filteredRows, SORT_ACCESSORS, DATE_DESC_SORT)
  const monthValue = formatMonthInputValue(year, month)

  if (error) return <ErrorState message={error} onRetry={onRetry} />

  const clearAllFilters = () => {
    clearFilters()
    gridRef.current?.querySelector<HTMLButtonElement>(`button[aria-label="Filter by ${CATEGORY_HEADER}"]`)?.focus()
  }

  const renderCell = (column: Column, row: MonthTransactionDto): ReactNode => {
    const { transaction } = row
    switch (column.key) {
      case 'date':
        return formatShortDateUtc(transaction.date)
      case 'account':
        if (row.accountId == null) return NO_BANK
        return (
          <span className={styles.account}>
            <TruncatedText text={row.accountName} to={`/cashflow/accounts/${row.accountId}?month=${monthValue}&tab=transactions`} />
            {!row.accountIsActive && (
              <Badge appearance="outline" color="informative">
                Inactive
              </Badge>
            )}
          </span>
        )
      case 'kind':
        return typeLabel(row)
      case 'description':
        return <TruncatedText text={shownDescription(transaction)} />
      case 'category':
        return <TruncatedText text={transaction.categoryName} />
      default:
        return `${formatSignedN2(transaction.amount)} ${transaction.currency ?? ''}`.trimEnd()
    }
  }

  const requestDelete = (row: MonthTransactionDto) => {
    const { operation } = row.transaction
    if (operation) operationDelete.request({ operationId: operation.operationId, noun: operationNoun(operation, withdrawalIdsOf(row)) })
    else rowDelete.requestDelete(row.transaction)
  }

  const renderActions = (row: MonthTransactionDto) => {
    const { path, blockedReason: editBlockedReason } = monthRowEdit(row, monthValue)
    const deleteBlockedReason = monthRowDeleteBlockedReason(row)
    const isDeleting = isDeletingRow(row)
    return (
      <RowActionsCell
        itemName={rowName(row)}
        editBlockedReason={editBlockedReason}
        deleteBlockedReason={deleteBlockedReason}
        isDeleting={isDeleting}
        isOtherDeleting={isAnyDeleting && !isDeleting}
        onEdit={() => path && navigate(path)}
        onDelete={deleteBlockedReason ? undefined : () => requestDelete(row)}
      />
    )
  }

  const renderMessage = (message: string) => (
    <TableRow>
      <TableCell className="data-table__cell--message" colSpan={COLUMN_COUNT}>
        {message}
      </TableCell>
    </TableRow>
  )

  const renderBody = () => {
    if (isLoading) {
      return (
        <TableRow>
          <TableCell colSpan={COLUMN_COUNT}>
            <Skeleton aria-label="Loading transactions">
              <SkeletonItem />
              <SkeletonItem />
              <SkeletonItem />
            </Skeleton>
          </TableCell>
        </TableRow>
      )
    }
    if (rows.length === 0) return renderMessage(`No transactions in ${formatLongMonthYear(year, month)}.`)
    if (sortedRows.length === 0) return renderMessage(NO_FILTER_MATCH_MESSAGE)
    return sortedRows.map((row) => (
      <TableRow key={rowKey(row)}>
        {COLUMNS.map((column) => (
          <DataTableCell key={column.key} label={column.header} className={column.numeric ? 'data-table__col--numeric' : undefined}>
            {renderCell(column, row)}
          </DataTableCell>
        ))}
        {renderActions(row)}
      </TableRow>
    ))
  }

  return (
    <section ref={gridRef} className="monthly-page__section monthly-page__section--grid" onFocus={recordRowFocus}>
      <FilteredRowsNotice {...filterSummary} tableRef={tableRef} />
      {isFiltered && !isStackedLayout && (
        <div className={styles.toolbar}>
          <Button appearance="primary" onClick={clearAllFilters}>
            Clear filters
          </Button>
        </div>
      )}
      <div className="monthly-page__table-scroll">
        <Table ref={tableRef} tabIndex={-1} className="data-table" aria-label={GRID_LABEL}>
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
                >
                  {FILTER_KEYS.includes(column.key) && (
                    <ColumnFilterMenu
                      columnKey={column.key}
                      label={column.header}
                      availableValues={availableValues[column.key]}
                      selectedValues={selectedValues[column.key]}
                      onToggleValue={toggleValue}
                      onToggleAll={toggleAll}
                      isFiltered={isColumnFiltered(column.key)}
                    />
                  )}
                </SortableColumnHeader>
              ))}
              <ActionsColumnHeader />
            </TableRow>
          </TableHeader>
          <TableBody>{renderBody()}</TableBody>
        </Table>
      </div>
      {!isLoading && sortedRows.length > 0 && (
        <MonthTransactionsFooter
          rows={sortedRows}
          allRowCount={rows.length}
          isFiltered={isFiltered}
          showsSpend={isColumnFiltered('category')}
        />
      )}
      <ConfirmDeleteDialog
        open={rowDelete.isOpen}
        entityLabel={recordNoun(rowDelete.row?.kind ?? '')}
        message={rowDelete.row ? deleteConfirmation(rowDelete.row) : ''}
        isBusy={rowDelete.isDeleting}
        error={rowDelete.error}
        onConfirm={rowDelete.confirm}
        onCancel={rowDelete.cancel}
      />
      <ConfirmDeleteDialog
        open={operationDelete.isOpen}
        entityLabel={operationDelete.target?.noun ?? ''}
        message={operationDelete.target ? operationDeleteConfirmation(operationDelete.target.noun) : ''}
        isBusy={operationDelete.isDeleting}
        error={operationDelete.error}
        onConfirm={operationDelete.confirm}
        onCancel={operationDelete.cancel}
      />
    </section>
  )
}

interface MonthTransactionsFooterProps {
  rows: MonthTransactionDto[]
  allRowCount: number
  isFiltered: boolean
  showsSpend: boolean
}

function MonthTransactionsFooter({ rows, allRowCount, isFiltered, showsSpend }: MonthTransactionsFooterProps) {
  const totals = monthTransactionTotals(rows)
  return (
    <p className="monthly-page__section-total" role="group" aria-label={`Totals for ${GRID_LABEL}`}>
      <strong>{rows.length}</strong>
      {isFiltered && (
        <>
          {' of '}
          <strong>{allRowCount}</strong>
        </>
      )}
      {allRowCount === 1 ? ' row' : ' rows'}
      {totals.map(({ currency, total }) => (
        <span key={`total-${currency ?? NO_BANK}`}>
          {` · ${currency ? 'Total' : 'Total (no bank)'} `}
          <strong>{formatSignedN2(total)}</strong>{currency && ` ${currency}`}
        </span>
      ))}
      {showsSpend &&
        totals.filter(({ currency }) => currency).map(({ currency, spend }) => (
          <span key={`spend-${currency}`}>
            {' · Spend '}
            <strong>{formatN2(spend)}</strong> {currency}
          </span>
        ))}
    </p>
  )
}
