import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Button, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular } from '@fluentui/react-icons'
import EmptyState from './EmptyState'
import type { CorporateActionDto } from '../api/types'
import ActionErrorAlert from './ActionErrorAlert'
import ConfirmDeleteDialog from './ConfirmDeleteDialog'
import CorporateActionForm from './CorporateActionForm'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import StatusBadge from './StatusBadge'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { usePendingDelete } from '../hooks/usePendingDelete'
import { useSortableRows, DATE_DESC_SORT, type SortAccessor } from '../hooks/useSortableRows'
import { useCorporateActions } from '../hooks/useCorporateActions'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import { corporateActionTypeLabel } from '../utils/corporateActionTypeLabel'
import { formatN2, formatShortDate } from '../utils/formatters'
import './CorporateActionsTab.css'
import TruncatedText from './TruncatedText'
import ActionsColumnHeader from './grid/ActionsColumnHeader'
import RowActionsCell from './grid/RowActionsCell'
import { rowDeleteState, type RowDeleteState } from './grid/rowDeleteState'
import MissingValue from './MissingValue'

function formatRatioFactor(factor: number): string {
  return `×${factor.toFixed(4).replace(/\.?0+$/, '')}`
}

function resultingChange(record: CorporateActionDto): ReactNode {
  if (record.type === 'Split' && record.ratioFactor !== null) {
    return `Quantity/average cost rescaled ${formatRatioFactor(record.ratioFactor)}`
  }
  if (record.type === 'Merger') {
    if (record.role === 'Source') {
      return 'Position closed and converted'
    }
    if (record.role === 'Target') {
      return record.convertedQuantity !== null
        ? `+${formatN2(record.convertedQuantity)} units received`
        : 'Units received from merger'
    }
    return <MissingValue label="None" />
  }
  if (record.type === 'SpinOff') {
    if (record.role === 'Parent') {
      return 'Cost basis reduced by spin-off'
    }
    if (record.role === 'New') {
      return record.convertedQuantity !== null
        ? `+${formatN2(record.convertedQuantity)} units received`
        : 'Units received from spin-off'
    }
    return <MissingValue label="None" />
  }
  return <MissingValue label="None" />
}

function corporateActionName(record: CorporateActionDto, affectedAssetName: string): string {
  return `corporate action ${corporateActionTypeLabel(record.type)} ${record.linkedAssetName ?? affectedAssetName}, ${formatShortDate(record.effectiveDate)}`
}

const SORT_ACCESSORS: Record<string, SortAccessor<CorporateActionDto>> = {
  date: (record) => new Date(record.effectiveDate),
  type: (record) => corporateActionTypeLabel(record.type),
}

interface CorporateActionRowProps {
  record: CorporateActionDto
  affectedAssetName: string
  isHighlighted: boolean
  deleteState: RowDeleteState
  onEdit: (record: CorporateActionDto) => void
  onDelete: (record: CorporateActionDto) => void
}

function CorporateActionRow({ record, affectedAssetName, isHighlighted, deleteState, onEdit, onDelete }: CorporateActionRowProps) {
  return (
    <TableRow
      data-corporate-action-id={record.id}
      tabIndex={-1}
      className={isHighlighted ? 'corporate-actions-tab__row corporate-actions-tab__row--highlighted' : undefined}
    >
      <DataTableCell label="Date">{formatShortDate(record.effectiveDate)}</DataTableCell>
      <DataTableCell label="Type">{corporateActionTypeLabel(record.type)}</DataTableCell>
      <DataTableCell label="Affected/linked asset"><TruncatedText text={record.linkedAssetName ?? affectedAssetName} /></DataTableCell>
      <DataTableCell label="Resulting change">{resultingChange(record)}</DataTableCell>
      <DataTableCell label="Tax status">
        {record.calculationStatus ? <StatusBadge status={record.calculationStatus} /> : <MissingValue label="None" />}
      </DataTableCell>
      <RowActionsCell
        itemName={corporateActionName(record, affectedAssetName)}
        {...deleteState}
        onEdit={() => onEdit(record)}
        onDelete={() => onDelete(record)}
      />
    </TableRow>
  )
}

interface CorporateActionsTabProps {
  focusRecordId?: string | null
}

export default function CorporateActionsTab({ focusRecordId }: CorporateActionsTabProps) {
  const {
    asset,
    corporateActions,
    isLoading,
    error,
    retry,
    isFormVisible,
    editingId,
    formEffectiveDate,
    formType,
    formRatioNumerator,
    formRatioDenominator,
    formNote,
    formStep,
    formTargetAsset,
    formExchangeRatio,
    formCashInLieu,
    formNewAsset,
    formQuantityReceived,
    formAllocationPercentage,
    isSaving,
    saveError,
    saveErrorFields,
    deletingId,
    deleteError,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    setTargetAsset,
    setNewAsset,
    advanceToConfirm,
    backToFields,
    saveForm,
    deleteCorporateAction,
  } = useCorporateActions()

  const { sortedRows, sortState, requestSort } = useSortableRows(corporateActions, SORT_ACCESSORS, DATE_DESC_SORT)

  const containerRef = useRef<HTMLDivElement>(null)
  const [highlightedId, setHighlightedId] = useState<string | null>(null)

  useEffect(() => {
    if (!focusRecordId || isLoading) return
    if (!corporateActions.some((record) => record.id === focusRecordId)) return
    const row = containerRef.current?.querySelector<HTMLElement>(
      `[data-corporate-action-id="${focusRecordId}"]`,
    )
    if (!row) return
    row.scrollIntoView({ behavior: 'smooth', block: 'center' })
    ;(row.querySelector<HTMLElement>('button') ?? row).focus()
    setHighlightedId(focusRecordId)
    const timer = setTimeout(() => setHighlightedId(null), 2000)
    return () => clearTimeout(timer)
  }, [focusRecordId, isLoading, corporateActions])

  const deletion = usePendingDelete((row: CorporateActionDto) => deleteCorporateAction(row.id))
  const regionRef = useRef<HTMLDivElement | null>(null)
  const newCorporateActionRef = useRef<HTMLButtonElement | null>(null)
  const recordRowFocus = useRowFocusRecovery(regionRef, newCorporateActionRef, isLoading)

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  const affectedAssetName = asset?.name ?? ''

  return (
    <div className="corporate-actions-tab" ref={containerRef}>
      <div className="corporate-actions-tab__toolbar">
        <Button ref={newCorporateActionRef} appearance="primary" icon={<AddRegular />} onClick={showNewForm}>
          New corporate action
        </Button>
      </div>

      {isFormVisible && (
        <CorporateActionForm
          editingId={editingId}
          formEffectiveDate={formEffectiveDate}
          formType={formType}
          formRatioNumerator={formRatioNumerator}
          formRatioDenominator={formRatioDenominator}
          formNote={formNote}
          formStep={formStep}
          formTargetAsset={formTargetAsset}
          formExchangeRatio={formExchangeRatio}
          formCashInLieu={formCashInLieu}
          formNewAsset={formNewAsset}
          formQuantityReceived={formQuantityReceived}
          formAllocationPercentage={formAllocationPercentage}
          sourceAssetName={affectedAssetName}
          sourceQuantity={asset?.quantity ?? 0}
          sourceCostBasis={asset?.costOfUnitsHeld ?? 0}
          isSaving={isSaving}
          saveError={saveError}
          saveErrorFields={saveErrorFields}
          onFieldChange={setFormField}
          onTargetAssetChange={setTargetAsset}
          onNewAssetChange={setNewAsset}
          onAdvanceToConfirm={advanceToConfirm}
          onBackToFields={backToFields}
          onSave={saveForm}
          onCancel={cancelForm}
        />
      )}

      {corporateActions.length === 0 ? (
        <EmptyState>
          No corporate actions yet — nothing has changed this holding through a split, merger, or spin-off.
        </EmptyState>
      ) : (
        <div ref={regionRef} className="corporate-actions-tab__table-wrapper" onFocus={recordRowFocus}>
          <Table className="corporate-actions-tab__table data-table" aria-label="Corporate actions">
            <TableHeader>
              <TableRow>
                <SortableColumnHeader
                  label="Date"
                  columnKey="date"
                  sortDirection={sortState?.columnKey === 'date' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Type"
                  columnKey="type"
                  sortDirection={sortState?.columnKey === 'type' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <TableHeaderCell>Affected/linked asset</TableHeaderCell>
                <TableHeaderCell>Resulting change</TableHeaderCell>
                <TableHeaderCell>Tax status</TableHeaderCell>
                <ActionsColumnHeader />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((record) => (
                <CorporateActionRow
                  key={record.id}
                  record={record}
                  affectedAssetName={affectedAssetName}
                  isHighlighted={record.id === highlightedId}
                  deleteState={rowDeleteState(deletingId, record.id)}
                  onEdit={showEditForm}
                  onDelete={deletion.request}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <ActionErrorAlert message={deleteError} />

      {deletion.pendingRow && (
        <ConfirmDeleteDialog
          entityLabel="corporate action"
          message={`Delete the ${corporateActionName(deletion.pendingRow, affectedAssetName)}? This cannot be undone.`}
          onConfirm={deletion.confirm}
          onCancel={deletion.cancel}
        />
      )}
    </div>
  )
}
