import { Button, Tooltip } from '@fluentui/react-components'
import { Info16Regular } from '@fluentui/react-icons'
import type { CreditDto } from '../api/types'
import { formatN2, formatShortDate } from '../utils/formatters'

interface DividendYieldTooltipProps {
  credit: CreditDto
}

export default function DividendYieldTooltip({ credit }: DividendYieldTooltipProps) {
  if (credit.attributedShares == null) return null

  const label =
    credit.sharesForDividend == null
      ? `${credit.attributedShares} shares attributed (entire position - none specified)`
      : `${credit.attributedShares} shares attributed`

  const content = (
    <div>
      <div>{label}</div>
      {credit.averageCostPerShare != null && <div>Average cost/share: {formatN2(credit.averageCostPerShare)}</div>}
      {credit.investedAmount != null && <div>Total bought: {formatN2(credit.investedAmount)}</div>}
      {credit.priceOnDate != null && <div>Share price on {formatShortDate(credit.date)}: {formatN2(credit.priceOnDate)}</div>}
      {credit.marketValueOnDate != null && <div>Total current value: {formatN2(credit.marketValueOnDate)}</div>}
    </div>
  )

  return (
    <Tooltip content={content} relationship="description">
      <Button appearance="subtle" size="small" icon={<Info16Regular />} aria-label="Dividend yield details" />
    </Tooltip>
  )
}
