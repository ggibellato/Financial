import { useRef } from 'react'
import { Button, Field, Input, Switch, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import type { PotBalanceDto } from '../api/types'
import { potNameError, potSplitError, useManagePots, type PotRow, type PotRowChange } from '../hooks/useManagePots'
import { formatN2To4 } from '../utils/formatters'
import EmptyState from './EmptyState'
import EntityFormDialog from './EntityFormDialog'
import './ManagePotsDialog.css'

interface ManagePotsDialogProps {
  potsAccountId: string
  accountName: string
  initialPots: readonly PotBalanceDto[]
  onSaved: () => void
  onClose: () => void
}

interface PotRowFieldsProps {
  row: PotRow
  onChange: (change: PotRowChange) => void
  onRemove: () => void
}

function PotRowFields({ row, onChange, onRemove }: PotRowFieldsProps) {
  const nameError = potNameError(row)
  const splitError = potSplitError(row)
  const displayName = row.name.trim() || row.label

  return (
    <div className="manage-pots__row" role="group" aria-label={`Pot ${displayName}`}>
      <Field label="Name" required validationState={nameError ? 'error' : 'none'} validationMessage={nameError}>
        <Input
          className="manage-pots__input"
          value={row.name}
          autoFocus={row.id === null}
          onChange={(event) => onChange({ name: event.target.value })}
        />
      </Field>
      <Field
        className="manage-pots__split"
        label="Split %"
        required
        validationState={splitError ? 'error' : 'none'}
        validationMessage={splitError}
      >
        <Input
          className="manage-pots__input"
          type="number"
          min={0}
          max={100}
          step="any"
          value={row.splitPercentage}
          onChange={(event) => onChange({ splitPercentage: event.target.value })}
        />
      </Field>
      <Switch
        className="manage-pots__active"
        label="Active"
        labelPosition="above"
        checked={row.isActive}
        onChange={(event) => onChange({ isActive: event.target.checked })}
      />
      {row.id === null && (
        <Button
          className="manage-pots__remove"
          appearance="subtle"
          size="small"
          icon={<DeleteRegular />}
          aria-label={`Remove ${displayName}`}
          onClick={onRemove}
        />
      )}
    </div>
  )
}

export default function ManagePotsDialog({ potsAccountId, accountName, initialPots, onSaved, onClose }: ManagePotsDialogProps) {
  const form = useManagePots(potsAccountId, initialPots, onSaved)
  const addPotButton = useRef<HTMLButtonElement | null>(null)

  const removeNewPot = (key: string) => {
    form.removeNewPot(key)
    addPotButton.current?.focus()
  }

  return (
    <EntityFormDialog
      title="Manage pots"
      error={form.error}
      isSaving={form.isSaving}
      canSave={form.isDirty}
      isDirty={form.isDirty}
      onSave={form.save}
      onCancel={onClose}
    >
      <div className="manage-pots">
        <Text>
          Pots of <strong>{accountName}</strong> save together. A pot can't be deleted: turn off Active to retire it, and it keeps its balance and history.
        </Text>
        <div>
          <Button ref={addPotButton} appearance="primary" icon={<AddRegular />} onClick={form.addPot}>
            Add pot
          </Button>
        </div>
        {form.rows.length === 0 && <EmptyState>This account has no pots yet.</EmptyState>}
        {form.rows.map((row) => (
          <PotRowFields
            key={row.key}
            row={row}
            onChange={(change) => form.changeRow(row.key, change)}
            onRemove={() => removeNewPot(row.key)}
          />
        ))}
        <p className="manage-pots__total">
          Active pots total: <strong>{formatN2To4(form.activeTotal)}%</strong>.{' '}
          <span role="status">{form.guidance}</span>
        </p>
      </div>
    </EntityFormDialog>
  )
}
