import { Radio, RadioGroup } from '@fluentui/react-components'
import type { RadioGroupOnChangeData } from '@fluentui/react-components'
import FilterTabList from '../FilterTabList'
import type { FilterTabListOption } from '../FilterTabList'
import type { BrokerCurrencyFilter, Currency } from '../../api/types'
import './DashboardCurrencyControls.css'

const CURRENCY_OPTIONS: { value: Currency; label: string }[] = [
  { value: 'GBP', label: 'GBP' },
  { value: 'BRL', label: 'BRL' },
  { value: 'USD', label: 'USD' },
]

const BROKER_FILTER_OPTIONS: readonly FilterTabListOption<BrokerCurrencyFilter>[] = [
  { value: 'ALL', label: 'All currencies' },
  { value: 'BRL', label: 'BRL' },
  { value: 'GBP', label: 'GBP' },
  { value: 'USD', label: 'USD' },
]

interface DashboardCurrencyControlsProps {
  displayCurrency: Currency
  onDisplayCurrencyChange: (currency: Currency) => void
  brokerCurrencyFilter: BrokerCurrencyFilter
  onBrokerCurrencyFilterChange: (filter: BrokerCurrencyFilter) => void
}

export default function DashboardCurrencyControls({
  displayCurrency,
  onDisplayCurrencyChange,
  brokerCurrencyFilter,
  onBrokerCurrencyFilterChange,
}: DashboardCurrencyControlsProps) {
  const handleDisplayCurrencyChange = (_event: unknown, data: RadioGroupOnChangeData) => {
    onDisplayCurrencyChange(data.value as Currency)
  }

  return (
    <div className="dashboard-currency-controls">
      <div className="dashboard-currency-controls__field">
        <span className="dashboard-currency-controls__label">Display currency</span>
        <RadioGroup value={displayCurrency} onChange={handleDisplayCurrencyChange} layout="horizontal">
          {CURRENCY_OPTIONS.map((option) => (
            <Radio key={option.value} value={option.value} label={option.label} />
          ))}
        </RadioGroup>
      </div>
      <FilterTabList
        label="Brokers"
        options={BROKER_FILTER_OPTIONS}
        selected={brokerCurrencyFilter}
        onSelect={onBrokerCurrencyFilterChange}
      />
    </div>
  )
}
