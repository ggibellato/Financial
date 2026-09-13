import { MessageBar, MessageBarBody, Radio, RadioGroup } from '@fluentui/react-components'
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
  const { currency, isLoading, error, retry, saveError, setCurrency } = useReportingCurrency()

  const handleChange = (_event: unknown, data: RadioGroupOnChangeData) => {
    void setCurrency(data.value)
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
      <div className="reporting-currency-page__field">
        <span className="reporting-currency-page__label">Currency</span>
        <RadioGroup value={currency ?? undefined} onChange={handleChange} layout="horizontal">
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
