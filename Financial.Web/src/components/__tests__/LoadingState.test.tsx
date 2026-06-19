import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import LoadingState from '../LoadingState'

describe('LoadingState', () => {
  it('renders the default loading message', () => {
    render(<LoadingState />)

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('renders a custom message when provided', () => {
    render(<LoadingState message="Please wait..." />)

    expect(screen.getByText('Please wait...')).toBeInTheDocument()
  })
})
