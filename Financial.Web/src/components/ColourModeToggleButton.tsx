import { WeatherMoon24Regular, WeatherSunny24Regular } from '@fluentui/react-icons'
import { useColourMode } from '../context/ColourModeContext'

function ColourModeToggleButton() {
  const { colourMode, toggleColourMode } = useColourMode()
  const label = colourMode === 'light' ? 'Switch to Dark mode' : 'Switch to Light mode'

  return (
    <button
      type="button"
      className="colour-mode-toggle"
      onClick={toggleColourMode}
      aria-label={label}
      title={label}
    >
      {colourMode === 'light' ? <WeatherMoon24Regular /> : <WeatherSunny24Regular />}
    </button>
  )
}

export default ColourModeToggleButton
