import { Button, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { CorporateActionDto } from '../api/types'
import CorporateActionForm from './CorporateActionForm'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useCorporateActions } from '../hooks/useCorporateActions'
import { confirmThenRun } from '../utils/confirmThenRun'
import { formatShortDate } from '../utils/formatters'
import './CorporateActionsTab.css'

const TYPE_LABELS: Record<string, string> = {
  Split: 'Split',
  Merger: 'Merger',
  SpinOff: 'Spin-off',
}

function typeLabel(type: string): string {
  return TYPE_LABELS[type] ?? type
}

function formatRatioFactor(factor: number): string {
  return `×${factor.toFixed(4).replace(/\.?0+$/, '')}`
}

function resultingChange(record: CorporateActionDto): string {
  if (record.type === 'Split' && record.ratioFactor !== null) {
    return `Quantity/average cost rescaled ${formatRatioFactor(record.ratioFactor)}`
  }
  return '—'
}

const SORT_ACCESSORS: Record<string, SortAccessor<CorporateActionDto>> = {
  date: (record) => new Date(record.effectiveDate),
  type: (record) => typeLabel(record.type),
}

interface CorporateActionRowProps {
  record: CorporateActionDto
  affectedAssetName: string
  onEdit: (record: CorporateActionDto) => void
  onDelete: (id: string) => void
}

function CorporateActionRow({ record, affectedAssetName, onEdit, onDelete }: CorporateActionRowProps) {
  return (
    <TableRow>
      <DataTableCell label="Date">{formatShortDate(record.effectiveDate)}</DataTableCell>
      <DataTableCell label="Type">{typeLabel(record.type)}</DataTableCell>
      <DataTableCell label="Affected/Linked Asset">{record.linkedAssetName ?? affectedAssetName}</DataTableCell>
      <DataTableCell label="Resulting Change">{resultingChange(record)}</DataTableCell>
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

export default function CorporateActionsTab() {
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
    isSaving,
    saveError,
    saveErrorFields,
    deleteError,
    showNewForm,
    showEditForm,
    cancelForm,
    setFormField,
    saveForm,
    deleteCorporateAction,
  } = useCorporateActions()

  const { sortedRows, sortState, requestSort } = useSortableRows(corporateActions, SORT_ACCESSORS)

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
    <div className="corporate-actions-tab">
      <div className="corporate-actions-tab__toolbar">
        <Button appearance="primary" icon={<AddRegular />} onClick={showNewForm}>
          New Corporate Action
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
          isSaving={isSaving}
          saveError={saveError}
          saveErrorFields={saveErrorFields}
          onFieldChange={setFormField}
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
                <TableHeaderCell className="data-table__col--action" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((record) => (
                <CorporateActionRow
                  key={record.id}
                  record={record}
                  affectedAssetName={affectedAssetName}
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
