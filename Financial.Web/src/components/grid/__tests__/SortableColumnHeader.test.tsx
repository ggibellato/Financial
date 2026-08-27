import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '../../../test/renderWithFluent'
import SortableColumnHeader from '../SortableColumnHeader'

describe('SortableColumnHeader', () => {
  it('renders no sort icon when the column is not the active sort column', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableColumnHeader label="Value" columnKey="value" onSort={vi.fn()} />
          </tr>
        </thead>
      </table>,
    )

    expect(screen.getByRole('columnheader')).toHaveAttribute('aria-sort', 'none')
  })

  it('renders an ascending indicator only on the active ascending column', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableColumnHeader label="Value" columnKey="value" sortDirection="ascending" onSort={vi.fn()} />
          </tr>
        </thead>
      </table>,
    )

    expect(screen.getByRole('columnheader')).toHaveAttribute('aria-sort', 'ascending')
  })

  it('renders a descending indicator when the column is sorted descending', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableColumnHeader label="Value" columnKey="value" sortDirection="descending" onSort={vi.fn()} />
          </tr>
        </thead>
      </table>,
    )

    expect(screen.getByRole('columnheader')).toHaveAttribute('aria-sort', 'descending')
  })

  it('calls onSort with the column key when the header button is clicked', () => {
    const onSort = vi.fn()
    render(
      <table>
        <thead>
          <tr>
            <SortableColumnHeader label="Value" columnKey="value" onSort={onSort} />
          </tr>
        </thead>
      </table>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Value' }))

    expect(onSort).toHaveBeenCalledWith('value')
  })

  it('renders children in the same header cell, for the future filter icon slot', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableColumnHeader label="Category" columnKey="category" onSort={vi.fn()}>
              <span data-testid="filter-slot" />
            </SortableColumnHeader>
          </tr>
        </thead>
      </table>,
    )

    expect(screen.getByTestId('filter-slot')).toBeInTheDocument()
  })

  it('is keyboard-operable via a native button element', () => {
    render(
      <table>
        <thead>
          <tr>
            <SortableColumnHeader label="Value" columnKey="value" onSort={vi.fn()} />
          </tr>
        </thead>
      </table>,
    )

    expect(screen.getByRole('button', { name: 'Value' }).tagName).toBe('BUTTON')
  })
})
