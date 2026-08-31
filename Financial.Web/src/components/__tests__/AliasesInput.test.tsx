import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import AliasesInput from '../AliasesInput'

describe('AliasesInput', () => {
  it('renders no chips when there are no aliases', () => {
    render(<AliasesInput aliases={[]} onChange={vi.fn()} />)

    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument()
  })

  it('renders an existing alias as a chip', () => {
    render(<AliasesInput aliases={['Monzo']} onChange={vi.fn()} />)

    expect(screen.getByText('Monzo')).toBeInTheDocument()
  })

  it('adds a new alias via the Add button and clears the input', () => {
    const onChange = vi.fn()
    render(<AliasesInput aliases={['Monzo']} onChange={onChange} />)

    fireEvent.change(screen.getByLabelText('New alias'), { target: { value: 'Monzo Pot' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add alias' }))

    expect(onChange).toHaveBeenCalledWith(['Monzo', 'Monzo Pot'])
  })

  it('adds a new alias via the Enter key', () => {
    const onChange = vi.fn()
    render(<AliasesInput aliases={[]} onChange={onChange} />)

    fireEvent.change(screen.getByLabelText('New alias'), { target: { value: 'Monzo' } })
    fireEvent.keyDown(screen.getByLabelText('New alias'), { key: 'Enter' })

    expect(onChange).toHaveBeenCalledWith(['Monzo'])
  })

  it('does not add a blank alias', () => {
    const onChange = vi.fn()
    render(<AliasesInput aliases={['Monzo']} onChange={onChange} />)

    fireEvent.change(screen.getByLabelText('New alias'), { target: { value: '   ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add alias' }))

    expect(onChange).not.toHaveBeenCalled()
  })

  it('does not add a case-insensitive duplicate alias', () => {
    const onChange = vi.fn()
    render(<AliasesInput aliases={['Monzo']} onChange={onChange} />)

    fireEvent.change(screen.getByLabelText('New alias'), { target: { value: 'monzo' } })
    fireEvent.click(screen.getByRole('button', { name: 'Add alias' }))

    expect(onChange).not.toHaveBeenCalled()
  })

  it('removes an alias when its chip is clicked', () => {
    const onChange = vi.fn()
    render(<AliasesInput aliases={['Monzo', 'MonzoPot']} onChange={onChange} />)

    fireEvent.click(screen.getByRole('button', { name: 'Monzo' }))

    expect(onChange).toHaveBeenCalledWith(['MonzoPot'])
  })
})
