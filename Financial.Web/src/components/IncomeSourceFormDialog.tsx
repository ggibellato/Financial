import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
  Switch,
} from '@fluentui/react-components'
import type { IncomeSourceDto } from '../api/types'
import { getErrorMessage } from '../utils/formatters'

const GROUP_OPTIONS = ['Salary', 'DividendoJuros', 'NonReportable']

interface IncomeSourceFormDialogProps {
  incomeSource: IncomeSourceDto | null
  onCancel: () => void
  onSubmit: (name: string, group: string, isActive: boolean, autoSplitToReserve: boolean) => Promise<unknown>
}

export default function IncomeSourceFormDialog({ incomeSource, onCancel, onSubmit }: IncomeSourceFormDialogProps) {
  const isEditing = incomeSource !== null
  const [name, setName] = useState(incomeSource?.name ?? '')
  const [group, setGroup] = useState(incomeSource?.group ?? GROUP_OPTIONS[0])
  const [isActive, setIsActive] = useState(incomeSource?.isActive ?? true)
  const [autoSplitToReserve, setAutoSplitToReserve] = useState(incomeSource?.autoSplitToReserve ?? false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedName = name.trim()
  const validationMessage = trimmedName.length === 0 ? 'Name is required.' : ''
  const canSubmit = validationMessage.length === 0 && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit(trimmedName, group, isActive, autoSplitToReserve)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The income source could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Income Source' : 'Create Income Source'}</DialogTitle>
          <DialogContent>
            <Field
              label="Name"
              required
              validationState={validationMessage ? 'error' : 'none'}
              validationMessage={validationMessage}
            >
              <Input value={name} onChange={(e) => setName(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field label="Group">
              <Select value={group} onChange={(e) => setGroup(e.target.value)} disabled={isSaving}>
                {GROUP_OPTIONS.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </Select>
            </Field>

            <Field label="Active">
              <Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} disabled={isSaving} />
            </Field>

            <Field label="Auto-split to reserve">
              <Switch
                checked={autoSplitToReserve}
                onChange={(e) => setAutoSplitToReserve(e.target.checked)}
                disabled={isSaving}
              />
            </Field>

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={() => void handleSubmit()} disabled={!canSubmit}>
              Save
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
