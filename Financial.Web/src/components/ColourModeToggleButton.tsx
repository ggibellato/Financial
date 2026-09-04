import { Button } from '@fluentui/react-components'
import { WeatherMoon24Regular, WeatherSunny24Regular } from '@fluentui/react-icons'
import { useColourMode } from '../context/ColourModeContext'

function ColourModeToggleButton() {
  const { colourMode, toggleColourMode } = useColourMode()
  const label = colourMode === 'light' ? 'Switch to Dark mode' : 'Switch to Light mode'

  return (
    <Button
      appearance="subtle"
      icon={colourMode === 'light' ? <WeatherMoon24Regular /> : <WeatherSunny24Regular />}
      onClick={toggleColourMode}
      aria-label={label}
      title={label}
    />
  )
}

export default ColourModeToggleButton
