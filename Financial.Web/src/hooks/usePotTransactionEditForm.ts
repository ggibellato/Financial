import { useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ReserveMovementDto } from '../api/types'
import { getSaveErrorMessage } from '../utils/formatters'
import {
  EDIT_MOVEMENT_FIELDS,
  editMovementValues,
  toMovementUpdate,
  validateEditMovement,
  type EditMovementField,
  type EditMovementValues,
} from '../utils/movementEdit'
import { hasChangedFields, type FieldErrors } from './inlineFormSlice'

interface PotTransactionEdit {
  movement: ReserveMovementDto
  values: EditMovementValues
  isSaving: boolean
  error: string | null
  errorFields: FieldErrors<EditMovementField>
}

export function usePotTransactionEditForm(onSaved: () => void) {
  const [edit, setEdit] = useState<PotTransactionEdit | null>(null)

  const open = (movement: ReserveMovementDto) =>
    setEdit({ movement, values: editMovementValues(movement), isSaving: false, error: null, errorFields: {} })

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
    void apiClient
      .updateReserveMovement(edit.movement.id, toMovementUpdate(edit.values))
      .then(() => {
        setEdit(null)
        onSaved()
      })
      .catch((err: unknown) => {
        setEdit((current) => current && { ...current, isSaving: false, error: getSaveErrorMessage(err, 'Failed to update the pot transaction.') })
      })
  }

  return {
    edit,
    isOpen: edit !== null,
    isDirty: edit !== null && hasChangedFields(edit.values, editMovementValues(edit.movement), EDIT_MOVEMENT_FIELDS),
    open,
    cancel,
    setField,
    submit,
  }
}
