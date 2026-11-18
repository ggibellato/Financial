import { describe, expect, it } from 'vitest'
import { getMetaString } from '../treeNodeMetadata'

describe('getMetaString', () => {
  it.each([
    [{ Name: 'VUSA' }, 'VUSA'],
    [{ Name: 7 }, ''],
    [{}, ''],
  ])('reads %o as %j', (metadata, expected) => {
    expect(getMetaString(metadata, 'Name')).toBe(expected)
  })
})
