import { useId, type ReactNode } from 'react'
import { Button } from '@fluentui/react-components'
import { ArrowSyncRegular } from '@fluentui/react-icons'
import type { AssetPriceDto } from '../api/types'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import PriceBadge from './PriceBadge'
import { useAssetSummary } from '../hooks/useAssetSummary'
import { useSelectedNode } from '../context/SelectedNodeContext'
import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'
import { formatDateTime, formatN2, formatN8, formatPercentFraction, formatShortDateUtc, signClass } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import MissingValue from './MissingValue'
import './AssetSummaryTab.css'

const MANUAL_PRICE_MEANING = 'This price was entered manually, not fetched live.'
const STALE_PRICE_MEANING = 'This price is older than the most recent weekday.'
const NO_PRICE_MEANING = 'No price or value has been recorded for this holding yet.'
const VALUE_CLASS = 'asset-summary__value'

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

function signedClass(value: number | null): string | undefined {
  return value === null ? undefined : signClass(value, VALUE_CLASS)
}

interface FieldProps {
  label: string
  children: ReactNode
  valueClassName?: string
  full?: boolean
}

function Field({ label, children, valueClassName, full = false }: FieldProps) {
  return (
    <div className={full ? 'asset-summary__field asset-summary__field--full' : 'asset-summary__field'}>
      <dt className="asset-summary__label">{label}</dt>
      <dd className={valueClassName ? `${VALUE_CLASS} ${valueClassName}` : VALUE_CLASS}>{children}</dd>
    </div>
  )
}

export default function AssetSummaryTab() {
  const { scope } = useSelectedNode()
  const hidden = useVisuallyHiddenStyles()
  const realisedTitleId = useId()
  const currentTitleId = useId()
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

  const inCurrency = (label: string) => (asset.currency ? `${label} (${asset.currency})` : label)
  const marketValue = asset.marketValue
  const totalCurrentPlusCredits = marketValue === null ? null : marketValue + asset.totalCredits
  const { resultPercent, resultWithCreditsPercent } = asset
  const xirr = asset.priceOnlyReturn
  const xirrWithCredits = asset.totalReturn

  return (
    <div className="asset-summary">
      <dl className="asset-summary__grid">
        <Field label="Quantity">{formatN8(asset.quantity)}</Field>
        <Field label={inCurrency('Average price')}>{formatN2(asset.averagePrice)}</Field>
        <Field label="ISIN">{asset.isin || <MissingValue label="None" />}</Field>
        <Field label="Country">{asset.country}</Field>
        <Field label="Tax jurisdiction">
          {asset.taxJurisdictions.length > 0 ? asset.taxJurisdictions.join(', ') : <MissingValue label="None" />}
        </Field>
        <Field label="Local type">{asset.localTypeCode || <MissingValue label="None" />}</Field>
        <Field label="Asset class">{asset.class}</Field>
      </dl>

      <dl className="asset-summary__grid asset-summary__group">
        <Field label={inCurrency('Total bought')}>{formatN2(asset.totalBought)}</Field>
        <Field label={inCurrency('Total sold')}>{formatN2(asset.totalSold)}</Field>
        <Field label={inCurrency('Total credits')} full>
          {formatN2(asset.totalCredits)}
        </Field>
      </dl>

      {scope === 'historic' && (
        <section className="asset-summary__group" aria-labelledby={realisedTitleId}>
          <div className="asset-summary__section-header">
            <h3 id={realisedTitleId} className="asset-summary__section-title">
              Realised
            </h3>
          </div>
          <dl className="asset-summary__grid">
            <Field label={inCurrency('Realised gain/loss')} valueClassName={signedClass(asset.realisedGainLoss)}>
              {formatN2(asset.realisedGainLoss)}
            </Field>
            <Field label={inCurrency('Realised (shares only)')} valueClassName={signedClass(asset.realisedGainLossSharesOnly)}>
              {formatN2(asset.realisedGainLossSharesOnly)}
            </Field>
            <Field label="XIRR with credits" valueClassName={signedClass(xirrWithCredits)}>
              {orMissing(xirrWithCredits, formatPercentFraction)}
            </Field>
            <Field label="XIRR (shares only)" valueClassName={signedClass(xirr)}>
              {orMissing(xirr, formatPercentFraction)}
            </Field>
            <Field label="Portfolio weight">{orMissing(portfolioWeight, formatPercentPoints)}</Field>
          </dl>
        </section>
      )}

      {scope === 'active' && showCurrentSection && (
        <section className="asset-summary__group" aria-labelledby={currentTitleId}>
          <div className="asset-summary__section-header">
            <h3 id={currentTitleId} className="asset-summary__section-title">
              Current
            </h3>
            <Button appearance="primary" icon={<ArrowSyncRegular />} onClick={refresh} disabledFocusable={!canRefresh}>
              Refresh price
            </Button>
          </div>
          <p className={hidden.visuallyHidden} role="status">
            {isLoadingPrice ? 'Loading the current price.' : ''}
          </p>
          <dl className="asset-summary__grid">
            <Field label={inCurrency('Current price')}>
              {isLoadingPrice ? <MissingValue label="Loading" /> : orMissing(price, (current) => formatN2(current.price))}
              {!isLoadingPrice && price?.isManual && (
                <PriceBadge abbreviation="Manual" meaning={MANUAL_PRICE_MEANING} className="asset-summary__badge" />
              )}
            </Field>
            <Field label="As of">
              {isLoadingPrice ? <MissingValue label="Loading" /> : formatAsOf(price)}
              {!isLoadingPrice && asset.marketStatus === 'Stale' && (
                <PriceBadge abbreviation="Stale" meaning={STALE_PRICE_MEANING} className="asset-summary__badge" />
              )}
              {!isLoadingPrice && asset.marketStatus === 'Unavailable' && (
                <PriceBadge abbreviation="Unavailable" meaning={NO_PRICE_MEANING} className="asset-summary__badge" />
              )}
            </Field>

            {!isLoadingPrice && (
              <>
                <Field label={inCurrency('Total current value')}>{orMissing(marketValue, formatN2)}</Field>
                <Field label="Result %" valueClassName={signedClass(resultPercent)}>
                  {orMissing(resultPercent, formatPercentPoints)}
                </Field>
                <Field label={inCurrency('Total current + credits')}>{orMissing(totalCurrentPlusCredits, formatN2)}</Field>
                <Field label="Result % with credits" valueClassName={signedClass(resultWithCreditsPercent)}>
                  {orMissing(resultWithCreditsPercent, formatPercentPoints)}
                </Field>
                <Field label="XIRR (shares only)" valueClassName={signedClass(xirr)}>
                  {orMissing(xirr, formatPercentFraction)}
                </Field>
                <Field label="XIRR with credits" valueClassName={signedClass(xirrWithCredits)}>
                  {orMissing(xirrWithCredits, formatPercentFraction)}
                </Field>
              </>
            )}

            {priceError && (
              <Field label="Status" valueClassName="asset-summary__value--error" full>
                <span role="alert">{priceError} Select Refresh price to try again.</span>
              </Field>
            )}
          </dl>
        </section>
      )}
    </div>
  )
}
