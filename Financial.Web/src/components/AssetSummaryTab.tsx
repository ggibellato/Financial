import type { ReactNode } from 'react'
import { Button } from '@fluentui/react-components'
import { ArrowSyncRegular } from '@fluentui/react-icons'
import type { AssetPriceDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useAssetSummary } from '../hooks/useAssetSummary'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { formatDateTime, formatN2, formatN8, formatPercentFraction, formatShortDateUtc, signClass } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import './AssetSummaryTab.css'
import MissingValue from './MissingValue'

/** A live quote carries a time of day; a price read from Price History carries only a date, and
 * is read as UTC so the reader's time zone cannot shift it onto the previous day. */
function formatAsOf(price: AssetPriceDto | null): ReactNode {
  if (!price) return <MissingValue />
  if (price.asOf) return formatDateTime(price.asOf)
  if (price.asOfDate) return formatShortDateUtc(price.asOfDate)
  return <MissingValue />
}

function formatPercentPoints(percent: number): string {
  return formatPercentFraction(percent / 100)
}

export default function AssetSummaryTab() {
  const { scope } = useSelectedNode()
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
    portfolioWeight,
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

  const marketValue = asset.marketValue
  const totalCurrentPlusCredits = marketValue === null ? null : marketValue + asset.totalCredits
  const { resultPercent, resultWithCreditsPercent } = asset
  const xirr = asset.priceOnlyReturn
  const xirrWithCredits = asset.totalReturn

  const resultClass = resultPercent === null ? '' : signClass(resultPercent, 'asset-summary__value')
  const resultWithCreditsClass = resultWithCreditsPercent === null ? '' : signClass(resultWithCreditsPercent, 'asset-summary__value')
  const xirrClass = xirr === null ? '' : signClass(xirr, 'asset-summary__value')
  const xirrWithCreditsClass = xirrWithCredits === null ? '' : signClass(xirrWithCredits, 'asset-summary__value')

  return (
    <div className="asset-summary">
      <div className="asset-summary__grid">
        <div className="asset-summary__field">
          <span className="asset-summary__label">Quantity</span>
          <span className="asset-summary__value">{formatN8(asset.quantity)}</span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Average price</span>
          <span className="asset-summary__value">{formatN2(asset.averagePrice)}</span>
        </div>

        <div className="asset-summary__field">
          <span className="asset-summary__label">ISIN</span>
          <span className="asset-summary__value">{asset.isin || <MissingValue />}</span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Country</span>
          <span className="asset-summary__value">{asset.country}</span>
        </div>

        <div className="asset-summary__field">
          <span className="asset-summary__label">Tax jurisdiction</span>
          <span className="asset-summary__value">
            {asset.taxJurisdictions.length > 0 ? asset.taxJurisdictions.join(', ') : <MissingValue />}
          </span>
        </div>

        <div className="asset-summary__field">
          <span className="asset-summary__label">Local type</span>
          <span className="asset-summary__value">{asset.localTypeCode || <MissingValue />}</span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Asset class</span>
          <span className="asset-summary__value">{asset.class}</span>
        </div>

        <div className="asset-summary__separator" />

        <div className="asset-summary__field">
          <span className="asset-summary__label">Total bought</span>
          <span className="asset-summary__value asset-summary__value--green">
            {formatN2(asset.totalBought)}
          </span>
        </div>
        <div className="asset-summary__field">
          <span className="asset-summary__label">Total sold</span>
          <span className="asset-summary__value asset-summary__value--red">
            {formatN2(asset.totalSold)}
          </span>
        </div>

        <div className="asset-summary__field asset-summary__field--full">
          <span className="asset-summary__label">Total credits</span>
          <span className="asset-summary__value asset-summary__value--blue">
            {formatN2(asset.totalCredits)}
          </span>
        </div>

        {scope === 'historic' && (
          <>
            <div className="asset-summary__separator" />

            <div className="asset-summary__section-header">
              <span className="asset-summary__section-title">Realized</span>
            </div>

            <div className="asset-summary__field">
              <span className="asset-summary__label">Realized gain/loss</span>
              <span
                className={`asset-summary__value ${signClass(asset.realizedGainLoss, 'asset-summary__value')}`}
              >
                {formatN2(asset.realizedGainLoss)}
              </span>
            </div>
            <div className="asset-summary__field">
              <span className="asset-summary__label">Realized (shares only)</span>
              <span
                className={`asset-summary__value ${signClass(asset.realizedGainLossSharesOnly, 'asset-summary__value')}`}
              >
                {formatN2(asset.realizedGainLossSharesOnly)}
              </span>
            </div>
            <div className="asset-summary__field">
              <span className="asset-summary__label">XIRR</span>
              <span className={`asset-summary__value ${xirrWithCreditsClass}`}>
                {orMissing(xirrWithCredits, formatPercentFraction)}
              </span>
            </div>
            <div className="asset-summary__field">
              <span className="asset-summary__label">XIRR (Shares only)</span>
              <span className={`asset-summary__value ${xirrClass}`}>
                {orMissing(xirr, formatPercentFraction)}
              </span>
            </div>
            <div className="asset-summary__field">
              <span className="asset-summary__label">Portfolio weight</span>
              <span className="asset-summary__value">
                {orMissing(portfolioWeight, formatPercentPoints)}
              </span>
            </div>
          </>
        )}

        {scope === 'active' && showCurrentSection && (
          <>
            <div className="asset-summary__separator" />

            <div className="asset-summary__section-header">
              <span className="asset-summary__section-title">Current</span>
              <Button appearance="primary" icon={<ArrowSyncRegular />} onClick={refresh} disabled={!canRefresh}>
                Refresh
              </Button>
            </div>

            <div className="asset-summary__field">
              <span className="asset-summary__label">Current value</span>
              <span className="asset-summary__value">
                {isLoadingPrice || !price ? <MissingValue /> : formatN2(price.price)}
                {!isLoadingPrice && price?.isManual && (
                  <span
                    className="asset-summary__manual-badge"
                    title="This value came from a manually-entered price, not a live fetch."
                  >
                    {' '}
                    (Manual)
                  </span>
                )}
              </span>
            </div>
            <div className="asset-summary__field">
              <span className="asset-summary__label">As of</span>
              <span className="asset-summary__value">
                {isLoadingPrice ? <MissingValue /> : formatAsOf(price)}
                {!isLoadingPrice && asset.marketStatus === 'Stale' && (
                  <span className="asset-summary__stale-badge" title="This price is older than the most recent weekday.">
                    {' '}
                    (Stale)
                  </span>
                )}
                {!isLoadingPrice && asset.marketStatus === 'Unavailable' && (
                  <span className="asset-summary__stale-badge" title="No price or value has been recorded for this holding yet.">
                    {' '}
                    (Unavailable)
                  </span>
                )}
              </span>
            </div>

            {!isLoadingPrice && (
              <>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">Total current value</span>
                  <span className="asset-summary__value">{orMissing(marketValue, formatN2)}</span>
                </div>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">Result %</span>
                  <span className={`asset-summary__value ${resultClass}`}>
                    {orMissing(resultPercent, formatPercentPoints)}
                  </span>
                </div>

                <div className="asset-summary__field">
                  <span className="asset-summary__label">Total current + credits</span>
                  <span className="asset-summary__value">
                    {orMissing(totalCurrentPlusCredits, formatN2)}
                  </span>
                </div>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">Result % with Credits</span>
                  <span className={`asset-summary__value ${resultWithCreditsClass}`}>
                    {orMissing(resultWithCreditsPercent, formatPercentPoints)}
                  </span>
                </div>

                <div className="asset-summary__field">
                  <span className="asset-summary__label">XIRR</span>
                  <span className={`asset-summary__value ${xirrClass}`}>
                    {orMissing(xirr, formatPercentFraction)}
                  </span>
                </div>
                <div className="asset-summary__field">
                  <span className="asset-summary__label">XIRR w/ credits</span>
                  <span className={`asset-summary__value ${xirrWithCreditsClass}`}>
                    {orMissing(xirrWithCredits, formatPercentFraction)}
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
