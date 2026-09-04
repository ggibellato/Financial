import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import SplitPanel from '../SplitPanel'

describe('SplitPanel', () => {
  it('renders left child content', () => {
    render(<SplitPanel left={<span>Left content</span>} right={<span>Right</span>} />)
    expect(screen.getByText('Left content')).toBeInTheDocument()
  })

  it('renders right child content', () => {
    render(<SplitPanel left={<span>Left</span>} right={<span>Right content</span>} />)
    expect(screen.getByText('Right content')).toBeInTheDocument()
  })

  it('drag handle is present', () => {
    render(<SplitPanel left={<span>L</span>} right={<span>R</span>} />)
    expect(screen.getByLabelText('Resize panel')).toBeInTheDocument()
  })

  it('left panel has default width of 300px', () => {
    render(<SplitPanel left={<span>L</span>} right={<span>R</span>} />)
    const leftPanel = screen.getByText('L').closest('.split-panel__left')
    expect(leftPanel).toHaveStyle({ width: '300px' })
  })

  it('handle is a focusable separator with the current width exposed', () => {
    render(<SplitPanel left={<span>L</span>} right={<span>R</span>} />)
    const handle = screen.getByRole('separator', { name: 'Resize panel' })
    expect(handle).toHaveAttribute('tabIndex', '0')
    expect(handle).toHaveAttribute('aria-orientation', 'vertical')
    expect(handle).toHaveAttribute('aria-valuenow', '300')
    expect(handle).toHaveAttribute('aria-valuemin', '300')
  })

  it('ArrowLeft/ArrowRight resize the left panel by the keyboard step', () => {
    render(<SplitPanel left={<span>L</span>} right={<span>R</span>} />)
    const handle = screen.getByRole('separator', { name: 'Resize panel' })
    const leftPanel = screen.getByText('L').closest('.split-panel__left')

    fireEvent.keyDown(handle, { key: 'ArrowRight' })
    expect(leftPanel).toHaveStyle({ width: '320px' })
    expect(handle).toHaveAttribute('aria-valuenow', '320')

    fireEvent.keyDown(handle, { key: 'ArrowLeft' })
    expect(leftPanel).toHaveStyle({ width: '300px' })
  })

  it('ArrowLeft does not resize below the minimum width', () => {
    render(<SplitPanel left={<span>L</span>} right={<span>R</span>} />)
    const handle = screen.getByRole('separator', { name: 'Resize panel' })
    const leftPanel = screen.getByText('L').closest('.split-panel__left')

    fireEvent.keyDown(handle, { key: 'ArrowLeft' })
    expect(leftPanel).toHaveStyle({ width: '300px' })
  })

  it('Home jumps to the minimum width', () => {
    render(<SplitPanel left={<span>L</span>} right={<span>R</span>} />)
    const handle = screen.getByRole('separator', { name: 'Resize panel' })
    const leftPanel = screen.getByText('L').closest('.split-panel__left')

    fireEvent.keyDown(handle, { key: 'ArrowRight' })
    fireEvent.keyDown(handle, { key: 'Home' })
    expect(leftPanel).toHaveStyle({ width: '300px' })
  })
})
