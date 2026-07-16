import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useAssetSummary } from '../hooks/useAssetSummary'
import { formatN2, formatN8, formatShortDate, pad } from '../utils/formatters'
import './AssetSummaryTab.css'

function formatPercent(value: number): string {
  return new Intl.NumberFormat(undefined, {
    style: 'percent',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

function formatDateTime(isoString: string | null): string {
  if (!isoString) return '—'
  const d = new Date(isoString)
  if (Number.isNaN(d.getTime())) return isoString
  return `${formatShortDate(isoString)} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export default function AssetSummaryTab() {
  const {
    asset,
    isLoadingAsset,
    assetError,
    retryAsset,
    price,
    isLoadingPrice,
    priceError,
    canRefresh,
    refresh,
    showCurrentSection,
    totalCurrentValue,
    resultPercent,
    totalCurrentPlusCredits,
    resultWithCreditsPercent,
    xirr,
    xirrWithCredits,
  } = useAssetSummary()

  if (isLoadingAsset) {
    return <LoadingState />
  }

  if (assetError) {
    return <ErrorState message={assetError} onRetry={retryAsset} />
  }

  if (!asset) {
    return null
  }

  const realizedGainLossClass =
    asset.realizedGainLoss >= 0 ? 'asset-summary__value--green' : 'asset-summary__value--red'
  const resultClass =
    resultPercent >= 0 ? 'asset-summary__value--green' : 'asset-summary__value--red'
  const resultWithCreditsClass =
    resultWithCreditsPercent >= 0 ? 'asset-summary__value--green' : 'asset-summary__value--red'
  const xirrClass = xirr === null ? '' : xirr >= 0 ? 'asset-summary__value--green' : 'asset-summary__value--red'
  const xirrWithCreditsClass =
    xirrWithCredits === null ? '' : xirrWithCredits >= 0 ? 'asset-summary__value--green' : 'asset-summary__value--red'

  return (
    <div className="asset-summary">
      <div className="asset-summary__grid">
        <div className="asset-summary__field">
          <span className="asset-summary__label">Quantity</span>
          <span className="asset-summary__value">{formatN8(asset.quantity)}</span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Average Price</span>
          <span className="asset-summary__value">{formatN2(asset.averagePrice)}</span>
        </div>

        <div className="asset-summary__field">
          <span className="asset-summary__label">ISIN</span>
          <span className="asset-summary__value">{asset.isin || '—'}</span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Country</span>
          <span className="asset-summary__value">{asset.country}</span>
        </div>

        <div className="asset-summary__field">
          <span className="asset-summary__label">Local Type</span>
          <span className="asset-summary__value">{asset.localTypeCode || '—'}</span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Asset Class</span>
          <span className="asset-summary__value">{asset.class}</span>
        </div>

        <div className="asset-summary__separator" />

        <div className="asset-summary__field">
          <span className="asset-summary__label">Total Bought</span>
          <span className="asset-summary__value asset-summary__value--green">
            {formatN2(asset.totalBought)}
          </span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Total Sold</span>
          <span className="asset-summary__value asset-summary__value--red">
            {formatN2(asset.totalSold)}
          </span>
        </div>

        <div className="asset-summary__field">
          <span className="asset-summary__label">Total Credits</span>
          <span className="asset-summary__value asset-summary__value--blue">
            {formatN2(asset.totalCredits)}
          </span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Realized Gain/Loss</span>
          <span className={`asset-summary__value ${realizedGainLossClass}`}>
            {formatN2(asset.realizedGainLoss)}
          </span>
        </div>

        {showCurrentSection && (
          <>
            <div className="asset-summary__separator" />

            <div className="asset-summary__section-header">
              <span className="asset-summary__section-title">Current</span>
              <button
                className="asset-summary__refresh-btn"
                type="button"
                onClick={refresh}
                disabled={!canRefresh}
              >
                Refresh
              </button>
            </div>

            <div className="asset-summary__field">
              <span className="asset-summary__label">Current Value</span>
              <span className="asset-summary__value">
                {isLoadingPrice || !price ? '—' : formatN2(price.price)}
              </span>
            </div>
            <div className="asset-summary__field">
              <span className="asset-summary__label">As of</span>
              <span className="asset-summary__value">
                {isLoadingPrice ? '—' : formatDateTime(price?.asOf ?? null)}
              </span>
            </div>

            {price && !isLoadingPrice && (
              <>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">Total Current Value</span>
                  <span className="asset-summary__value">{formatN2(totalCurrentValue)}</span>
                </div>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">Result %</span>
                  <span className={`asset-summary__value ${resultClass}`}>
                    {formatPercent(resultPercent)}
                  </span>
                </div>

                <div className="asset-summary__field">
                  <span className="asset-summary__label">Total Current + Credits</span>
                  <span className="asset-summary__value">{formatN2(totalCurrentPlusCredits)}</span>
                </div>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">Result % with Credits</span>
                  <span className={`asset-summary__value ${resultWithCreditsClass}`}>
                    {formatPercent(resultWithCreditsPercent)}
                  </span>
                </div>

                <div className="asset-summary__field">
                  <span className="asset-summary__label">XIRR</span>
                  <span className={`asset-summary__value ${xirrClass}`}>
                    {xirr === null ? '—' : formatPercent(xirr)}
                  </span>
                </div>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">XIRR w/ Credits</span>
                  <span className={`asset-summary__value ${xirrWithCreditsClass}`}>
                    {xirrWithCredits === null ? '—' : formatPercent(xirrWithCredits)}
                  </span>
                </div>
              </>
            )}

            {priceError && (
              <div className="asset-summary__field asset-summary__field--full">
                <span className="asset-summary__label">Status</span>
                <span className="asset-summary__value asset-summary__value--error">
                  {priceError}
                </span>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
