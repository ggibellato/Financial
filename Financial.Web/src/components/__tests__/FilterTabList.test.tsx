import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import FilterTabList from '../FilterTabList'

const OPTIONS = [
  { value: '1Y', label: '1 Year' },
  { value: 'All', label: 'All' },
] as const

describe('FilterTabList', () => {
  it('is named by its visible label', () => {
    render(<FilterTabList label="View:" options={OPTIONS} selected="1Y" onSelect={vi.fn()} />)

    expect(screen.getByRole('tablist', { name: 'View:' })).toBeInTheDocument()
  })

  it('is named by aria-label when it shows no label', () => {
    render(<FilterTabList aria-label="Period" options={OPTIONS} selected="1Y" onSelect={vi.fn()} />)

    expect(screen.getByRole('tablist', { name: 'Period' })).toBeInTheDocument()
  })

  it('reports the chosen option', () => {
    const onSelect = vi.fn()
    render(<FilterTabList aria-label="Period" options={OPTIONS} selected="1Y" onSelect={onSelect} />)

    fireEvent.click(screen.getByRole('tab', { name: 'All' }))

    expect(onSelect).toHaveBeenCalledWith('All')
  })
})
