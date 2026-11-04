import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ReserveMovementDto } from '../api/types'
import { getSaveErrorMessage } from '../utils/formatters'
import { validateEditMovement, type EditMovementField } from './useReserva'

type PotTransactionValues = Record<EditMovementField, string>

interface PotTransactionEdit {
  movementId: string
  original: PotTransactionValues
  values: PotTransactionValues
  isSaving: boolean
  error: string | null
  errorFields: Partial<PotTransactionValues>
}

const BLANK_VALUES: PotTransactionValues = {
  editMovementBucketId: '',
  editMovementAmount: '',
  editMovementDate: '',
  editMovementDescription: '',
}

function valuesOf(movement: ReserveMovementDto): PotTransactionValues {
  return {
    editMovementBucketId: movement.bucketId,
    editMovementAmount: String(movement.amount),
    editMovementDate: movement.date,
    editMovementDescription: movement.description,
  }
}

function hasChanges({ original, values }: PotTransactionEdit): boolean {
  return (Object.keys(values) as EditMovementField[]).some((field) => values[field] !== original[field])
}

export function usePotTransactionEditForm(onSaved: () => void) {
  const [edit, setEdit] = useState<PotTransactionEdit | null>(null)

  const open = (movement: ReserveMovementDto) => {
    const values = valuesOf(movement)
    setEdit({ movementId: movement.id, original: values, values, isSaving: false, error: null, errorFields: {} })
  }

  const cancel = () => setEdit(null)

  const setField = (field: EditMovementField, value: string) =>
    setEdit((current) => current && { ...current, values: { ...current.values, [field]: value } })

  const submit = () => {
    if (!edit || edit.isSaving) return
    const errorFields = validateEditMovement(edit.values)
    if (Object.keys(errorFields).length > 0) {
      setEdit({ ...edit, error: null, errorFields })
      return
    }

    setEdit({ ...edit, isSaving: true, error: null, errorFields: {} })
    const { editMovementBucketId, editMovementAmount, editMovementDate, editMovementDescription } = edit.values
    apiClient
      .updateReserveMovement(edit.movementId, {
        bucketId: editMovementBucketId,
        amount: Number(editMovementAmount),
        date: editMovementDate,
        description: editMovementDescription,
      })
      .then(() => {
        setEdit(null)
        onSaved()
      })
      .catch((err: unknown) => {
        setEdit((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to update the pot transaction') })
      })
  }

  return {
    isOpen: edit !== null,
    isDirty: edit !== null && hasChanges(edit),
    isSaving: edit?.isSaving ?? false,
    values: edit?.values ?? BLANK_VALUES,
    error: edit?.error ?? null,
    errorFields: edit?.errorFields ?? {},
    open,
    cancel,
    setField,
    submit,
  }
}
