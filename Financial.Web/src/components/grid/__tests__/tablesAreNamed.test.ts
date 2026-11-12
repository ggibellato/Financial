import { describe, expect, it } from 'vitest'

const sources = import.meta.glob<string>(['../../../**/*.tsx', '!../../../**/__tests__/**'], {
  query: '?raw',
  import: 'default',
  eager: true,
})

const OPENING_TABLE_TAG = /<Table(?=[\s>])[^>]*>/g

function unnamedTablesIn(source: string): string[] {
  return [...source.matchAll(OPENING_TABLE_TAG)].map((match) => match[0]).filter((tag) => !/aria-label(ledby)?=/.test(tag))
}

describe('data tables', () => {
  it('scans the app sources', () => {
    expect(Object.keys(sources).length).toBeGreaterThan(50)
  })

  it('every Fluent Table has an accessible name', () => {
    const unnamed = Object.entries(sources).flatMap(([file, source]) => unnamedTablesIn(source).map((tag) => `${file}: ${tag}`))

    expect(unnamed).toEqual([])
  })

  it('reports a Table without aria-label or aria-labelledby', () => {
    expect(unnamedTablesIn('<Table className="data-table">')).toHaveLength(1)
    expect(unnamedTablesIn('<Table\n  className="data-table"\n  aria-labelledby={headingId}\n>')).toEqual([])
    expect(unnamedTablesIn('<TableRow>')).toEqual([])
  })
})
