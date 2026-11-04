import type { ReserveMovementDto, ReserveMovementUpdateDto } from '../api/types'
import type { FieldErrors } from '../hooks/inlineFormSlice'
import { parseValidatedNumber } from './formatters'

export const BUCKET_REQUIRED_ERROR = 'Bucket is required'

export const EDIT_MOVEMENT_FIELDS = ['editMovementBucketId', 'editMovementAmount', 'editMovementDate', 'editMovementDescription'] as const

export type EditMovementField = (typeof EDIT_MOVEMENT_FIELDS)[number]

export type EditMovementValues = Record<EditMovementField, string>

export function editMovementValues(movement: ReserveMovementDto): EditMovementValues {
  return {
    editMovementBucketId: movement.bucketId,
    editMovementAmount: String(movement.amount),
    editMovementDate: movement.date,
    editMovementDescription: movement.description,
  }
}

export function validateEditMovement(values: EditMovementValues): FieldErrors<EditMovementField> {
  const errors: FieldErrors<EditMovementField> = {}

  if (!values.editMovementDate.trim()) {
    errors.editMovementDate = 'Date is required'
  }

  if (!values.editMovementBucketId.trim()) {
    errors.editMovementBucketId = BUCKET_REQUIRED_ERROR
  }

  if (!values.editMovementDescription.trim()) {
    errors.editMovementDescription = 'Description is required'
  }

  if (parseValidatedNumber(values.editMovementAmount) === null) {
    errors.editMovementAmount = 'Amount must be a number'
  }

  return errors
}

export function toMovementUpdate(values: EditMovementValues): ReserveMovementUpdateDto {
  return {
    bucketId: values.editMovementBucketId,
    amount: Number(values.editMovementAmount),
    date: values.editMovementDate,
    description: values.editMovementDescription,
  }
}
