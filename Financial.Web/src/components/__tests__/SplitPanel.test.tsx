import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import SplitPanel from '../SplitPanel'

function renderSplitPanel() {
  return render(<SplitPanel left={<div>Left content</div>} right={<div>Right content</div>} />)
}

function getLeftPanel() {
  return document.querySelector<HTMLElement>('.split-panel__left')
}

describe('SplitPanel', () => {
  it('renders the left and right content', () => {
    renderSplitPanel()
    expect(screen.getByText('Left content')).toBeInTheDocument()
    expect(screen.getByText('Right content')).toBeInTheDocument()
  })

  it('starts at the default width', () => {
    renderSplitPanel()
    expect(getLeftPanel()).toHaveStyle({ width: '300px' })
  })

  it('dragging the handle to the right widens the left panel', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.mouseDown(handle, { clientX: 100 })
    expect(document.body.style.cursor).toBe('col-resize')

    fireEvent.mouseMove(document, { clientX: 150 })
    expect(getLeftPanel()).toHaveStyle({ width: '350px' })

    fireEvent.mouseUp(document)
    expect(document.body.style.cursor).toBe('')
  })

  it('dragging the handle left of the minimum width clamps at the minimum', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.mouseDown(handle, { clientX: 100 })
    fireEvent.mouseMove(document, { clientX: -1000 })

    expect(getLeftPanel()).toHaveStyle({ width: '300px' })
  })

  it('mouse movement after mouseup no longer resizes the panel', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.mouseDown(handle, { clientX: 100 })
    fireEvent.mouseMove(document, { clientX: 150 })
    fireEvent.mouseUp(document)
    fireEvent.mouseMove(document, { clientX: 400 })

    expect(getLeftPanel()).toHaveStyle({ width: '350px' })
  })

  it('ArrowRight widens the panel by the keyboard step', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.keyDown(handle, { key: 'ArrowRight' })

    expect(getLeftPanel()).toHaveStyle({ width: '320px' })
  })

  it('ArrowLeft narrows the panel, clamped at the minimum width', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.keyDown(handle, { key: 'ArrowRight' })
    fireEvent.keyDown(handle, { key: 'ArrowLeft' })
    fireEvent.keyDown(handle, { key: 'ArrowLeft' })

    expect(getLeftPanel()).toHaveStyle({ width: '300px' })
  })

  it('Home jumps to the minimum width', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.keyDown(handle, { key: 'ArrowRight' })
    fireEvent.keyDown(handle, { key: 'Home' })

    expect(getLeftPanel()).toHaveStyle({ width: '300px' })
  })

  it('End jumps to the maximum width', () => {
    renderSplitPanel()
    const handle = screen.getByRole('separator', { name: 'Resize panel' })

    fireEvent.keyDown(handle, { key: 'End' })

    expect(getLeftPanel()).toHaveStyle({ width: `${window.innerWidth / 2}px` })
  })
})
