import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import CashFlowPlaceholderPage from '../CashFlowPlaceholderPage'

describe('CashFlowPlaceholderPage', () => {
  it('renders the given title', () => {
    render(<CashFlowPlaceholderPage title="Reserva" />)

    expect(screen.getByRole('heading', { name: 'Reserva' })).toBeInTheDocument()
  })

  it('renders a coming soon message referencing the title', () => {
    render(<CashFlowPlaceholderPage title="Monthly" />)

    expect(screen.getByText('Monthly view is coming soon.')).toBeInTheDocument()
  })
})
