import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import PortfolioNavigatorPage from '../PortfolioNavigatorPage'

describe('PortfolioNavigatorPage', () => {
  it('renders placeholder message', () => {
    render(<PortfolioNavigatorPage />)

    expect(screen.getByText('Portfolio Navigator — coming soon')).toBeInTheDocument()
  })
})
