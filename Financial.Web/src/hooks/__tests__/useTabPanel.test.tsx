import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useTabPanel } from '../useTabPanel'

const TABS = ['summary', 'credits', 'history']

function Tabs() {
  const [active, setActive] = useState('summary')
  const { tabProps, panelProps } = useTabPanel(active)
  return (
    <>
      <div role="tablist" aria-label="Views">
        {TABS.map((tab) => (
          <button key={tab} role="tab" aria-selected={tab === active} onClick={() => setActive(tab)} {...tabProps(tab)}>
            {tab}
          </button>
        ))}
      </div>
      <div {...panelProps}>{active} content</div>
    </>
  )
}

describe('useTabPanel', () => {
  it('points every tab at the one panel', () => {
    render(<Tabs />)

    const panel = screen.getByRole('tabpanel')
    expect(panel.id).not.toBe('')
    screen.getAllByRole('tab').forEach((tab) => expect(tab).toHaveAttribute('aria-controls', panel.id))
  })

  it('gives each tab its own id and names the panel after the active tab', () => {
    render(<Tabs />)

    const ids = screen.getAllByRole('tab').map((tab) => tab.id)
    expect(new Set(ids).size).toBe(TABS.length)
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', screen.getByRole('tab', { name: 'summary' }).id)
    expect(screen.getByRole('tabpanel', { name: 'summary' })).toHaveTextContent('summary content')
  })

  it('follows the active tab when it changes', () => {
    render(<Tabs />)

    fireEvent.click(screen.getByRole('tab', { name: 'credits' }))

    const credits = screen.getByRole('tab', { name: 'credits' })
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', credits.id)
    expect(screen.getByRole('tabpanel', { name: 'credits' })).toHaveTextContent('credits content')
  })
})
