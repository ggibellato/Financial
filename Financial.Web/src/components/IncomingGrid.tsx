import { Checkbox } from '@fluentui/react-components'
import type { IncomeTotal } from '../hooks/useMonthly'
import type { TitheSummaryDto } from '../api/types'
import { formatMonthYear, formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'

interface IncomingGridProps {
  incomeTotals: IncomeTotal[]
  totalIncoming: number
  titheSummary: TitheSummaryDto | null
  carryForwardUpdating: boolean
  onToggleCarryForward: (included: boolean) => void
  /** Outcome of the last carry-forward toggle. Shares useMonthly's single "last action" slot with
   * other grids' actions, so it can also reflect an unrelated action - an accepted trade-off of
   * reusing that shared state rather than introducing a dedicated field. */
  carryForwardActionError?: string | null
  carryForwardActionWarning?: string | null
}

export default function IncomingGrid({
  incomeTotals,
  totalIncoming,
  titheSummary,
  carryForwardUpdating,
  onToggleCarryForward,
  carryForwardActionError,
  carryForwardActionWarning,
}: IncomingGridProps) {
  const carryForward = titheSummary?.carryForward ?? null

  return (
    <>
      {carryForwardActionError && (
        <p className="monthly-page__action-error" role="alert">
          {carryForwardActionError}
        </p>
      )}
      {carryForwardActionWarning && (
        <p className="monthly-page__action-warning" role="status">
          {carryForwardActionWarning}
        </p>
      )}
      <TotalsGrid
        columns={[
          {
            key: 'source',
            header: 'Source',
            render: (i: IncomeTotal) => i.source,
            sortAccessor: (i: IncomeTotal) => i.source,
          },
          {
            key: 'gross',
            header: 'Gross',
            numeric: true,
            render: (i: IncomeTotal) => (i.grossValue != null ? formatN2(i.grossValue) : '—'),
            sortAccessor: (i: IncomeTotal) => i.grossValue,
          },
          {
            key: 'net',
            header: 'Net',
            numeric: true,
            render: (i: IncomeTotal) => formatN2(i.netValue),
            sortAccessor: (i: IncomeTotal) => i.netValue,
          },
        ]}
        rows={incomeTotals}
        rowKey={(i) => i.source}
        footerItems={[
          { label: 'Total Incoming', value: formatN2(totalIncoming) },
          ...(titheSummary
            ? [
                { label: 'Calculated Tithe', value: formatN2(titheSummary.calculatedTithe) },
                { label: 'Tithe Balance', value: formatN2(titheSummary.titheBalance) },
              ]
            : []),
          ...(carryForward
            ? [
                {
                  label: 'Carry forward',
                  value: (
                    <Checkbox
                      label={`${formatN2(carryForward.amount)} from ${formatMonthYear(new Date(carryForward.fromYear, carryForward.fromMonth - 1, 1))}`}
                      checked={carryForward.included}
                      disabled={carryForwardUpdating}
                      onChange={(_, data) => onToggleCarryForward(data.checked === true)}
                    />
                  ),
                },
              ]
            : []),
        ]}
      />
    </>
  )
}
