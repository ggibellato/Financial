import { describe, expect, it } from 'vitest'
import { distinctSegments } from '../assetSegments'

describe('distinctSegments', () => {
  it('lists each segment once, alphabetically, ignoring case and surrounding spaces', () => {
    expect(distinctSegments(['Paper', ' logistics ', null, 'Logistics', '  ', undefined, 'Banks'])).toEqual([
      'Banks',
      'logistics',
      'Paper',
    ])
  })

  it('returns no segments when none are set', () => {
    expect(distinctSegments([null, undefined, ''])).toEqual([])
  })
})
