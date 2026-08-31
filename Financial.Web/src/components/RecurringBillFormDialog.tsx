import { useState } from 'react'
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
  Textarea,
} from '@fluentui/react-components'
import type { RecurringBillDto } from '../api/types'
import { useFormPanelStyles } from './formPanelStyles'
import { getErrorMessage } from '../utils/formatters'

const AREA_OPTIONS = ['Brasil', 'UK']
const STATUS_OPTIONS = ['Unset', 'Scheduled', 'Paid']

export interface RecurringBillFormValues {
  dueDay: number
  description: string
  value: number
  area: string
  note: string
  nitNumber: string | null
  minimumWageValue: number | null
  status: string
}

interface RecurringBillFormDialogProps {
  recurringBill: RecurringBillDto | null
  onCancel: () => void
  onSubmit: (values: RecurringBillFormValues) => Promise<unknown>
}

export default function RecurringBillFormDialog({ recurringBill, onCancel, onSubmit }: RecurringBillFormDialogProps) {
  const styles = useFormPanelStyles()
  const isEditing = recurringBill !== null
  const [dueDay, setDueDay] = useState(String(recurringBill?.dueDay ?? ''))
  const [description, setDescription] = useState(recurringBill?.description ?? '')
  const [value, setValue] = useState(String(recurringBill?.value ?? ''))
  const [area, setArea] = useState(recurringBill?.area ?? AREA_OPTIONS[0])
  const [note, setNote] = useState(recurringBill?.note ?? '')
  const [nitNumber, setNitNumber] = useState(recurringBill?.nitNumber ?? '')
  const [minimumWageValue, setMinimumWageValue] = useState(
    recurringBill?.minimumWageValue !== null && recurringBill?.minimumWageValue !== undefined
      ? String(recurringBill.minimumWageValue)
      : '',
  )
  const [status, setStatus] = useState(recurringBill?.status ?? STATUS_OPTIONS[0])
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const parsedDueDay = Number(dueDay)
  const parsedValue = Number(value)
  const trimmedDescription = description.trim()

  const dueDayError = dueDay.trim().length === 0 || !Number.isInteger(parsedDueDay) || parsedDueDay < 1 || parsedDueDay > 31
    ? 'Due day must be between 1 and 31.'
    : ''
  const descriptionError = trimmedDescription.length === 0 ? 'Description is required.' : ''
  const valueError = value.trim().length === 0 || Number.isNaN(parsedValue) ? 'Value must be a number.' : ''

  const canSubmit = !dueDayError && !descriptionError && !valueError && !isSaving

  const handleSubmit = async () => {
    if (!canSubmit) return

    setIsSaving(true)
    setError(null)
    try {
      await onSubmit({
        dueDay: parsedDueDay,
        description: trimmedDescription,
        value: parsedValue,
        area,
        note,
        nitNumber: nitNumber.trim().length === 0 ? null : nitNumber.trim(),
        minimumWageValue: minimumWageValue.trim().length === 0 ? null : Number(minimumWageValue),
        status,
      })
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'The recurring bill could not be saved.'))
      setIsSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={(_, data) => { if (!data.open) onCancel() }}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>{isEditing ? 'Edit Recurring Bill' : 'Create Recurring Bill'}</DialogTitle>
          <DialogContent>
            <Field
              label="Due Day"
              required
              validationState={dueDayError ? 'error' : 'none'}
              validationMessage={dueDayError}
            >
              <Input type="number" value={dueDay} onChange={(e) => setDueDay(e.target.value)} disabled={isSaving} autoFocus />
            </Field>

            <Field
              label="Description"
              required
              validationState={descriptionError ? 'error' : 'none'}
              validationMessage={descriptionError}
            >
              <Input value={description} onChange={(e) => setDescription(e.target.value)} disabled={isSaving} />
            </Field>

            <Field
              label="Value"
              required
              validationState={valueError ? 'error' : 'none'}
              validationMessage={valueError}
            >
              <Input type="number" value={value} onChange={(e) => setValue(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="Area">
              <Select value={area} onChange={(e) => setArea(e.target.value)} disabled={isSaving}>
                {AREA_OPTIONS.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </Select>
            </Field>

            <Field label="Note">
              <Textarea value={note} onChange={(e) => setNote(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="NIT Number">
              <Input value={nitNumber} onChange={(e) => setNitNumber(e.target.value)} disabled={isSaving} />
            </Field>

            <Field label="Minimum Wage Value">
              <Input
                type="number"
                value={minimumWageValue}
                onChange={(e) => setMinimumWageValue(e.target.value)}
                disabled={isSaving}
              />
            </Field>

            {isEditing && (
              <Field label="Status">
                <Select value={status} onChange={(e) => setStatus(e.target.value)} disabled={isSaving}>
                  {STATUS_OPTIONS.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </Select>
              </Field>
            )}

            {error && (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            )}
          </DialogContent>
          <div className={styles.actions}>
            <Button appearance="primary" onClick={() => void handleSubmit()} disabled={!canSubmit}>
              Save
            </Button>
            <Button appearance="secondary" onClick={onCancel} disabled={isSaving}>
              Cancel
            </Button>
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
