import { render, screen } from '@testing-library/react'
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
})
