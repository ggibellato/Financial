import { act, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { awaitRetryOutcome } from '../../utils/retryOutcomeFocus'
import LoadingState from '../LoadingState'

describe('LoadingState', () => {
  it('renders the default loading message', () => {
    render(<LoadingState />)

    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('renders a custom message when provided', () => {
    render(<LoadingState message="Please wait…" />)

    expect(screen.getByText('Please wait…')).toBeInTheDocument()
  })

  it('announces the loading message as a polite status', () => {
    render(<LoadingState message="Loading accounts…" />)

    expect(screen.getByRole('status')).toHaveTextContent('Loading accounts…')
  })

  it('leaves focus to the loading that Try again started when an earlier loading ends late', async () => {
    function Bills({ phase }: { phase: 'first-load' | 'retrying' | 'loaded' }) {
      return (
        <section aria-label="Bills">
          {phase === 'first-load' && <LoadingState key="first-load" />}
          {phase === 'retrying' && <LoadingState key="retrying" />}
          {phase === 'loaded' && <button>First bill</button>}
        </section>
      )
    }
    const { rerender } = render(<Bills phase="first-load" />)
    const bills = screen.getByRole('region', { name: 'Bills' })

    awaitRetryOutcome(bills)
    rerender(<Bills phase="retrying" />)
    await act(async () => {})

    expect(bills).not.toHaveFocus()
    rerender(<Bills phase="loaded" />)
    await waitFor(() => expect(screen.getByRole('button', { name: 'First bill' })).toHaveFocus())
  })
})
