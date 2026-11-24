import { useState } from 'react'
import { Field, Input, Select, Textarea } from '@fluentui/react-components'
import type { RecurringBillDto } from '../api/types'
import EntityFormDialog from './EntityFormDialog'
import { useDialogSubmit } from '../hooks/useDialogSubmit'
import { useEntityForm } from '../hooks/useEntityForm'
import { DUE_DAY_MESSAGE, parseDueDay } from '../utils/recurringBillRules'

const AREA_OPTIONS = ['Brasil', 'UK']
const STATUS_OPTIONS = ['Unset', 'Scheduled', 'Paid']

function valueErrorOf(value: string, parsedValue: number): string {
  if (value.trim().length === 0) return 'Value is required.'
  if (Number.isNaN(parsedValue)) return 'Value must be a number.'
  return ''
}

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
  const { isSaving, error, submit } = useDialogSubmit('The recurring bill could not be saved.')
  const form = useEntityForm({ dueDay, description, value, area, note, nitNumber, minimumWageValue, status })

  const parsedDueDay = parseDueDay(dueDay)
  const parsedValue = Number(value)
  const trimmedDescription = description.trim()

  const dueDayError = parsedDueDay === null ? DUE_DAY_MESSAGE : ''
  const descriptionError = trimmedDescription.length === 0 ? 'Description is required.' : ''
  const valueError = valueErrorOf(value, parsedValue)

  const handleSubmit = () => {
    form.revealValidation()
    if (parsedDueDay === null || descriptionError || valueError) return
    void submit(() =>
      onSubmit({
        dueDay: parsedDueDay,
        description: trimmedDescription,
        value: parsedValue,
        area,
        note,
        nitNumber: nitNumber.trim().length === 0 ? null : nitNumber.trim(),
        minimumWageValue: minimumWageValue.trim().length === 0 ? null : Number(minimumWageValue),
        status,
      }),
    )
  }

  return (
    <EntityFormDialog
      title={isEditing ? 'Edit recurring bill' : 'New recurring bill'}
      saveLabel={isEditing ? 'Save' : 'Add recurring bill'}
      error={error}
      isSaving={isSaving}
      isDirty={form.isDirty}
      onSave={handleSubmit}
      onCancel={onCancel}
    >
      <Field
        label="Due day"
        required
        {...form.fieldValidation(dueDayError)}
      >
        <Input type="number" value={dueDay} onChange={(e) => setDueDay(e.target.value)} />
      </Field>

      <Field label="Area">
        <Select value={area} onChange={(e) => setArea(e.target.value)}>
          {AREA_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </Select>
      </Field>

      <Field
        label="Description"
        required
        {...form.fieldValidation(descriptionError)}
      >
        <Input value={description} onChange={(e) => setDescription(e.target.value)} />
      </Field>

      <Field
        label="Value"
        required
        {...form.fieldValidation(valueError)}
      >
        <Input type="number" value={value} onChange={(e) => setValue(e.target.value)} />
      </Field>

      <Field label="Note">
        <Textarea value={note} onChange={(e) => setNote(e.target.value)} />
      </Field>

      <Field label="NIT number">
        <Input value={nitNumber} onChange={(e) => setNitNumber(e.target.value)} />
      </Field>

      <Field label="Minimum wage value">
        <Input
          type="number"
          value={minimumWageValue}
          onChange={(e) => setMinimumWageValue(e.target.value)}
        />
      </Field>

      {isEditing && (
        <Field label="Status">
          <Select value={status} onChange={(e) => setStatus(e.target.value)}>
            {STATUS_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </Select>
        </Field>
      )}
    </EntityFormDialog>
  )
}
