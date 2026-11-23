import { Text } from '@fluentui/react-components'
import type { InterestRateDto, SavingsSummaryDto } from '../api/types'
import { formatDayMonthYearUtc, formatPercentN2 } from '../utils/formatters'

const rateText = (rate: InterestRateDto, preposition: string) =>
  `${formatPercentN2(rate.annualPercent)} a year ${preposition} ${formatDayMonthYearUtc(rate.effectiveFrom)}`

function scheduleText(savings: SavingsSummaryDto): string {
  return savings.interestFrequency === 'Daily'
    ? 'Interest paid daily'
    : `Interest paid monthly on day ${savings.paymentDay}`
}

export default function SavingsTermsSummary({ savings }: { savings: SavingsSummaryDto }) {
  const rates = [
    savings.currentRate ? rateText(savings.currentRate, 'since') : 'No rate in force yet',
    savings.upcomingRate && rateText(savings.upcomingRate, 'from'),
  ].filter(Boolean)

  return (
    <>
      <Text>{rates.join(' · ')}</Text>
      <Text>{scheduleText(savings)}</Text>
    </>
  )
}
