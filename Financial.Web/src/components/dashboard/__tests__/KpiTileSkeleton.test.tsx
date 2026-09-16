import { describe, expect, it } from 'vitest'
import { render, screen } from '../../../test/renderWithFluent'
import KpiTileSkeleton from '../KpiTileSkeleton'

describe('KpiTileSkeleton', () => {
  it('renders_a_busy_placeholder_in_place_of_the_figure', () => {
    render(<KpiTileSkeleton />)

    expect(screen.getByRole('progressbar')).toHaveAttribute('aria-busy', 'true')
  })

  it('announces_a_generic_loading_name_never_content', () => {
    render(<KpiTileSkeleton />)

    const skeleton = screen.getByRole('progressbar')
    expect(skeleton).toHaveAccessibleName('Loading')
    expect(skeleton.textContent).toBe('')
  })
})
