import { useEffect, useRef, useState } from 'react'
import { Button, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { CorporateActionDto } from '../api/types'
import CorporateActionForm from './CorporateActionForm'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import StatusBadge from './StatusBadge'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useCorporateActions } from '../hooks/useCorporateActions'
import { confirmThenRun } from '../utils/confirmThenRun'
import { corporateActionTypeLabel } from '../utils/corporateActionTypeLabel'
import { formatN2, formatShortDate } from '../utils/formatters'
import './CorporateActionsTab.css'

function formatRatioFactor(factor: number): string {
  return `×${factor.toFixed(4).replace(/\.?0+$/, '')}`
}

function resultingChange(record: CorporateActionDto): string {
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
    return '—'
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
    return '—'
  }
  return '—'
}

const SORT_ACCESSORS: Record<string, SortAccessor<CorporateActionDto>> = {
  date: (record) => new Date(record.effectiveDate),
  type: (record) => corporateActionTypeLabel(record.type),
}

interface CorporateActionRowProps {
  record: CorporateActionDto
  affectedAssetName: string
  isHighlighted: boolean
  onEdit: (record: CorporateActionDto) => void
  onDelete: (id: string) => void
}

function CorporateActionRow({ record, affectedAssetName, isHighlighted, onEdit, onDelete }: CorporateActionRowProps) {
  return (
    <TableRow
      data-corporate-action-id={record.id}
      tabIndex={-1}
      className={isHighlighted ? 'corporate-actions-tab__row corporate-actions-tab__row--highlighted' : undefined}
    >
      <DataTableCell label="Date">{formatShortDate(record.effectiveDate)}</DataTableCell>
      <DataTableCell label="Type">{corporateActionTypeLabel(record.type)}</DataTableCell>
      <DataTableCell label="Affected/Linked Asset">{record.linkedAssetName ?? affectedAssetName}</DataTableCell>
      <DataTableCell label="Resulting Change">{resultingChange(record)}</DataTableCell>
      <DataTableCell label="Tax Status">
        {record.calculationStatus ? <StatusBadge status={record.calculationStatus} /> : '—'}
      </DataTableCell>
      <DataTableCell label="Actions" className="data-table__col--action">
        <div className="data-table__actions-cell">
          <Button
            appearance="subtle"
            size="small"
            icon={<EditRegular />}
            aria-label="Edit corporate action"
            onClick={() => onEdit(record)}
          />
          <Button
            appearance="subtle"
            size="small"
            icon={<DeleteRegular />}
            aria-label="Delete corporate action"
            onClick={() => onDelete(record.id)}
          />
        </div>
      </DataTableCell>
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

  const { sortedRows, sortState, requestSort } = useSortableRows(corporateActions, SORT_ACCESSORS)

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

  const confirmAndDelete = (id: string) =>
    confirmThenRun('Delete this corporate action?', () => deleteCorporateAction(id))

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
        <Button appearance="primary" icon={<AddRegular />} onClick={showNewForm}>
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
        <p className="corporate-actions-tab__empty">
          No corporate actions yet — nothing has changed this holding through a split, merger, or spin-off.
        </p>
      ) : (
        <div className="corporate-actions-tab__table-wrapper">
          <Table className="corporate-actions-tab__table data-table">
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
                <TableHeaderCell>Affected/Linked Asset</TableHeaderCell>
                <TableHeaderCell>Resulting Change</TableHeaderCell>
                <TableHeaderCell>Tax Status</TableHeaderCell>
                <TableHeaderCell className="data-table__col--action" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((record) => (
                <CorporateActionRow
                  key={record.id}
                  record={record}
                  affectedAssetName={affectedAssetName}
                  isHighlighted={record.id === highlightedId}
                  onEdit={showEditForm}
                  onDelete={confirmAndDelete}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {deleteError && <p className="corporate-actions-tab__delete-error">{deleteError}</p>}
    </div>
  )
}
