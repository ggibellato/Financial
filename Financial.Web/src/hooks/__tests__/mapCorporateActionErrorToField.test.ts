import { describe, expect, it } from 'vitest'
import { mapCorporateActionErrorToField } from '../mapCorporateActionErrorToField'

describe('mapCorporateActionErrorToField', () => {
  it('maps a target-asset name collision message to the formTargetAsset field', () => {
    const result = mapCorporateActionErrorToField(
      'An asset named "Company B" already exists — select it or choose a different name',
    )

    expect(result).toBe('formTargetAsset')
  })

  it('returns null for an unrecognized message', () => {
    const result = mapCorporateActionErrorToField('Network request failed')

    expect(result).toBeNull()
  })
})
