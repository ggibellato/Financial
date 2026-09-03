import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import AppearancePage from '../AppearancePage'
import { ColourModeProvider, useColourMode } from '../../context/ColourModeContext'

function ModeDisplay() {
  const { colourMode } = useColourMode()
  return <div data-testid="mode">{colourMode}</div>
}

function ModeToggler() {
  const { toggleColourMode } = useColourMode()
  return (
    <button type="button" onClick={() => toggleColourMode()}>
      toggle-elsewhere
    </button>
  )
}

describe('AppearancePage', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('renders_colour_mode_heading_and_two_text_labelled_options', () => {
    render(
      <ColourModeProvider>
        <AppearancePage />
      </ColourModeProvider>,
    )
    expect(screen.getByText('Colour mode')).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'Light' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'Dark' })).toBeInTheDocument()
  })

  it('light_is_selected_by_default', () => {
    render(
      <ColourModeProvider>
        <AppearancePage />
      </ColourModeProvider>,
    )
    expect(screen.getByRole('radio', { name: 'Light' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'Dark' })).not.toBeChecked()
  })

  it('dark_is_selected_when_previously_stored', () => {
    localStorage.setItem('financial.colourMode', 'dark')
    render(
      <ColourModeProvider>
        <AppearancePage />
      </ColourModeProvider>,
    )
    expect(screen.getByRole('radio', { name: 'Dark' })).toBeChecked()
  })

  it('selecting_dark_updates_the_shared_colour_mode', async () => {
    render(
      <ColourModeProvider>
        <AppearancePage />
        <ModeDisplay />
      </ColourModeProvider>,
    )
    fireEvent.click(screen.getByRole('radio', { name: 'Dark' }))
    expect(await screen.findByTestId('mode')).toHaveTextContent('dark')
  })

  it('reflects_a_mode_changed_elsewhere_in_the_same_provider', () => {
    render(
      <ColourModeProvider>
        <AppearancePage />
        <ModeToggler />
      </ColourModeProvider>,
    )
    fireEvent.click(screen.getByText('toggle-elsewhere'))
    expect(screen.getByRole('radio', { name: 'Dark' })).toBeChecked()
  })
})
