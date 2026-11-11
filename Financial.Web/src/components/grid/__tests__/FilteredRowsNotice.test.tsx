import { createRef } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../../test/renderWithFluent'
import { mockMatchMedia } from '../../../test/mockMatchMedia'
import FilteredRowsNotice from '../FilteredRowsNotice'

const originalMatchMedia = window.matchMedia

function renderNotice({ isFiltered = true, shownCount = 3, totalCount = 10, onClearFilters = vi.fn() } = {}) {
  const tableRef = createRef<HTMLTableElement>()
  const noticeShowing = (shown: number) => (
    <>
      <FilteredRowsNotice
        isFiltered={isFiltered}
        shownCount={shown}
        totalCount={totalCount}
        onClearFilters={onClearFilters}
        tableRef={tableRef}
      />
      <table ref={tableRef} tabIndex={-1} aria-label="Totals" />
    </>
  )
  const { rerender } = render(noticeShowing(shownCount))
  return { onClearFilters, showCount: (shown: number) => rerender(noticeShowing(shown)) }
}

describe('FilteredRowsNotice', () => {
  afterEach(() => {
    window.matchMedia = originalMatchMedia
  })

  it('announces how many rows an active filter shows when the grid is stacked', () => {
    mockMatchMedia(true)
    renderNotice()

    expect(screen.getByRole('status')).toHaveTextContent('Filtered: 3 of 10 rows')
    expect(screen.getByRole('button', { name: 'Clear filters' })).toBeInTheDocument()
  })

  it('reads a single row in the singular', () => {
    mockMatchMedia(true)
    renderNotice({ shownCount: 0, totalCount: 1 })

    expect(screen.getByRole('status')).toHaveTextContent('Filtered: 0 of 1 row')
  })

  it('shows nothing while no filter is active', () => {
    mockMatchMedia(true)
    renderNotice({ isFiltered: false })

    expect(screen.getByRole('status')).toBeEmptyDOMElement()
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
  })

  it('still announces the filtered rows at desktop width, without the visible notice the header filter replaces', () => {
    mockMatchMedia(false)
    renderNotice()

    expect(screen.getByRole('status')).toHaveTextContent('Filtered: 3 of 10 rows')
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
  })

  it('announces nothing at desktop width while no filter is active', () => {
    mockMatchMedia(false)
    renderNotice({ isFiltered: false })

    expect(screen.getByRole('status')).toBeEmptyDOMElement()
  })

  it.each([false, true])('announces in the same live region when the last filter is cleared (stacked: %s)', (isStacked) => {
    mockMatchMedia(isStacked)
    const tableRef = createRef<HTMLTableElement>()
    const notice = (isFiltered: boolean) => (
      <FilteredRowsNotice isFiltered={isFiltered} shownCount={isFiltered ? 3 : 10} totalCount={10} onClearFilters={vi.fn()} tableRef={tableRef} />
    )
    const { rerender } = render(notice(true))
    const liveRegion = screen.getByRole('status')

    rerender(notice(false))

    expect(screen.getByRole('status')).toBe(liveRegion)
    expect(liveRegion).toHaveTextContent('Filters cleared: showing all 10 rows')
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
  })

  it('does not re-announce the cleared filters when the row count later changes', () => {
    mockMatchMedia(false)
    const tableRef = createRef<HTMLTableElement>()
    const notice = (isFiltered: boolean, totalCount: number) => (
      <FilteredRowsNotice isFiltered={isFiltered} shownCount={1} totalCount={totalCount} onClearFilters={vi.fn()} tableRef={tableRef} />
    )
    const { rerender } = render(notice(true, 10))
    rerender(notice(false, 10))

    rerender(notice(false, 12))

    expect(screen.getByRole('status')).toHaveTextContent('Filters cleared: showing all 10 rows')
  })

  it('announces nothing on first render with no filter, and the filtered count again once a filter returns', () => {
    mockMatchMedia(false)
    const tableRef = createRef<HTMLTableElement>()
    const notice = (isFiltered: boolean) => (
      <FilteredRowsNotice isFiltered={isFiltered} shownCount={1} totalCount={1} onClearFilters={vi.fn()} tableRef={tableRef} />
    )
    const { rerender } = render(notice(false))
    expect(screen.getByRole('status')).toBeEmptyDOMElement()

    rerender(notice(true))
    rerender(notice(false))
    expect(screen.getByRole('status')).toHaveTextContent('Filters cleared: showing all 1 row')

    rerender(notice(true))
    expect(screen.getByRole('status')).toHaveTextContent('Filtered: 1 of 1 row')
  })

  it('keeps the same live region and updates its text when the filtered count changes', () => {
    mockMatchMedia(false)
    const { showCount } = renderNotice()
    const liveRegion = screen.getByRole('status')

    showCount(5)

    expect(screen.getByRole('status')).toBe(liveRegion)
    expect(liveRegion).toHaveTextContent('Filtered: 5 of 10 rows')
  })

  it('clears the filters and moves focus to the grid', () => {
    mockMatchMedia(true)
    const { onClearFilters } = renderNotice()

    fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }))

    expect(onClearFilters).toHaveBeenCalledOnce()
    expect(screen.getByRole('table', { name: 'Totals' })).toHaveFocus()
  })
})
