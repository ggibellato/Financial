import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../../test/renderWithFluent'
import ColumnFilterMenu from '../ColumnFilterMenu'

const TEN_VALUES = Array.from({ length: 10 }, (_, i) => `Category ${i + 1}`)
const ELEVEN_VALUES = Array.from({ length: 11 }, (_, i) => `Category ${i + 1}`)

describe('ColumnFilterMenu', () => {
  it('renders the filter icon without the active class when unfiltered', () => {
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={['Casa', 'Mercado']}
        selectedValues={undefined}
        onToggleValue={vi.fn()}
        onToggleAll={vi.fn()}
        isFiltered={false}
      />,
    )

    const trigger = screen.getByRole('button', { name: 'Filter by Category' })
    expect(trigger.className).not.toMatch(/--active/)
  })

  it('marks the trigger active when the column is filtered', () => {
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={['Casa', 'Mercado']}
        selectedValues={new Set(['Casa'])}
        onToggleValue={vi.fn()}
        onToggleAll={vi.fn()}
        isFiltered
      />,
    )

    const trigger = screen.getByRole('button', { name: 'Filter by Category' })
    expect(trigger.className).toMatch(/--active/)
  })

  it('opens the checklist with one checkbox per available value, plus (All)', () => {
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={['Casa', 'Mercado']}
        selectedValues={undefined}
        onToggleValue={vi.fn()}
        onToggleAll={vi.fn()}
        isFiltered={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))

    expect(screen.getByRole('checkbox', { name: '(All)' })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Casa' })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Mercado' })).toBeInTheDocument()
  })

  it('calls onToggleValue when a value checkbox is clicked', () => {
    const onToggleValue = vi.fn()
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={['Casa', 'Mercado']}
        selectedValues={undefined}
        onToggleValue={onToggleValue}
        onToggleAll={vi.fn()}
        isFiltered={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Casa' }))

    expect(onToggleValue).toHaveBeenCalledWith('category', 'Casa')
  })

  it('calls onToggleAll when the (All) checkbox is clicked', () => {
    const onToggleAll = vi.fn()
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={['Casa', 'Mercado']}
        selectedValues={undefined}
        onToggleValue={vi.fn()}
        onToggleAll={onToggleAll}
        isFiltered={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    fireEvent.click(screen.getByRole('checkbox', { name: '(All)' }))

    expect(onToggleAll).toHaveBeenCalledWith('category')
  })

  it('does not render a search box at exactly 10 available values', () => {
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={TEN_VALUES}
        selectedValues={undefined}
        onToggleValue={vi.fn()}
        onToggleAll={vi.fn()}
        isFiltered={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))

    expect(screen.queryByPlaceholderText('Search Category')).not.toBeInTheDocument()
  })

  it('renders and narrows via a search box at 11+ available values', () => {
    render(
      <ColumnFilterMenu
        columnKey="category"
        label="Category"
        availableValues={ELEVEN_VALUES}
        selectedValues={undefined}
        onToggleValue={vi.fn()}
        onToggleAll={vi.fn()}
        isFiltered={false}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Filter by Category' }))
    const searchBox = screen.getByPlaceholderText('Search Category')
    fireEvent.change(searchBox, { target: { value: 'Category 1' } })

    expect(screen.getByRole('checkbox', { name: 'Category 1' })).toBeInTheDocument()
    expect(screen.queryByRole('checkbox', { name: 'Category 2' })).not.toBeInTheDocument()
  })
})
