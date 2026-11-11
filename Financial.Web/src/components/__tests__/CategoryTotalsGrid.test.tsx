import { act, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { mockMatchMedia } from '../../test/mockMatchMedia'
import CategoryTotalsGrid from '../CategoryTotalsGrid'
import type { CategoryTotalDto } from '../../api/types'

const CATEGORY_TOTALS: CategoryTotalDto[] = [
  { category: 'Mercado', totalValue: 42.5 },
  { category: 'Casa', totalValue: 100 },
]

describe('CategoryTotalsGrid', () => {
  it('renders a row per category with the footer total', () => {
    render(<CategoryTotalsGrid categoryTotals={CATEGORY_TOTALS} categoryTotalsSum={142.5} />)

    expect(screen.getByText('Mercado')).toBeInTheDocument()
    expect(screen.getByText('Casa')).toBeInTheDocument()
    expect(screen.getByText('42.50')).toBeInTheDocument()
    expect(screen.getByText('100.00')).toBeInTheDocument()
    expect(screen.getByText('142.50')).toBeInTheDocument()
  })

  it('labels the footer total as covering all rows while a Category filter is active, and plainly when none is', () => {
    render(<CategoryTotalsGrid categoryTotals={CATEGORY_TOTALS} categoryTotalsSum={142.5} />)

    const footer = () => document.querySelector('.monthly-page__section-total')!.textContent

    expect(footer()).toBe('Total: 142.50')

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Mercado' }))

    expect(footer()).toBe('Total (all rows): 142.50')

    fireEvent.click(screen.getByRole('checkbox', { name: 'Mercado' }))

    expect(footer()).toBe('Total: 142.50')
  })

  it('sorts rows by Category ascending when its header is clicked', () => {
    render(<CategoryTotalsGrid categoryTotals={CATEGORY_TOTALS} categoryTotalsSum={142.5} />)

    fireEvent.click(screen.getByRole('button', { name: 'Category' }))

    const dataRows = screen.getAllByRole('row').slice(1)
    expect(within(dataRows[0]).getByText('Casa')).toBeInTheDocument()
    expect(within(dataRows[1]).getByText('Mercado')).toBeInTheDocument()
  })

  it('filters rows by Category via the header checklist, and shows the empty message when nothing matches', () => {
    render(<CategoryTotalsGrid categoryTotals={CATEGORY_TOTALS} categoryTotalsSum={142.5} />)

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Mercado' }))

    // The header's own filter checklist keeps every category name in the DOM (as checkbox
    // labels) regardless of which rows are filtered out, so assertions must be scoped to tbody.
    const tbody = within(document.querySelector('tbody')!)
    expect(tbody.queryByText('Mercado')).not.toBeInTheDocument()
    expect(tbody.getByText('Casa')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('checkbox', { name: 'Casa' }))

    expect(screen.getByText('No rows match the current filters')).toBeInTheDocument()
  })

  it('shows its empty message when no expense was recorded in the month', () => {
    render(<CategoryTotalsGrid categoryTotals={[]} categoryTotalsSum={0} />)

    expect(screen.getByText('No expenses recorded for this month')).toBeInTheDocument()
    expect(screen.getByText('0.00')).toBeInTheDocument()
  })

  describe('in the stacked narrow layout, where the header row is clipped', () => {
    const originalMatchMedia = window.matchMedia

    afterEach(() => {
      window.matchMedia = originalMatchMedia
    })

    function filterOutMercado() {
      fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
      fireEvent.click(screen.getByRole('checkbox', { name: 'Mercado' }))
    }

    it('shows an active filter above the grid, and Clear filters brings every row back', async () => {
      mockMatchMedia(true)
      render(<CategoryTotalsGrid categoryTotals={CATEGORY_TOTALS} categoryTotalsSum={142.5} />)
      expect(screen.getByRole('status')).toBeEmptyDOMElement()

      filterOutMercado()

      expect(await screen.findByText('Filtered: 1 of 2 rows')).toBeInTheDocument()
      fireEvent.click(screen.getByRole('button', { name: 'Clear filters' }))

      expect(screen.getByRole('status')).toBeEmptyDOMElement()
      expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
      expect(within(document.querySelector('tbody')!).getByText('Mercado')).toBeInTheDocument()
      expect(screen.getByRole('table', { name: 'Category totals' })).toHaveFocus()
    })

    it('shows a filter set at desktop width once the window narrows, and hides it again at desktop width', async () => {
      const media = mockMatchMedia(false)
      render(<CategoryTotalsGrid categoryTotals={CATEGORY_TOTALS} categoryTotalsSum={142.5} />)
      filterOutMercado()
      expect(screen.getByRole('status')).toBeEmptyDOMElement()

      act(() => media.setMatches(true))
      expect(await screen.findByText('Filtered: 1 of 2 rows')).toBeInTheDocument()

      act(() => media.setMatches(false))
      expect(screen.getByRole('status')).toBeEmptyDOMElement()
      expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument()
    })
  })
})
