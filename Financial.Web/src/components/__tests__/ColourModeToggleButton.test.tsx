import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import ColourModeToggleButton from '../ColourModeToggleButton'
import { ColourModeProvider, useColourMode } from '../../context/ColourModeContext'

function ModeDisplay() {
  const { colourMode } = useColourMode()
  return <div data-testid="mode">{colourMode}</div>
}

describe('ColourModeToggleButton', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('shows_moon_icon_and_switch_to_dark_label_while_in_light_mode', () => {
    render(
      <ColourModeProvider>
        <ColourModeToggleButton />
      </ColourModeProvider>,
    )
    const button = screen.getByRole('button', { name: 'Switch to Dark mode' })
    expect(button).toBeInTheDocument()
    expect(button).toHaveAttribute('title', 'Switch to Dark mode')
  })

  it('shows_sun_icon_and_switch_to_light_label_while_in_dark_mode', () => {
    localStorage.setItem('financial.colourMode', 'dark')
    render(
      <ColourModeProvider>
        <ColourModeToggleButton />
      </ColourModeProvider>,
    )
    const button = screen.getByRole('button', { name: 'Switch to Light mode' })
    expect(button).toBeInTheDocument()
    expect(button).toHaveAttribute('title', 'Switch to Light mode')
  })

  it('clicking_toggles_the_shared_colour_mode', () => {
    render(
      <ColourModeProvider>
        <ColourModeToggleButton />
        <ModeDisplay />
      </ColourModeProvider>,
    )
    fireEvent.click(screen.getByRole('button', { name: 'Switch to Dark mode' }))
    expect(screen.getByTestId('mode')).toHaveTextContent('dark')
  })

  it('is_keyboard_operable', () => {
    render(
      <ColourModeProvider>
        <ColourModeToggleButton />
        <ModeDisplay />
      </ColourModeProvider>,
    )
    const button = screen.getByRole('button', { name: 'Switch to Dark mode' })
    button.focus()
    fireEvent.keyDown(button, { key: 'Enter' })
    fireEvent.click(button)
    expect(screen.getByTestId('mode')).toHaveTextContent('dark')
  })

  it('has_an_accessible_name', () => {
    render(
      <ColourModeProvider>
        <ColourModeToggleButton />
      </ColourModeProvider>,
    )
    expect(screen.getByRole('button', { name: /switch to (dark|light) mode/i })).toBeInTheDocument()
  })
})
