import { Button, Field, Input, MessageBar, MessageBarBody, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow, Text } from '@fluentui/react-components'
import { EditRegular } from '@fluentui/react-icons'
import type { InvestmentSnapshotDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useFieldError } from '../hooks/useFieldError'
import { useInvestmentSnapshots } from '../hooks/useInvestmentSnapshots'
import { formatN2 } from '../utils/formatters'
import './InvestmentSnapshotsPage.css'

function SnapshotColumns() {
  return (
    <colgroup>
      <col className="investment-snapshots-page__col-actions" />
      <col />
      <col className="investment-snapshots-page__col-value" />
    </colgroup>
  )
}

interface SnapshotRowProps {
  snapshot: InvestmentSnapshotDto
  onEdit: (snapshot: InvestmentSnapshotDto) => void
}

function SnapshotRow({ snapshot, onEdit }: SnapshotRowProps) {
  const label = snapshot.isLiability ? `${snapshot.accountName} (liability)` : snapshot.accountName

  return (
    <TableRow>
      <TableCell>
        <Button
          appearance="subtle"
          size="small"
          icon={<EditRegular />}
          aria-label="Edit snapshot"
          onClick={() => onEdit(snapshot)}
        />
      </TableCell>
      <TableCell>{label}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(snapshot.value)}</TableCell>
    </TableRow>
  )
}

export default function InvestmentSnapshotsPage() {
  const {
    monthInputValue,
    setMonthInputValue,
    snapshots,
    totalValue,
    isLoading,
    error,
    retry,
    editingId,
    editValue,
    isSaving,
    saveError,
    saveErrorField,
    setEditValue,
    showEditForm,
    cancelEdit,
    saveEdit,
  } = useInvestmentSnapshots()

  const isEditing = editingId !== null
  const styles = useFormPanelStyles()
  const fieldError = useFieldError(saveError, saveErrorField)

  const snapshotAccessors: Record<string, SortAccessor<InvestmentSnapshotDto>> = {
    account: (snapshot) => snapshot.accountName,
    value: (snapshot) => snapshot.value,
  }
  const { sortedRows: sortedSnapshots, sortState, requestSort } = useSortableRows(snapshots, snapshotAccessors)

  return (
    <div className="investment-snapshots-page">
      <div className="investment-snapshots-page__month-picker">
        <label htmlFor="investment-snapshots-month">Month</label>
        <input
          id="investment-snapshots-month"
          type="month"
          value={monthInputValue}
          onChange={(e) => setMonthInputValue(e.target.value)}
        />
      </div>

      {isEditing && (
        <div className={styles.panel}>
          <Text as="h2" weight="semibold" size={400}>
            Edit Snapshot
          </Text>

          <div className={styles.grid}>
            <Field
              label="Value"
              required
              validationState={fieldError('editValue') ? 'error' : 'none'}
              validationMessage={fieldError('editValue')}
            >
              <Input type="number" step="0.01" min="0" value={editValue} onChange={(e) => setEditValue(e.target.value)} />
            </Field>
          </div>

          <div className={styles.actions}>
            <Button appearance="primary" disabled={isSaving} onClick={saveEdit}>
              {isSaving ? 'Saving...' : 'Save'}
            </Button>
            <Button appearance="secondary" onClick={cancelEdit}>
              Cancel
            </Button>
          </div>

          {saveErrorField === null && saveError && (
            <MessageBar intent="error">
              <MessageBarBody>{saveError}</MessageBarBody>
            </MessageBar>
          )}
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="investment-snapshots-page__content">
          <section className="investment-snapshots-page__section">
            <Table className="investment-snapshots-page__table data-table">
              <SnapshotColumns />
              <TableHeader>
                <TableRow>
                  <TableHeaderCell />
                  <SortableColumnHeader
                    label="Account"
                    columnKey="account"
                    sortDirection={sortState?.columnKey === 'account' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                  <SortableColumnHeader
                    label="Value"
                    columnKey="value"
                    numeric
                    sortDirection={sortState?.columnKey === 'value' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                </TableRow>
              </TableHeader>
              <TableBody>
                {sortedSnapshots.map((snapshot) => (
                  <SnapshotRow key={snapshot.id} snapshot={snapshot} onEdit={showEditForm} />
                ))}
              </TableBody>
            </Table>
          </section>
        </div>
      )}

      {!isLoading && !error && (
        <Table className="investment-snapshots-page__table investment-snapshots-page__totals-table data-table">
          <SnapshotColumns />
          <TableBody>
            <TableRow className="investment-snapshots-page__totals-row">
              <TableCell />
              <TableCell>Total (net of liabilities)</TableCell>
              <TableCell className="data-table__col--numeric">{formatN2(totalValue)}</TableCell>
            </TableRow>
          </TableBody>
        </Table>
      )}
    </div>
  )
}
