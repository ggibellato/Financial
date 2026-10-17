import { Radio, RadioGroup } from '@fluentui/react-components'
import type { RadioGroupOnChangeData } from '@fluentui/react-components'
import FilterTabList from '../FilterTabList'
import type { FilterTabListOption } from '../FilterTabList'
import type { BrokerCurrencyFilter, Currency } from '../../api/types'
import { CURRENCIES, REPORTING_CURRENCIES } from '../../utils/currency'
import './DashboardCurrencyControls.css'

const BROKER_FILTER_OPTIONS: readonly FilterTabListOption<BrokerCurrencyFilter>[] = [
  { value: 'ALL', label: 'All currencies' },
  ...CURRENCIES.map((currency) => ({ value: currency, label: currency })),
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
          {REPORTING_CURRENCIES.map((currency) => (
            <Radio key={currency} value={currency} label={currency} />
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
