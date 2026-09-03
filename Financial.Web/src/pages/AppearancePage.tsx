import { Radio, RadioGroup } from '@fluentui/react-components'
import type { RadioGroupOnChangeData } from '@fluentui/react-components'
import { useColourMode } from '../context/ColourModeContext'
import type { ColourMode } from '../utils/colourModeStorage'
import './AppearancePage.css'

export default function AppearancePage() {
  const { colourMode, setColourMode } = useColourMode()

  const handleChange = (_event: unknown, data: RadioGroupOnChangeData) => {
    setColourMode(data.value as ColourMode)
  }

  return (
    <section className="appearance-page">
      <header className="appearance-page__header">
        <h2>Appearance</h2>
      </header>
      <div className="appearance-page__field">
        <span className="appearance-page__label">Colour mode</span>
        <RadioGroup value={colourMode} onChange={handleChange} layout="horizontal">
          <Radio value="light" label="Light" />
          <Radio value="dark" label="Dark" />
        </RadioGroup>
      </div>
    </section>
  )
}
