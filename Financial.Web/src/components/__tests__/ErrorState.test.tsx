import { useState } from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import ErrorState from '../ErrorState'
import LoadingState from '../LoadingState'

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

  it('leaves focus where it is when the error first appears, so the page does not jump', async () => {
    vi.resetModules()
    const { default: FreshErrorState } = await import('../ErrorState')
    render(<FreshErrorState message="An error occurred" onRetry={vi.fn()} />)

    expect(screen.getByRole('alert')).not.toHaveFocus()
  })

  it('stops waiting for the retry outcome once the user moves focus elsewhere', () => {
    function RetryThenMoveOn() {
      const [phase, setPhase] = useState<'error' | 'working' | 'later-error'>('error')
      if (phase === 'error') return <ErrorState message="Failed" onRetry={() => setPhase('working')} />
      if (phase === 'working') return <button onClick={() => setPhase('later-error')}>Carry on</button>
      return <ErrorState message="A later, unrelated failure" />
    }
    render(<RetryThenMoveOn />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    const carryOn = screen.getByRole('button', { name: 'Carry on' })
    carryOn.focus()
    carryOn.blur()
    fireEvent.click(carryOn)

    expect(screen.getByRole('alert')).not.toHaveFocus()
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

    const error = screen.getByRole('group', { name: 'Still failing' })
    expect(error).toHaveFocus()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
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

  describe('after Try again succeeds', () => {
    function RetryThatLoads({ outcome }: { outcome: 'loaded' | 'failed' }) {
      const [phase, setPhase] = useState<'error' | 'loading' | 'done'>('error')
      return (
        <section aria-label="Bills">
          {phase === 'error' && <ErrorState message="Unable to load" onRetry={() => setPhase('loading')} />}
          {phase === 'loading' && (
            <>
              <LoadingState />
              <button onClick={() => setPhase('done')}>Finish loading</button>
            </>
          )}
          {phase === 'done' && outcome === 'loaded' && <button>First bill</button>}
          {phase === 'done' && outcome === 'failed' && <ErrorState message="Still unable to load" onRetry={() => setPhase('loading')} />}
        </section>
      )
    }

    function retryAndFinish() {
      fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
      const finish = screen.getByRole('button', { name: 'Finish loading' })
      fireEvent.click(finish)
    }

    it('moves focus to the reloaded content', async () => {
      render(<RetryThatLoads outcome="loaded" />)

      retryAndFinish()

      await waitFor(() => expect(screen.getByRole('button', { name: 'First bill' })).toHaveFocus())
    })

    it('moves focus to the error again when the retry fails after loading', async () => {
      render(<RetryThatLoads outcome="failed" />)

      retryAndFinish()

      await waitFor(() => expect(screen.getByRole('group', { name: 'Still unable to load' })).toHaveFocus())
    })

    it('leaves focus alone when the user moved on while it reloaded', async () => {
      render(
        <>
          <input aria-label="Search" />
          <RetryThatLoads outcome="loaded" />
        </>,
      )

      fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
      const search = screen.getByRole('textbox', { name: 'Search' })
      search.focus()
      fireEvent.click(screen.getByRole('button', { name: 'Finish loading' }))

      await screen.findByRole('button', { name: 'First bill' })
      expect(search).toHaveFocus()
    })
  })

  it('stays an alert when a later failure is not the outcome of the retry', () => {
    const { unmount } = render(<ErrorState message="First failure" onRetry={vi.fn()} />)
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    unmount()

    render(<ErrorState message="Unrelated failure" />)

    expect(screen.getByRole('alert')).toHaveTextContent('Unrelated failure')
    expect(screen.getByRole('alert')).not.toHaveFocus()
  })
})
