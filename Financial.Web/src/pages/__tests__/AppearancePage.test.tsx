import { fireEvent, render, screen } from '../../test/renderWithFluent'
import { afterEach, describe, expect, it } from 'vitest'
import AppearancePage from '../AppearancePage'
import { ColourModeProvider, useColourMode } from '../../context/ColourModeContext'

function ModeDisplay() {
  const { colourMode } = useColourMode()
  return <div data-testid="mode">{colourMode}</div>
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

  it('selecting_dark_updates_the_shared_colour_mode', async () => {
    render(
      <ColourModeProvider>
        <AppearancePage />
        <ModeDisplay />
      </ColourModeProvider>,
    )
    expect(screen.getByRole('radio', { name: 'Light' })).toBeChecked()

    fireEvent.click(screen.getByRole('radio', { name: 'Dark' }))

    expect(await screen.findByTestId('mode')).toHaveTextContent('dark')
    expect(screen.getByRole('radio', { name: 'Dark' })).toBeChecked()
  })
})
