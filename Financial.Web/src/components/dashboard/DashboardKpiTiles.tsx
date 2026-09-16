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
  native: (summary: PortfolioDashboardDto) => number | null
  converted: (summary: PortfolioDashboardDto) => number | null
}

const KPI_TILES: KpiTileDefinition[] = [
  {
    label: 'Market Value',
    isPercent: false,
    isSigned: false,
    native: (summary) => summary.marketValue,
    converted: (summary) => summary.convertedMarketValue,
  },
  {
    label: 'Invested',
    isPercent: false,
    isSigned: false,
    native: (summary) => summary.invested,
    converted: (summary) => summary.convertedInvested,
  },
  {
    label: 'Unrealised Gain/Loss',
    isPercent: false,
    isSigned: true,
    native: (summary) => summary.unrealisedGainLoss,
    converted: (summary) => summary.convertedUnrealisedGainLoss,
  },
  {
    label: 'Realised Gain/Loss (Lifetime)',
    isPercent: false,
    isSigned: true,
    native: (summary) => summary.realisedGainLoss,
    converted: (summary) => summary.convertedRealisedGainLoss,
  },
  {
    label: 'Income YTD',
    isPercent: false,
    isSigned: false,
    native: (summary) => summary.incomeYtd,
    converted: (summary) => summary.convertedIncomeYtd,
  },
  {
    label: 'Income Lifetime',
    isPercent: false,
    isSigned: false,
    native: (summary) => summary.incomeLifetime,
    converted: (summary) => summary.convertedIncomeLifetime,
  },
  {
    label: 'Gross XIRR',
    isPercent: true,
    isSigned: true,
    native: (summary) => summary.grossXirr,
    converted: (summary) => summary.convertedGrossXirr,
  },
  {
    label: 'Net XIRR (of Tax)',
    isPercent: true,
    isSigned: true,
    native: (summary) => summary.netXirr,
    converted: (summary) => summary.convertedNetXirr,
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

function ConvertedKpiTiles({ summary, retry }: { summary: PortfolioDashboardDto; retry: () => void }) {
  if (!summary.isReportingCurrencyEnabled) return null

  if (summary.isReportingCurrencyUnavailable) {
    return <ErrorState message="Converted totals unavailable — showing native-currency figures only." onRetry={retry} />
  }

  return (
    <div className="dashboard-kpi-tiles__converted">
      <h4 className="dashboard-kpi-tiles__converted-heading">Converted to {summary.reportingCurrency}</h4>
      {summary.isReportingCurrencyPartial && (
        <p className="dashboard-kpi-tiles__notice" role="status">
          Some figures could not be converted to {summary.reportingCurrency} — showing partial totals.
        </p>
      )}
      <div className="dashboard-kpi-tiles__grid">
        {KPI_TILES.map((tile) => (
          <KpiTile
            key={tile.label}
            label={`${tile.label} (converted to ${summary.reportingCurrency})`}
            value={tile.converted(summary)}
            isPercent={tile.isPercent}
            isSigned={tile.isSigned}
            isLoading={false}
          />
        ))}
      </div>
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

  const isPending = isLoading || !summary
  const unvaluedMessage = summary ? unvaluedHoldingsMessage(summary) : null

  return (
    <div className="dashboard-kpi-tiles">
      <div className="dashboard-kpi-tiles__grid">
        {KPI_TILES.map((tile) => (
          <KpiTile
            key={tile.label}
            label={tile.label}
            value={summary ? tile.native(summary) : null}
            isPercent={tile.isPercent}
            isSigned={tile.isSigned}
            isLoading={isPending}
          />
        ))}
      </div>
      {unvaluedMessage && (
        <p className="dashboard-kpi-tiles__notice" role="status">
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
      {summary && <ConvertedKpiTiles summary={summary} retry={retry} />}
    </div>
  )
}
