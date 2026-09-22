import { Button, Tooltip } from '@fluentui/react-components'
import { Info16Regular } from '@fluentui/react-icons'
import type { CreditDto } from '../api/types'
import { formatN2, formatPercent1 } from '../utils/formatters'

interface DividendYieldTooltipProps {
  credit: CreditDto
}

export default function DividendYieldTooltip({ credit }: DividendYieldTooltipProps) {
  if (credit.sharesForDividend == null) return null

  const content = (
    <div>
      <div>{credit.sharesForDividend} shares attributed</div>
      {credit.investedAmount != null && <div>Invested: {formatN2(credit.investedAmount)}</div>}
      {credit.marketValueOnDate != null && <div>Market value on date: {formatN2(credit.marketValueOnDate)}</div>}
      {credit.yieldOnInvested != null && <div>Yield on invested: {formatPercent1(credit.yieldOnInvested)}</div>}
      {credit.yieldOnMarket != null && <div>Yield on market: {formatPercent1(credit.yieldOnMarket)}</div>}
    </div>
  )

  return (
    <Tooltip content={content} relationship="description">
      <Button appearance="subtle" size="small" icon={<Info16Regular />} aria-label="Dividend yield details" />
    </Tooltip>
  )
}
