import { render, screen, act } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { ColourModeProvider, useColourMode } from '../ColourModeContext'

function ModeDisplay({ testId = 'mode' }: { testId?: string }) {
  const { colourMode } = useColourMode()
  return <div data-testid={testId}>{colourMode}</div>
}

function ModeSetter() {
  const { setColourMode } = useColourMode()
  return <button onClick={() => setColourMode('dark')}>set-dark</button>
}

function ModeToggler() {
  const { toggleColourMode } = useColourMode()
  return <button onClick={() => toggleColourMode()}>toggle</button>
}

describe('ColourModeContext', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('defaults_to_light_when_no_stored_preference', () => {
    render(
      <ColourModeProvider>
        <ModeDisplay />
      </ColourModeProvider>,
    )
    expect(screen.getByTestId('mode').textContent).toBe('light')
  })

  it('initializes_from_a_previously_stored_dark_preference', () => {
    localStorage.setItem('financial.colourMode', 'dark')
    render(
      <ColourModeProvider>
        <ModeDisplay />
      </ColourModeProvider>,
    )
    expect(screen.getByTestId('mode').textContent).toBe('dark')
  })

  it('setColourMode_updates_the_context_value_and_persists_it', () => {
    render(
      <ColourModeProvider>
        <ModeSetter />
        <ModeDisplay />
      </ColourModeProvider>,
    )
    act(() => {
      screen.getByText('set-dark').click()
    })
    expect(screen.getByTestId('mode').textContent).toBe('dark')
    expect(localStorage.getItem('financial.colourMode')).toBe('dark')
  })

  it('toggleColourMode_flips_light_to_dark_and_back', () => {
    render(
      <ColourModeProvider>
        <ModeToggler />
        <ModeDisplay />
      </ColourModeProvider>,
    )
    act(() => {
      screen.getByText('toggle').click()
    })
    expect(screen.getByTestId('mode').textContent).toBe('dark')
    act(() => {
      screen.getByText('toggle').click()
    })
    expect(screen.getByTestId('mode').textContent).toBe('light')
  })

  it('two_consumers_stay_in_sync_after_one_changes_the_mode', () => {
    render(
      <ColourModeProvider>
        <ModeSetter />
        <ModeDisplay testId="mode-a" />
        <ModeDisplay testId="mode-b" />
      </ColourModeProvider>,
    )
    act(() => {
      screen.getByText('set-dark').click()
    })
    expect(screen.getByTestId('mode-a').textContent).toBe('dark')
    expect(screen.getByTestId('mode-b').textContent).toBe('dark')
  })

  it('useColourMode_throws_when_called_outside_provider', () => {
    const original = console.error
    console.error = () => {}
    expect(() => render(<ModeDisplay />)).toThrow('useColourMode must be used within a ColourModeProvider')
    console.error = original
  })
})
