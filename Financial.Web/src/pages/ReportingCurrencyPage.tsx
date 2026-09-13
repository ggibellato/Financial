import { MessageBar, MessageBarBody, Radio, RadioGroup, Switch } from '@fluentui/react-components'
import type { RadioGroupOnChangeData } from '@fluentui/react-components'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useReportingCurrency } from '../hooks/useReportingCurrency'
import './ReportingCurrencyPage.css'

const CURRENCY_OPTIONS = [
  { value: 'GBP', label: 'GBP' },
  { value: 'BRL', label: 'BRL' },
  { value: 'USD', label: 'USD' },
]

export default function ReportingCurrencyPage() {
  const { currency, enabled, isLoading, error, retry, saveError, setCurrency, setEnabled } = useReportingCurrency()

  const handleChange = (_event: unknown, data: RadioGroupOnChangeData) => {
    void setCurrency(data.value)
  }

  const handleEnabledChange = (_event: unknown, data: { checked: boolean }) => {
    void setEnabled(data.checked)
  }

  if (isLoading) {
    return <LoadingState message="Loading reporting currency…" />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  return (
    <section className="reporting-currency-page">
      <header className="reporting-currency-page__header">
        <h2>Reporting Currency</h2>
      </header>
      <Switch label="Show converted totals" checked={enabled ?? true} onChange={handleEnabledChange} />
      <div className="reporting-currency-page__field">
        <span className="reporting-currency-page__label">Currency</span>
        <RadioGroup value={currency ?? undefined} onChange={handleChange} layout="horizontal" disabled={!enabled}>
          {CURRENCY_OPTIONS.map((option) => (
            <Radio key={option.value} value={option.value} label={option.label} />
          ))}
        </RadioGroup>
      </div>
      {saveError && (
        <MessageBar intent="error">
          <MessageBarBody>{saveError}</MessageBarBody>
        </MessageBar>
      )}
    </section>
  )
}
