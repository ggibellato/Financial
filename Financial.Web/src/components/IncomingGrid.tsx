import { Checkbox } from '@fluentui/react-components'
import type { IncomeSourceTotalDto, TitheSummaryDto } from '../api/types'
import { formatMonthYear, formatN2, formatN2OrDash } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'

interface IncomingGridProps {
  incomeTotals: IncomeSourceTotalDto[]
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
        label="Income totals"
        columns={[
          {
            key: 'source',
            header: 'Source',
            render: (i: IncomeSourceTotalDto) => i.name,
            sortAccessor: (i: IncomeSourceTotalDto) => i.name,
          },
          {
            key: 'gross',
            header: 'Gross',
            numeric: true,
            render: (i: IncomeSourceTotalDto) => formatN2OrDash(i.grossValue),
            sortAccessor: (i: IncomeSourceTotalDto) => i.grossValue,
          },
          {
            key: 'net',
            header: 'Net',
            numeric: true,
            render: (i: IncomeSourceTotalDto) => formatN2(i.netValue),
            sortAccessor: (i: IncomeSourceTotalDto) => i.netValue,
          },
        ]}
        rows={incomeTotals}
        rowKey={(i) => i.incomeSourceId}
        emptyMessage="No income recorded for this month"
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
