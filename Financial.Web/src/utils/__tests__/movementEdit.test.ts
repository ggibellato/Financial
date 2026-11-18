import { describe, expect, it } from 'vitest'
import { validateEditMovement, type EditMovementValues } from '../movementEdit'

const VALID: EditMovementValues = {
  editMovementBucketId: 'bucket-1',
  editMovementAmount: '10',
  editMovementDate: '2026-01-01',
  editMovementDescription: 'Top-up',
}

describe('validateEditMovement', () => {
  it('accepts a complete movement', () => {
    expect(validateEditMovement(VALID)).toEqual({})
  })

  it('refuses a date or bucket of spaces as missing', () => {
    expect(validateEditMovement({ ...VALID, editMovementDate: '  ', editMovementBucketId: '  ' })).toEqual({
      editMovementDate: 'Date is required.',
      editMovementBucketId: 'Bucket is required.',
    })
  })
})
