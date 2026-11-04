import { useRef } from 'react'
import { Button, Field, Input, Switch, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import type { PotBalanceDto } from '../api/types'
import { potNameError, potSplitError, useManagePots, type PotRow, type PotRowChange } from '../hooks/useManagePots'
import { formatN2To4 } from '../utils/formatters'
import DiscardChangesDialog from './DiscardChangesDialog'
import EntityFormDialog from './EntityFormDialog'
import DataTableCell from './grid/DataTableCell'
import './ManagePotsDialog.css'

interface ManagePotsDialogProps {
  potsAccountId: string
  accountName: string
  initialPots: readonly PotBalanceDto[]
  onSaved: () => void
  onClose: () => void
}

function totalGuidance(isTotalRight: boolean, hasPots: boolean, hasActivePots: boolean): string {
  if (isTotalRight) return ''
  if (!hasPots) return 'Add a pot. Active pots must total exactly 100% to save.'
  if (!hasActivePots) return 'Keep at least one pot active. Active pots must total exactly 100% to save.'
  return 'Active pots must total exactly 100% to save.'
}

interface PotRowFieldsProps {
  row: PotRow
  isDisabled: boolean
  focusName: boolean
  onChange: (change: PotRowChange) => void
  onRemove: () => void
}

function PotRowFields({ row, isDisabled, focusName, onChange, onRemove }: PotRowFieldsProps) {
  const nameError = potNameError(row)
  const splitError = potSplitError(row)

  return (
    <TableRow>
      <DataTableCell label="Name">
        <Field className="manage-pots__field" required validationState={nameError ? 'error' : 'none'} validationMessage={nameError}>
          <Input
            className="manage-pots__input"
            aria-label={`Name, ${row.label}`}
            value={row.name}
            autoFocus={focusName}
            disabled={isDisabled}
            onChange={(event) => onChange({ name: event.target.value })}
          />
        </Field>
      </DataTableCell>
      <DataTableCell label="Split %" className="manage-pots__split">
        <Field className="manage-pots__field" required validationState={splitError ? 'error' : 'none'} validationMessage={splitError}>
          <Input
            className="manage-pots__input"
            aria-label={`Split percentage, ${row.label}`}
            type="number"
            min={0}
            max={100}
            step="any"
            contentAfter="%"
            value={row.splitPercentage}
            disabled={isDisabled}
            onChange={(event) => onChange({ splitPercentage: event.target.value })}
          />
        </Field>
      </DataTableCell>
      <DataTableCell label="Active" className="manage-pots__active">
        <Switch
          aria-label={`Active, ${row.label}`}
          checked={row.isActive}
          disabled={isDisabled}
          onChange={(event) => onChange({ isActive: event.target.checked })}
        />
      </DataTableCell>
      {row.id === null ? (
        <DataTableCell label="Remove" className="manage-pots__remove">
          <Button
            appearance="subtle"
            size="small"
            icon={<DeleteRegular />}
            aria-label={`Remove ${row.label}`}
            disabled={isDisabled}
            onClick={onRemove}
          />
        </DataTableCell>
      ) : (
        <TableCell className="manage-pots__remove" />
      )}
    </TableRow>
  )
}

export default function ManagePotsDialog({ potsAccountId, accountName, initialPots, onSaved, onClose }: ManagePotsDialogProps) {
  const form = useManagePots(potsAccountId, initialPots, onSaved)
  const addPotButton = useRef<HTMLButtonElement | null>(null)
  const guidance = totalGuidance(form.isTotalRight, form.rows.length > 0, form.hasActivePots)

  const cancel = () => {
    if (!form.isSaving) form.discardPrompt.confirmDiscard(onClose)
  }

  const removeNewPot = (key: string) => {
    form.removeNewPot(key)
    addPotButton.current?.focus()
  }

  return (
    <>
      <EntityFormDialog
        title="Manage pots"
        error={form.error}
        isSaving={form.isSaving}
        canSave={form.canSave}
        onSave={form.save}
        onCancel={cancel}
      >
        <div className="manage-pots">
          <Text>
            Pots of <strong>{accountName}</strong> save together. A pot can't be deleted: turn off Active to retire it, and it keeps its balance and history.
          </Text>
          {form.rows.length === 0 ? (
            <Text>This account has no pots yet.</Text>
          ) : (
            <Table aria-label="Pots" className="data-table manage-pots__table">
              <TableHeader>
                <TableRow>
                  <TableHeaderCell>Name</TableHeaderCell>
                  <TableHeaderCell className="manage-pots__split">Split %</TableHeaderCell>
                  <TableHeaderCell className="manage-pots__active">Active</TableHeaderCell>
                  <TableHeaderCell className="manage-pots__remove" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {form.rows.map((row) => (
                  <PotRowFields
                    key={row.key}
                    row={row}
                    isDisabled={form.isSaving}
                    focusName={row.key === form.lastAddedKey}
                    onChange={(change) => form.changeRow(row.key, change)}
                    onRemove={() => removeNewPot(row.key)}
                  />
                ))}
              </TableBody>
            </Table>
          )}
          <div>
            <Button ref={addPotButton} appearance="primary" icon={<AddRegular />} disabled={form.isSaving} onClick={form.addPot}>
              Add pot
            </Button>
          </div>
          <p className="manage-pots__total" role="status">
            Active pots total: <strong>{formatN2To4(form.activeTotal)}%</strong>
            {guidance && `. ${guidance}`}
          </p>
        </div>
      </EntityFormDialog>
      <DiscardChangesDialog
        open={form.discardPrompt.isPromptOpen}
        onKeepEditing={form.discardPrompt.keepEditing}
        onDiscard={form.discardPrompt.discard}
      />
    </>
  )
}
