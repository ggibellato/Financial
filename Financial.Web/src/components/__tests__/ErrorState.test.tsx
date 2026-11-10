import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ErrorState from '../ErrorState'

describe('ErrorState', () => {
  it('renders the error message', () => {
    render(<ErrorState message="Something went wrong" />)

    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('wraps content in an alert role', () => {
    render(<ErrorState message="An error occurred" />)

    expect(screen.getByRole('alert')).toBeInTheDocument()
  })

  it('does not render a retry button when onRetry is not provided', () => {
    render(<ErrorState message="An error occurred" />)

    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders a retry button when onRetry is provided', () => {
    render(<ErrorState message="An error occurred" onRetry={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('calls onRetry when the retry button is clicked', () => {
    const onRetry = vi.fn()
    render(<ErrorState message="An error occurred" onRetry={onRetry} />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(onRetry).toHaveBeenCalledOnce()
  })

  it('moves focus to the error when focus would otherwise be lost', () => {
    render(<ErrorState message="An error occurred" onRetry={vi.fn()} />)

    expect(screen.getByRole('alert')).toHaveFocus()
  })

  it('takes focus again after a failed retry unmounts the Try again button', () => {
    function RetryThatFails() {
      const [isRetrying, setIsRetrying] = useState(false)
      if (isRetrying) return <button onClick={() => setIsRetrying(false)}>Finish retry</button>
      return <ErrorState message="Still failing" onRetry={() => setIsRetrying(true)} />
    }
    render(<RetryThatFails />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    fireEvent.click(screen.getByRole('button', { name: 'Finish retry' }))

    expect(screen.getByRole('alert')).toHaveFocus()
  })

  it('leaves focus on a control the user is already in', () => {
    render(
      <>
        <input aria-label="Amount" autoFocus />
        <ErrorState message="Side panel failed" />
      </>,
    )

    expect(screen.getByRole('textbox', { name: 'Amount' })).toHaveFocus()
  })
})
