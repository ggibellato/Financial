import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from 'react'
import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Button,
  MessageBar,
  MessageBarActions,
  MessageBarBody,
} from '@fluentui/react-components'
import type { AccordionToggleData, AccordionToggleEvent } from '@fluentui/react-components'
import { DismissRegular } from '@fluentui/react-icons'
import ErrorState from '../ErrorState'
import LoadingState from '../LoadingState'
import { useHoldingNavigation } from '../../hooks/useHoldingNavigation'
import { corporateActionTypeLabel } from '../../utils/corporateActionTypeLabel'
import { formatN2, formatShortDate } from '../../utils/formatters'
import type { DataQualityReportDto } from '../../api/types'
import './DataQualityWarningsPanel.css'

export type WarningCategoryId =
  | 'salesExceedPurchases'
  | 'missingPrice'
  | 'missingCostBasis'
  | 'unresolvedTaxClassification'
  | 'corporateActionAwaitingTaxReview'

interface WarningRow {
  key: string
  brokerName: string
  portfolioName: string
  assetName: string
  secondary: string
  corporateActionId?: string
}

interface WarningCategory {
  id: WarningCategoryId
  label: string
  rows: (report: DataQualityReportDto) => WarningRow[]
}

function holdingLocationText(brokerName: string, portfolioName: string): string {
  return `${portfolioName} · ${brokerName}`
}

const SEVERE_CATEGORIES: WarningCategory[] = [
  {
    id: 'salesExceedPurchases',
    label: 'Impossible cash-flow sequence',
    rows: (report) =>
      report.salesExceedPurchases.map((finding, index) => ({
        key: `sales-${index}-${finding.assetName}`,
        brokerName: finding.brokerName,
        portfolioName: finding.portfolioName,
        assetName: finding.assetName,
        secondary: `${holdingLocationText(finding.brokerName, finding.portfolioName)} — sold ${formatN2(
          finding.shortfall,
        )} more than held on ${formatShortDate(finding.offendingSaleDate)} (held ${formatN2(finding.quantityHeld)})`,
      })),
  },
  {
    id: 'missingPrice',
    label: 'Missing price',
    rows: (report) =>
      report.unpricedOpenHoldings.map((finding, index) => ({
        key: `price-${index}-${finding.assetName}`,
        brokerName: finding.brokerName,
        portfolioName: finding.portfolioName,
        assetName: finding.assetName,
        secondary: holdingLocationText(finding.brokerName, finding.portfolioName),
      })),
  },
  {
    id: 'missingCostBasis',
    label: 'Missing cost basis',
    rows: (report) =>
      report.openHoldingsMissingCostBasis.map((finding, index) => ({
        key: `cost-${index}-${finding.assetName}`,
        brokerName: finding.brokerName,
        portfolioName: finding.portfolioName,
        assetName: finding.assetName,
        secondary: holdingLocationText(finding.brokerName, finding.portfolioName),
      })),
  },
]

const TAX_CATEGORY: WarningCategory = {
  id: 'unresolvedTaxClassification',
  label: 'Unresolved tax classification',
  rows: (report) =>
    report.unresolvedTaxClassifications.map((finding, index) => ({
      key: `tax-${index}-${finding.assetName}`,
      brokerName: finding.brokerName,
      portfolioName: finding.portfolioName,
      assetName: finding.assetName,
      secondary: `${holdingLocationText(finding.brokerName, finding.portfolioName)} — ${finding.eventCategory}, tax year ${
        finding.taxYear
      }`,
    })),
}

const CORPORATE_ACTION_CATEGORY: WarningCategory = {
  id: 'corporateActionAwaitingTaxReview',
  label: 'Corporate action awaiting tax review',
  rows: (report) =>
    report.corporateActionsAwaitingTaxReview.map((finding, index) => ({
      key: `corporate-action-${index}-${finding.corporateActionId}`,
      brokerName: finding.brokerName,
      portfolioName: finding.portfolioName,
      assetName: finding.assetName,
      secondary: `${holdingLocationText(finding.brokerName, finding.portfolioName)} — ${corporateActionTypeLabel(finding.type)}, tax year ${finding.taxYear}`,
      corporateActionId: finding.corporateActionId,
    })),
}

export interface DataQualityWarningsPanelHandle {
  expandCategory: (categoryId: WarningCategoryId) => void
}

interface DataQualityWarningsPanelProps {
  report: DataQualityReportDto | null
  isLoading: boolean
  error: string | null
  retry: () => void
}

interface CategoryGroupProps {
  categories: WarningCategory[]
  report: DataQualityReportDto
  openItems: WarningCategoryId[]
  onOpenChange: (groupIds: WarningCategoryId[], openIds: WarningCategoryId[]) => void
  onSelectRow: (row: WarningRow) => void
}

function CategoryGroup({ categories, report, openItems, onOpenChange, onSelectRow }: CategoryGroupProps) {
  const populated = categories
    .map((category) => ({ category, rows: category.rows(report) }))
    .filter((candidate) => candidate.rows.length > 0)

  if (populated.length === 0) return null

  const groupIds = populated.map(({ category }) => category.id)

  return (
    <Accordion
      multiple
      collapsible
      openItems={openItems}
      onToggle={(_event: AccordionToggleEvent, data: AccordionToggleData<WarningCategoryId>) =>
        onOpenChange(groupIds, data.openItems)
      }
    >
      {populated.map(({ category, rows }) => (
        <AccordionItem key={category.id} value={category.id}>
          <AccordionHeader as="h4" button={{ id: `warning-category-header-${category.id}` }}>
            {category.label} ({rows.length})
          </AccordionHeader>
          <AccordionPanel>
            <ul className="data-quality-warnings__rows">
              {rows.map((row) => (
                <li key={row.key}>
                  <button
                    type="button"
                    className="data-quality-warnings__row"
                    onClick={() => onSelectRow(row)}
                  >
                    <span className="data-quality-warnings__row-asset">{row.assetName}</span>
                    <span className="data-quality-warnings__row-secondary">{row.secondary}</span>
                  </button>
                </li>
              ))}
            </ul>
          </AccordionPanel>
        </AccordionItem>
      ))}
    </Accordion>
  )
}

const DataQualityWarningsPanel = forwardRef<DataQualityWarningsPanelHandle, DataQualityWarningsPanelProps>(
  function DataQualityWarningsPanel({ report, isLoading, error, retry }, ref) {
    const containerRef = useRef<HTMLDivElement>(null)
    const [openItems, setOpenItems] = useState<WarningCategoryId[]>([])
    const [navigationError, setNavigationError] = useState<string | null>(null)
    const focusCategoryRef = useRef<WarningCategoryId | null>(null)
    const [focusToken, setFocusToken] = useState(0)
    const { navigateToHolding } = useHoldingNavigation()

    useImperativeHandle(ref, () => ({
      expandCategory: (categoryId) => {
        setOpenItems((current) => (current.includes(categoryId) ? current : [...current, categoryId]))
        containerRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
        focusCategoryRef.current = categoryId
        setFocusToken((token) => token + 1)
      },
    }))

    // The accordion header a cross-panel link opens isn't in the DOM yet when expandCategory runs -
    // move focus once its own render lands, not scroll alone, so keyboard/screen-reader users get
    // the same "you're here now" signal a sighted mouse user gets from the scroll.
    useEffect(() => {
      if (focusToken === 0) return
      containerRef.current?.querySelector<HTMLElement>(`#warning-category-header-${focusCategoryRef.current}`)?.focus()
    }, [focusToken, openItems])

    // Two accordions render either side of the stale-valuation row to keep the PRD's severity
    // order, so each one's toggle must leave the other one's open items alone.
    const handleOpenChange = (groupIds: WarningCategoryId[], openIds: WarningCategoryId[]) => {
      setOpenItems((current) => [...current.filter((id) => !groupIds.includes(id)), ...openIds])
    }

    const handleSelectRow = (row: WarningRow) => {
      void navigateToHolding(row.brokerName, row.portfolioName, row.assetName, row.corporateActionId).then((navigated) => {
        setNavigationError(
          navigated
            ? null
            : `Unable to locate ${row.assetName} — it may have moved or been archived since this report was generated.`,
        )
      })
    }

    if (isLoading) {
      return <LoadingState />
    }

    if (error) {
      return <ErrorState message={error} onRetry={retry} />
    }

    if (!report) {
      return null
    }

    const staleValuationCount = report.staleValuationCount
    const findingCount =
      report.salesExceedPurchases.length +
      report.unpricedOpenHoldings.length +
      report.openHoldingsMissingCostBasis.length +
      report.unresolvedTaxClassifications.length +
      report.corporateActionsAwaitingTaxReview.length

    return (
      <div className="data-quality-warnings" ref={containerRef}>
        {navigationError && (
          <MessageBar intent="warning" role="alert">
            <MessageBarBody>{navigationError}</MessageBarBody>
            <MessageBarActions
              containerAction={
                <Button
                  appearance="transparent"
                  icon={<DismissRegular />}
                  aria-label="Dismiss"
                  onClick={() => setNavigationError(null)}
                />
              }
            />
          </MessageBar>
        )}

        {findingCount === 0 && staleValuationCount === 0 ? (
          <MessageBar intent="success">
            <MessageBarBody>No data-quality issues detected</MessageBarBody>
          </MessageBar>
        ) : (
          <>
            <CategoryGroup
              categories={SEVERE_CATEGORIES}
              report={report}
              openItems={openItems}
              onOpenChange={handleOpenChange}
              onSelectRow={handleSelectRow}
            />
            {staleValuationCount > 0 && (
              <p className="data-quality-warnings__stale">Stale valuation ({staleValuationCount})</p>
            )}
            <CategoryGroup
              categories={[TAX_CATEGORY, CORPORATE_ACTION_CATEGORY]}
              report={report}
              openItems={openItems}
              onOpenChange={handleOpenChange}
              onSelectRow={handleSelectRow}
            />
          </>
        )}
      </div>
    )
  },
)

export default DataQualityWarningsPanel
