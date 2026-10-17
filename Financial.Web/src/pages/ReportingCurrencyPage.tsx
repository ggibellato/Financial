import { MessageBar, MessageBarBody, Radio, RadioGroup, Switch } from '@fluentui/react-components'
import type { RadioGroupOnChangeData } from '@fluentui/react-components'
import ErrorState from '../components/ErrorState'
import LoadingState from '../components/LoadingState'
import { useReportingCurrency } from '../hooks/useReportingCurrency'
import { REPORTING_CURRENCIES } from '../utils/currency'
import './ReportingCurrencyPage.css'

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
          {REPORTING_CURRENCIES.map((currency) => (
            <Radio key={currency} value={currency} label={currency} />
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
