import { Badge, Skeleton, SkeletonItem, Text } from '@fluentui/react-components'
import type { PotBalanceDto, PotsSummaryDto } from '../api/types'
import { POTS_LOAD_ERROR } from '../hooks/usePots'
import { formatLongMonthYear, formatN2, formatPercentN2 } from '../utils/formatters'
import ErrorState from './ErrorState'
import TotalsGrid from './TotalsGrid'
import TruncatedText from './TruncatedText'

const COLUMNS = [
  {
    key: 'name',
    header: 'Pot',
    render: (pot: PotBalanceDto) => (
      <span className="pots-tab__name">
        <TruncatedText text={pot.name} />
        {!pot.isActive && (
          <Badge appearance="outline" color="informative">
            Inactive
          </Badge>
        )}
      </span>
    ),
    sortAccessor: (pot: PotBalanceDto) => pot.name,
  },
  {
    key: 'splitPercentage',
    header: 'Split %',
    numeric: true,
    render: (pot: PotBalanceDto) => formatPercentN2(pot.splitPercentage),
    sortAccessor: (pot: PotBalanceDto) => pot.splitPercentage,
  },
  {
    key: 'balance',
    header: 'Balance',
    numeric: true,
    render: (pot: PotBalanceDto) => formatN2(pot.balance),
    sortAccessor: (pot: PotBalanceDto) => pot.balance,
  },
]

interface PotsTabProps {
  summary: PotsSummaryDto | null
  hasError: boolean
  onRetry: () => void
  year: number
  month: number
}

export default function PotsTab({ summary, hasError, onRetry, year, month }: PotsTabProps) {
  return (
    <>
      <Text as="h2" size={400} weight="semibold">
        Pots
      </Text>
      {hasError ? (
        <ErrorState message={POTS_LOAD_ERROR} onRetry={onRetry} />
      ) : !summary ? (
        <Skeleton aria-label="Loading pots" className="account-screen__grid-skeleton">
          <SkeletonItem />
          <SkeletonItem />
          <SkeletonItem />
        </Skeleton>
      ) : (
        <div className="account-screen__grid-scroll pots-tab__content">
          <Text>Balances at end of {formatLongMonthYear(year, month)}</Text>
          <TotalsGrid
            label="Pots"
            columns={COLUMNS}
            rows={summary.pots}
            rowKey={(pot) => pot.id}
            emptyMessage="This account has no pots yet."
            footerItems={summary.pots.length > 0 ? [{ label: `Pots total ${summary.currency}`, value: formatN2(summary.total) }] : []}
          />
        </div>
      )}
    </>
  )
}
