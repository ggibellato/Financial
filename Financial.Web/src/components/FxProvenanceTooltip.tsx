import { Button, Tooltip } from '@fluentui/react-components'
import { Info16Regular } from '@fluentui/react-icons'
import type { FxRateSnapshotDto } from '../api/types'
import { formatDateTime } from '../utils/formatters'

interface FxProvenanceTooltipProps {
  currency: string
  fxRateSnapshot: FxRateSnapshotDto | null
}

export default function FxProvenanceTooltip({ currency, fxRateSnapshot }: FxProvenanceTooltipProps) {
  if (!fxRateSnapshot) return null

  const content = (
    <div>
      <div>
        1 {currency} = {fxRateSnapshot.rate} {fxRateSnapshot.toCurrency}
      </div>
      <div>Source: {fxRateSnapshot.source}</div>
      <div>Retrieved: {formatDateTime(fxRateSnapshot.retrievedAt)}</div>
    </div>
  )

  return (
    <Tooltip content={content} relationship="description">
      <Button appearance="subtle" size="small" icon={<Info16Regular />} aria-label="FX conversion details" />
    </Tooltip>
  )
}
