import { describe, expect, it } from 'vitest'
import { isAssetNameCollisionError } from '../isAssetNameCollisionError'

describe('isAssetNameCollisionError', () => {
  it('recognizes an asset name collision message', () => {
    const result = isAssetNameCollisionError(
      'An asset named "Company B" already exists — select it or choose a different name',
    )

    expect(result).toBe(true)
  })

  it('returns false for an unrecognized message', () => {
    const result = isAssetNameCollisionError('Network request failed')

    expect(result).toBe(false)
  })
})
