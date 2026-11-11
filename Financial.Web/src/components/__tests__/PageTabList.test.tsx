import { describe, expect, it, vi } from 'vitest'
import { Tab } from '@fluentui/react-components'
import { fireEvent, render, screen } from '../../test/renderWithFluent'
import PageTabList from '../PageTabList'

describe('PageTabList', () => {
  it('is a named tablist that reports the selected tab', () => {
    const onTabSelect = vi.fn()
    render(
      <PageTabList aria-label="Detail views" selectedValue="summary" onTabSelect={onTabSelect}>
        <Tab value="summary">Summary</Tab>
        <Tab value="credits">Credits</Tab>
      </PageTabList>,
    )

    expect(screen.getByRole('tablist', { name: 'Detail views' })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Summary' })).toHaveAttribute('aria-selected', 'true')
    fireEvent.click(screen.getByRole('tab', { name: 'Credits' }))
    expect(onTabSelect).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ value: 'credits' }))
  })

  it('wraps its tabs onto another row instead of overflowing at narrow widths', () => {
    render(
      <PageTabList aria-label="Monthly views">
        <Tab value="a">A</Tab>
      </PageTabList>,
    )

    expect(getComputedStyle(screen.getByRole('tablist', { name: 'Monthly views' })).flexWrap).toBe('wrap')
  })
})
