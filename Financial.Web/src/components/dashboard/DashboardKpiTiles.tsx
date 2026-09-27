import { Link } from '@fluentui/react-components'
import ErrorState from '../ErrorState'
import KpiTileSkeleton from './KpiTileSkeleton'
import { formatN2, formatPercentFraction, signClass } from '../../utils/formatters'
import type { PortfolioDashboardDto } from '../../api/types'
import './DashboardKpiTiles.css'

const VALUE_CLASS = 'dashboard-kpi-tiles__value'

interface KpiTileDefinition {
  label: string
  isPercent: boolean
  isSigned: boolean
  value: (summary: PortfolioDashboardDto) => number | null
}

const KPI_TILES: KpiTileDefinition[] = [
  {
    label: 'Market Value',
    isPercent: false,
    isSigned: false,
    value: (summary) => summary.convertedMarketValue,
  },
  {
    label: 'Invested',
    isPercent: false,
    isSigned: false,
    value: (summary) => summary.convertedInvested,
  },
  {
    label: 'Unrealised Gain/Loss',
    isPercent: false,
    isSigned: true,
    value: (summary) => summary.convertedUnrealisedGainLoss,
  },
  {
    label: 'Realised Gain/Loss (Lifetime)',
    isPercent: false,
    isSigned: true,
    value: (summary) => summary.convertedRealisedGainLoss,
  },
  {
    label: 'Income YTD',
    isPercent: false,
    isSigned: false,
    value: (summary) => summary.convertedIncomeYtd,
  },
  {
    label: 'Income Lifetime',
    isPercent: false,
    isSigned: false,
    value: (summary) => summary.convertedIncomeLifetime,
  },
  {
    label: 'Gross XIRR',
    isPercent: true,
    isSigned: true,
    value: (summary) => summary.convertedGrossXirr,
  },
  {
    label: 'Net XIRR (of Tax)',
    isPercent: true,
    isSigned: true,
    value: (summary) => summary.convertedNetXirr,
  },
]

function formatTileValue(value: number | null, isPercent: boolean): string {
  if (value === null) return '—'
  return isPercent ? formatPercentFraction(value) : formatN2(value)
}

function tileValueClass(value: number | null, isSigned: boolean): string {
  if (value === null || !isSigned) return VALUE_CLASS
  return `${VALUE_CLASS} ${signClass(value, VALUE_CLASS)}`
}

function unvaluedHoldingsMessage(summary: PortfolioDashboardDto): string | null {
  if (!summary.isPartial || summary.unvaluedHoldingCount === 0) return null
  const noun = summary.unvaluedHoldingCount === 1 ? 'holding' : 'holdings'
  return `${summary.unvaluedHoldingCount} ${noun} could not be valued; Market Value, Unrealised Gain/Loss and both XIRR figures are incomplete.`
}

function tileLabel(label: string, summary: PortfolioDashboardDto | null): string {
  return summary ? `${label} (${summary.reportingCurrency})` : label
}

interface KpiTileProps {
  label: string
  value: number | null
  isPercent: boolean
  isSigned: boolean
  isLoading: boolean
}

function KpiTile({ label, value, isPercent, isSigned, isLoading }: KpiTileProps) {
  return (
    <div className="dashboard-kpi-tiles__tile">
      <span className="dashboard-kpi-tiles__label">{label}</span>
      {isLoading ? (
        <KpiTileSkeleton />
      ) : (
        <span className={tileValueClass(value, isSigned)}>{formatTileValue(value, isPercent)}</span>
      )}
    </div>
  )
}

interface DashboardKpiTilesProps {
  summary: PortfolioDashboardDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
  onViewMissingPriceHoldings?: () => void
}

export default function DashboardKpiTiles({
  summary,
  isLoading,
  error,
  retry,
  onViewMissingPriceHoldings,
}: DashboardKpiTilesProps) {
  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (summary?.isReportingCurrencyUnavailable) {
    return (
      <ErrorState
        message={`Converted totals unavailable — unable to convert figures into ${summary.reportingCurrency} right now.`}
        onRetry={retry}
      />
    )
  }

  const isPending = isLoading || !summary
  const unvaluedMessage = summary ? unvaluedHoldingsMessage(summary) : null

  return (
    <div className="dashboard-kpi-tiles">
      {summary?.isReportingCurrencyPartial && (
        <p className="dashboard-kpi-tiles__notice dashboard-kpi-tiles__notice--partial" role="status">
          Some figures could not be converted to {summary.reportingCurrency} — showing partial totals.
        </p>
      )}
      <div className="dashboard-kpi-tiles__grid">
        {KPI_TILES.map((tile) => (
          <KpiTile
            key={tile.label}
            label={tileLabel(tile.label, summary)}
            value={summary ? tile.value(summary) : null}
            isPercent={tile.isPercent}
            isSigned={tile.isSigned}
            isLoading={isPending}
          />
        ))}
      </div>
      {unvaluedMessage && (
        <p className="dashboard-kpi-tiles__notice dashboard-kpi-tiles__notice--incomplete" role="status">
          {unvaluedMessage}
          {onViewMissingPriceHoldings && (
            <>
              {' '}
              <Link as="button" type="button" onClick={onViewMissingPriceHoldings}>
                View affected holdings
              </Link>
            </>
          )}
        </p>
      )}
    </div>
  )
}
