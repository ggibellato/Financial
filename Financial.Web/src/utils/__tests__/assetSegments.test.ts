import { describe, expect, it } from 'vitest'
import { distinctSegments, totalsBySegment } from '../assetSegments'

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

describe('totalsBySegment', () => {
  it('adds up each segment, named after its first item alphabetically, with No segment last', () => {
    expect(
      totalsBySegment([
        { name: 'ZZZZ11', segment: 'paper', value: 1 },
        { name: 'NONE11', segment: null, value: 5 },
        { name: 'AAAA11', segment: 'Paper', value: 2 },
        { name: 'LOGI11', segment: 'Logistics', value: 3 },
        { name: 'BLNK11', segment: null, value: 4 },
      ]),
    ).toEqual([
      { name: 'Logistics', value: 3 },
      { name: 'Paper', value: 3 },
      { name: 'No segment', value: 9 },
    ])
  })
})
