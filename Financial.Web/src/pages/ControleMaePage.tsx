import { Button, Field, Input, MessageBar, MessageBarBody, Select, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular, EditRegular } from '@fluentui/react-icons'
import type { MaeLedgerEntryDto } from '../api/types'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import SortableColumnHeader from '../components/grid/SortableColumnHeader'
import { useFormPanelStyles } from '../components/formPanelStyles'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { useFieldError } from '../hooks/useFieldError'
import { useControleMae } from '../hooks/useControleMae'
import { confirmThenRun } from '../utils/confirmThenRun'
import { formatN2, formatShortDate } from '../utils/formatters'
import './ControleMaePage.css'

function LedgerColumns() {
  return (
    <colgroup>
      <col className="controle-mae-page__col-actions" />
      <col className="controle-mae-page__col-actions" />
      <col className="controle-mae-page__col-date" />
      <col className="controle-mae-page__col-description" />
      <col className="controle-mae-page__col-note" />
      <col className="controle-mae-page__col-value" />
      <col className="controle-mae-page__col-value" />
    </colgroup>
  )
}

interface EntryRowProps {
  entry: MaeLedgerEntryDto
  isDeleting: boolean
  onEdit: (entry: MaeLedgerEntryDto) => void
  onDelete: (id: string) => void
}

function EntryRow({ entry, isDeleting, onEdit, onDelete }: EntryRowProps) {
  return (
    <tr>
      <td>
        <Button
          appearance="subtle"
          size="small"
          icon={<EditRegular />}
          aria-label="Edit entry"
          onClick={() => onEdit(entry)}
        />
      </td>
      <td>
        <Button
          appearance="subtle"
          size="small"
          icon={<DeleteRegular />}
          aria-label={isDeleting ? 'Deleting entry' : 'Delete entry'}
          disabled={isDeleting}
          onClick={() =>
            confirmThenRun(`Delete "${entry.description}"? This removes it for good.`, () => onDelete(entry.id))
          }
        />
      </td>
      <td>{formatShortDate(entry.date)}</td>
      <td>{entry.description}</td>
      <td>{entry.note}</td>
      <td className="data-table__col--numeric">{entry.brlValue !== null ? formatN2(entry.brlValue) : '—'}</td>
      <td className="data-table__col--numeric">{entry.gbpValue !== null ? formatN2(entry.gbpValue) : '—'}</td>
    </tr>
  )
}

export default function ControleMaePage() {
  const {
    fromDateInputValue,
    setFromDateInputValue,
    entries,
    totals,
    isLoading,
    error,
    retry,
    isCreateFormOpen,
    createDate,
    createDescription,
    createNote,
    createSourceCurrency,
    createSourceValue,
    isCreating,
    createError,
    createErrorField,
    showCreateForm,
    cancelCreateForm,
    setCreateField,
    submitCreate,
    editingId,
    editBrlValue,
    editGbpValue,
    isSaving,
    saveError,
    saveErrorField,
    setEditField,
    showEditForm,
    cancelEdit,
    saveEdit,
    deletingId,
    deleteError,
    deleteEntry,
  } = useControleMae()

  const isEditing = editingId !== null
  const isFormVisible = isCreateFormOpen || isEditing
  const styles = useFormPanelStyles()
  const createFieldError = useFieldError(createError, createErrorField)
  const editFieldError = useFieldError(saveError, saveErrorField)

  const entryAccessors: Record<string, SortAccessor<MaeLedgerEntryDto>> = {
    date: (entry) => new Date(entry.date),
    description: (entry) => entry.description,
    note: (entry) => entry.note,
    brl: (entry) => entry.brlValue,
    gbp: (entry) => entry.gbpValue,
  }
  const { sortedRows: sortedEntries, sortState, requestSort } = useSortableRows(entries, entryAccessors)

  return (
    <div className="controle-mae-page">
      <div className="controle-mae-page__header">
        <div className="controle-mae-page__month-picker">
          <label htmlFor="controle-mae-from-date">From</label>
          <input
            id="controle-mae-from-date"
            type="date"
            value={fromDateInputValue}
            onChange={(e) => setFromDateInputValue(e.target.value)}
          />
        </div>
        <Button appearance="primary" icon={<AddRegular />} onClick={showCreateForm}>
          New Entry
        </Button>
      </div>

      {deleteError && <p className="controle-mae-page__error">{deleteError}</p>}

      {isFormVisible && (
        <div className={styles.panel}>
          <Text as="h2" weight="semibold" size={400}>
            {isEditing ? 'Edit Entry' : 'New Entry'}
          </Text>

          {isEditing ? (
            <div className={styles.grid}>
              <Field
                label="BRL"
                validationState={editFieldError('editBrlValue') ? 'error' : 'none'}
                validationMessage={editFieldError('editBrlValue')}
              >
                <Input
                  type="number"
                  step="0.01"
                  value={editBrlValue}
                  onChange={(e) => setEditField('editBrlValue', e.target.value)}
                />
              </Field>

              <Field
                label="GBP"
                validationState={editFieldError('editGbpValue') ? 'error' : 'none'}
                validationMessage={editFieldError('editGbpValue')}
              >
                <Input
                  type="number"
                  step="0.01"
                  value={editGbpValue}
                  onChange={(e) => setEditField('editGbpValue', e.target.value)}
                />
              </Field>
            </div>
          ) : (
            <div className={styles.grid}>
              <Field
                label="Date"
                required
                validationState={createFieldError('createDate') ? 'error' : 'none'}
                validationMessage={createFieldError('createDate')}
              >
                <Input type="date" value={createDate} onChange={(e) => setCreateField('createDate', e.target.value)} />
              </Field>

              <Field label="Currency">
                <Select
                  value={createSourceCurrency}
                  onChange={(e) => setCreateField('createSourceCurrency', e.target.value)}
                >
                  <option value="BRL">BRL</option>
                  <option value="GBP">GBP</option>
                </Select>
              </Field>

              <div className={styles.spanTwo}>
                <Field
                  label="Description"
                  required
                  validationState={createFieldError('createDescription') ? 'error' : 'none'}
                  validationMessage={createFieldError('createDescription')}
                >
                  <Input
                    value={createDescription}
                    onChange={(e) => setCreateField('createDescription', e.target.value)}
                  />
                </Field>
              </div>

              <div className={styles.spanTwo}>
                <Field label="Note">
                  <Input value={createNote} onChange={(e) => setCreateField('createNote', e.target.value)} />
                </Field>
              </div>

              <Field
                label="Value"
                required
                validationState={createFieldError('createSourceValue') ? 'error' : 'none'}
                validationMessage={createFieldError('createSourceValue')}
              >
                <Input
                  type="number"
                  step="0.01"
                  value={createSourceValue}
                  onChange={(e) => setCreateField('createSourceValue', e.target.value)}
                />
              </Field>
            </div>
          )}

          <div className={styles.actions}>
            <Button appearance="primary" disabled={isEditing ? isSaving : isCreating} onClick={isEditing ? saveEdit : submitCreate}>
              {isEditing ? (isSaving ? 'Saving...' : 'Save') : isCreating ? 'Saving...' : 'Add Entry'}
            </Button>
            <Button appearance="secondary" onClick={isEditing ? cancelEdit : cancelCreateForm}>
              Cancel
            </Button>
          </div>

          {(isEditing ? saveError : createError) && (
            <MessageBar intent="error">
              <MessageBarBody>{isEditing ? saveError : createError}</MessageBarBody>
            </MessageBar>
          )}
        </div>
      )}

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={error} onRetry={retry} />
      ) : (
        <div className="controle-mae-page__content">
          <section className="controle-mae-page__section">
            <table className="controle-mae-page__table data-table">
              <LedgerColumns />
              <thead>
                <tr>
                  <th />
                  <th />
                  <SortableColumnHeader
                    label="Date"
                    columnKey="date"
                    sortDirection={sortState?.columnKey === 'date' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                  <SortableColumnHeader
                    label="Description"
                    columnKey="description"
                    sortDirection={sortState?.columnKey === 'description' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                  <SortableColumnHeader
                    label="Note"
                    columnKey="note"
                    sortDirection={sortState?.columnKey === 'note' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                  <SortableColumnHeader
                    label="BRL"
                    columnKey="brl"
                    numeric
                    sortDirection={sortState?.columnKey === 'brl' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                  <SortableColumnHeader
                    label="GBP"
                    columnKey="gbp"
                    numeric
                    sortDirection={sortState?.columnKey === 'gbp' ? sortState.direction : undefined}
                    onSort={requestSort}
                  />
                </tr>
              </thead>
              <tbody>
                {sortedEntries.map((entry) => (
                  <EntryRow
                    key={entry.id}
                    entry={entry}
                    isDeleting={deletingId === entry.id}
                    onEdit={showEditForm}
                    onDelete={deleteEntry}
                  />
                ))}
              </tbody>
            </table>
          </section>
        </div>
      )}

      {!isLoading && !error && (
        <table className="controle-mae-page__table controle-mae-page__totals-table data-table">
          <LedgerColumns />
          <tbody>
            <tr className="controle-mae-page__totals-row">
              <td />
              <td />
              <td colSpan={3}>Total (all entries)</td>
              <td className="data-table__col--numeric">{totals ? formatN2(totals.totalBrlValue) : '—'}</td>
              <td className="data-table__col--numeric">{totals ? formatN2(totals.totalGbpValue) : '—'}</td>
            </tr>
          </tbody>
        </table>
      )}
    </div>
  )
}
