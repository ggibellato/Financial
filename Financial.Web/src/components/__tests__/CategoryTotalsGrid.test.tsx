import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
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
})
