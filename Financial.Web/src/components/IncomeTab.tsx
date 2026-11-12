import { useEffect, useRef, type ReactNode } from 'react'
import {
  Button,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
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
import { AddRegular, DismissRegular } from '@fluentui/react-icons'
import type { IncomeDto } from '../api/types'
import { NO_FILTER_MATCH_MESSAGE, useColumnFilters } from '../hooks/useColumnFilters'
import { useFocusReloadedContent } from '../hooks/useFocusReloadedContent'
import { INCOME_GONE_WARNING, useIncomeTab } from '../hooks/useIncomeTab'
import { useMediaQuery } from '../hooks/useMediaQuery'
import { useRowEditRoute } from '../hooks/useRowEditRoute'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import { DATE_DESC_SORT, useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { MOBILE_MEDIA_QUERY } from '../styles/breakpoints'
import { isFocusLost } from '../utils/focus'
import { formatLongMonthYear, formatN2, formatShortDateUtc, toPence } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import ConfirmDeleteDialog from './ConfirmDeleteDialog'
import DiscardChangesDialog from './DiscardChangesDialog'
import ErrorState from './ErrorState'
import IncomeForm from './IncomeForm'
import TruncatedText from './TruncatedText'
import ActionsColumnHeader from './grid/ActionsColumnHeader'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import DataTableCell from './grid/DataTableCell'
import FilteredRowsNotice from './grid/FilteredRowsNotice'
import RowActionsCell from './grid/RowActionsCell'
import SortableColumnHeader from './grid/SortableColumnHeader'

const useStyles = makeStyles({
  toolbar: {
    display: 'flex',
    flexWrap: 'wrap',
    flexShrink: 0,
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  formArea: {
    marginBottom: tokens.spacingVerticalM,
  },
})

interface Column {
  key: string
  header: string
  numeric?: boolean
  accessor: SortAccessor<IncomeDto>
}

const NO_BANK = 'No bank'
const GRID_LABEL = 'Incomes'
const FIRST_FIELD_SELECTOR = 'input, select, textarea'
const SAVE_IN_PROGRESS = 'Wait for the save to finish.'
const EDITED_INCOME_NOT_FOUND = 'This income is no longer in this month.'

const SOURCE_HEADER = 'Source'

const sourceName = (income: IncomeDto) => income.incomeSourceName
const bankName = (income: IncomeDto) => income.bankName ?? NO_BANK

const COLUMNS: Column[] = [
  { key: 'date', header: 'Date', accessor: (income) => income.date },
  { key: 'source', header: SOURCE_HEADER, accessor: sourceName },
  { key: 'bank', header: 'Bank', accessor: bankName },
  { key: 'description', header: 'Description', accessor: (income) => income.description },
  { key: 'gross', header: 'Gross', numeric: true, accessor: (income) => income.grossValue },
  { key: 'net', header: 'Net', numeric: true, accessor: (income) => income.netValue },
]

const SORT_ACCESSORS = Object.fromEntries(COLUMNS.map((column) => [column.key, column.accessor]))

const FILTER_ACCESSORS = { source: sourceName, bank: bankName }

const FILTER_KEYS = Object.keys(FILTER_ACCESSORS)

const itemName = ({ date, incomeSourceName, description }: IncomeDto) =>
  `income ${formatShortDateUtc(date)} ${incomeSourceName}${description ? `, ${description}` : ''}`

function deleteMessage(income: IncomeDto | null): string {
  return income?.hasLinkedDeposit
    ? 'Delete this income? Its Pots deposit is deleted with it. This cannot be undone.'
    : 'Delete this income? This cannot be undone.'
}

function renderCell(column: Column, income: IncomeDto): ReactNode {
  switch (column.key) {
    case 'date':
      return formatShortDateUtc(income.date)
    case 'source':
      return income.incomeSourceName
    case 'bank':
      return <TruncatedText text={bankName(income)} />
    case 'description':
      return <TruncatedText text={income.description} />
    case 'gross':
      return orMissing(income.grossValue, formatN2, 'No gross value')
    default:
      return formatN2(income.netValue)
  }
}

interface IncomeTabProps {
  year: number
  month: number
}

export default function IncomeTab({ year, month }: IncomeTabProps) {
  const styles = useStyles()
  const {
    incomes,
    incomeSources,
    banks,
    isLoading,
    error,
    retry,
    isGoneNoticeShown,
    dismissGoneNotice,
    incomeForm,
    incomeDelete,
    discardPrompt,
    openCreate: openCreateForm,
    openEdit: openEditForm,
    closeForm,
  } = useIncomeTab(year, month)
  const sectionRef = useRef<HTMLElement>(null)
  const tableRef = useRef<HTMLTableElement>(null)
  const formAreaRef = useRef<HTMLDivElement>(null)
  const newIncomeRef = useRef<HTMLButtonElement>(null)
  const openerRef = useRef<HTMLElement | null>(null)
  const recordRowFocus = useRowFocusRecovery(sectionRef, newIncomeRef, isLoading)
  useFocusReloadedContent(sectionRef, isLoading)
  const isStackedLayout = useMediaQuery(MOBILE_MEDIA_QUERY)
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered, isFiltered, clearFilters, filterSummary } =
    useColumnFilters(incomes, FILTER_ACCESSORS)
  const { sortedRows, sortState, requestSort } = useSortableRows(filteredRows, SORT_ACCESSORS, DATE_DESC_SORT)
  const formKey = incomeForm.isIncomeFormOpen ? (incomeForm.editingIncomeId ?? 'new') : null
  const editRoute = useRowEditRoute({ rows: incomes, keyOf: (income) => income.id, isLoaded: !isLoading && !error, open: openEditForm })

  useEffect(() => {
    if (formKey) {
      formAreaRef.current?.querySelector<HTMLElement>(FIRST_FIELD_SELECTOR)?.focus()
      return
    }
    if (!isFocusLost()) return
    if (openerRef.current?.isConnected) openerRef.current.focus()
    else newIncomeRef.current?.focus()
  }, [formKey])

  if (error) return <ErrorState message={error} onRetry={retry} />

  const openCreate = (button: HTMLElement) => {
    openerRef.current = button
    openCreateForm()
  }

  const openEdit = (income: IncomeDto, button: HTMLElement) => {
    openerRef.current = button
    openEditForm(income)
  }

  const dismissWarning = () => {
    dismissGoneNotice()
    editRoute.dismiss()
    newIncomeRef.current?.focus()
  }

  const clearAllFilters = () => {
    clearFilters()
    sectionRef.current?.querySelector<HTMLButtonElement>(`button[aria-label="Filter by ${SOURCE_HEADER}"]`)?.focus()
  }

  const renderMessage = (message: string) => (
    <TableRow>
      <TableCell className="data-table__cell--message" colSpan={COLUMNS.length + 1}>
        {message}
      </TableCell>
    </TableRow>
  )

  const renderBody = () => {
    if (isLoading) {
      return (
        <TableRow>
          <TableCell colSpan={COLUMNS.length + 1}>
            <Skeleton aria-label="Loading incomes">
              <SkeletonItem />
              <SkeletonItem />
              <SkeletonItem />
            </Skeleton>
          </TableCell>
        </TableRow>
      )
    }
    if (incomes.length === 0) return renderMessage(`No income in ${formatLongMonthYear(year, month)}.`)
    if (sortedRows.length === 0) return renderMessage(NO_FILTER_MATCH_MESSAGE)
    return sortedRows.map((income) => (
      <TableRow key={income.id}>
        {COLUMNS.map((column) => (
          <DataTableCell key={column.key} label={column.header} className={column.numeric ? 'data-table__col--numeric' : undefined}>
            {renderCell(column, income)}
          </DataTableCell>
        ))}
        <RowActionsCell
          itemName={itemName(income)}
          editDisabled={incomeForm.isSavingIncome || incomeDelete.isDeleting}
          deleteBlockedReason={incomeForm.isSavingIncome ? SAVE_IN_PROGRESS : undefined}
          isDeleting={incomeDelete.isDeleting && incomeDelete.target?.id === income.id}
          isOtherDeleting={incomeDelete.isDeleting && incomeDelete.target?.id !== income.id}
          onEdit={(button) => openEdit(income, button)}
          onDelete={() => incomeDelete.request(income)}
        />
      </TableRow>
    ))
  }

  return (
    <section ref={sectionRef} className="monthly-page__section monthly-page__section--grid" onFocus={recordRowFocus}>
      <div className={styles.toolbar}>
        <Button ref={newIncomeRef} appearance="primary" icon={<AddRegular />} onClick={(event) => openCreate(event.currentTarget)}>
          New income
        </Button>
        {isFiltered && !isStackedLayout && (
          <Button appearance="primary" onClick={clearAllFilters}>
            Clear filters
          </Button>
        )}
      </div>
      {(isGoneNoticeShown || editRoute.isUnavailable) && (
        <MessageBar intent="warning">
          <MessageBarBody>{isGoneNoticeShown ? INCOME_GONE_WARNING : EDITED_INCOME_NOT_FOUND}</MessageBarBody>
          <MessageBarActions
            containerAction={<Button appearance="transparent" aria-label="Dismiss warning" icon={<DismissRegular />} onClick={dismissWarning} />}
          />
        </MessageBar>
      )}
      {incomeForm.isIncomeFormOpen && (
        <div ref={formAreaRef} className={styles.formArea}>
          <IncomeForm
            key={formKey}
            isEditing={incomeForm.isIncomeEditing}
            date={incomeForm.incomeDate}
            incomeSource={incomeForm.incomeSource}
            grossValue={incomeForm.incomeGrossValue}
            netValue={incomeForm.incomeNetValue}
            bank={incomeForm.incomeBank}
            description={incomeForm.incomeDescription}
            splitToReserve={incomeForm.incomeSplitToReserve === 'true'}
            potsAccountId={incomeForm.incomePotsAccountId}
            potsAccounts={incomeForm.potsAccounts}
            potsAccountsStatus={incomeForm.potsAccountsStatus}
            onRetryPotsAccounts={incomeForm.retryPotsAccounts}
            funding={incomeForm.incomeFunding}
            banks={banks}
            incomeSources={incomeSources}
            isSaving={incomeForm.isSavingIncome}
            saveError={incomeForm.saveIncomeError}
            saveErrorFields={incomeForm.saveIncomeErrorFields}
            onFieldChange={incomeForm.setIncomeField}
            onSave={incomeForm.submitIncome}
            onCancel={closeForm}
          />
        </div>
      )}
      <FilteredRowsNotice {...filterSummary} tableRef={tableRef} />
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
      {sortedRows.length > 0 && <IncomeFooter incomes={sortedRows} allCount={incomes.length} isFiltered={isFiltered} />}

      <ConfirmDeleteDialog
        open={incomeDelete.isOpen}
        entityLabel="income"
        message={deleteMessage(incomeDelete.target)}
        isBusy={incomeDelete.isDeleting}
        error={incomeDelete.error}
        onConfirm={incomeDelete.confirm}
        onCancel={incomeDelete.cancel}
      />
      <DiscardChangesDialog open={discardPrompt.isPromptOpen} onKeepEditing={discardPrompt.keepEditing} onDiscard={discardPrompt.discard} />
    </section>
  )
}

interface IncomeFooterProps {
  incomes: IncomeDto[]
  allCount: number
  isFiltered: boolean
}

function IncomeFooter({ incomes, allCount, isFiltered }: IncomeFooterProps) {
  const totalNet = incomes.reduce((sum, income) => sum + toPence(income.netValue), 0) / 100
  return (
    <p className="monthly-page__section-total" role="group" aria-label={`Totals for ${GRID_LABEL}`}>
      <strong>{incomes.length}</strong>
      {isFiltered && (
        <>
          {' of '}
          <strong>{allCount}</strong>
        </>
      )}
      {allCount === 1 ? ' income' : ' incomes'}
      {' · Total net '}
      <strong>{formatN2(totalNet)}</strong>
    </p>
  )
}
