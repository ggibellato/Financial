import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import AdminEntityPlaceholderPage from '../AdminEntityPlaceholderPage'

describe('AdminEntityPlaceholderPage', () => {
  it('renders the given entity label as its heading', () => {
    render(<AdminEntityPlaceholderPage entityLabel="Brokers" />)

    expect(screen.getByRole('heading', { name: 'Brokers' })).toBeInTheDocument()
  })

  it('renders a coming-soon notice', () => {
    render(<AdminEntityPlaceholderPage entityLabel="Reserve Buckets" />)

    expect(screen.getByText('Coming soon.')).toBeInTheDocument()
  })
})
