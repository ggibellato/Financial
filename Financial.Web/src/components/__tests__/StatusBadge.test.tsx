import { describe, expect, it } from 'vitest'
import { render, screen } from '../../test/renderWithFluent'
import StatusBadge from '../StatusBadge'

describe('StatusBadge', () => {
  it('renders_requires_review_status', () => {
    render(<StatusBadge status="RequiresReview" />)
    expect(screen.getByText('Requires review')).toBeInTheDocument()
  })

  it('falls_back_gracefully_for_an_unknown_status', () => {
    render(<StatusBadge status="SomethingNew" />)
    expect(screen.getByText('SomethingNew')).toBeInTheDocument()
  })

  it('falls_back_gracefully_for_a_null_status', () => {
    render(<StatusBadge status={null} />)
    expect(screen.getByText('Unknown')).toBeInTheDocument()
  })
})
