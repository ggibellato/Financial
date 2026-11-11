import { useRef, useState } from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import ErrorState from '../../components/ErrorState'
import { useFocusReloadedContent } from '../useFocusReloadedContent'

function SkeletonReload() {
  const [phase, setPhase] = useState<'error' | 'loading' | 'loaded'>('error')
  const regionRef = useRef<HTMLDivElement>(null)
  useFocusReloadedContent(regionRef, phase === 'loading')

  if (phase === 'error') return <ErrorState message="Unable to load" onRetry={() => setPhase('loading')} />
  return (
    <div ref={regionRef}>
      {phase === 'loading' ? <span aria-label="Loading rows" role="img" /> : <button>First row</button>}
      <button onClick={() => setPhase('loaded')} hidden={phase !== 'loading'}>
        Finish
      </button>
    </div>
  )
}

describe('useFocusReloadedContent', () => {
  it('moves focus into the region when a retried load that shows no LoadingState finishes', async () => {
    render(<SkeletonReload />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    fireEvent.click(screen.getByRole('button', { name: 'Finish' }))

    await waitFor(() => expect(screen.getByRole('button', { name: 'First row' })).toHaveFocus())
  })

  it('leaves focus alone after a first load that was never retried', async () => {
    function FirstLoad() {
      const [isLoading, setIsLoading] = useState(true)
      const regionRef = useRef<HTMLDivElement>(null)
      useFocusReloadedContent(regionRef, isLoading)
      return (
        <div ref={regionRef}>
          <button onClick={() => setIsLoading(false)}>{isLoading ? 'Finish' : 'First row'}</button>
        </div>
      )
    }
    render(<FirstLoad />)

    fireEvent.click(screen.getByRole('button', { name: 'Finish' }))

    await screen.findByRole('button', { name: 'First row' })
    expect(screen.getByRole('button', { name: 'First row' })).not.toHaveFocus()
  })

  it('focuses the reloaded region itself when it holds no control', async () => {
    function ReloadWithoutControls() {
      const [phase, setPhase] = useState<'error' | 'loading' | 'loaded'>('error')
      const regionRef = useRef<HTMLDivElement>(null)
      useFocusReloadedContent(regionRef, phase === 'loading')

      if (phase === 'error') return <ErrorState message="Unable to load" onRetry={() => setPhase('loading')} />
      return (
        <>
          <div ref={regionRef} aria-label="Totals" role="region">
            {phase === 'loaded' ? 'Market value 1,000.00' : 'Loading'}
          </div>
          {phase === 'loading' && <button onClick={() => setPhase('loaded')}>Finish</button>}
        </>
      )
    }
    render(<ReloadWithoutControls />)

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    fireEvent.click(screen.getByRole('button', { name: 'Finish' }))

    await waitFor(() => expect(screen.getByRole('region', { name: 'Totals' })).toHaveFocus())
  })
})
