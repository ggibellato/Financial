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
})
