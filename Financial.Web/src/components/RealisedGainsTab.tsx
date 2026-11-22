import { Button, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { ChevronDownRegular, ChevronRightRegular } from '@fluentui/react-icons'
import EmptyState from './EmptyState'
import type { RealisedGainDto } from '../api/types'
import ErrorState from './ErrorState'
import FilterTabList from './FilterTabList'
import LoadingState from './LoadingState'
import DataTableCell from './grid/DataTableCell'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, DATE_DESC_SORT, type SortAccessor } from '../hooks/useSortableRows'
import { ALL_TAX_YEARS, useRealisedGains, type RealisedGainChain } from '../hooks/useRealisedGains'
import { COST_BASIS_METHOD_LABELS } from '../utils/costBasisMethod'
import { formatDateTime, formatN2, formatN8, formatShortDate, signClass } from '../utils/formatters'
import './RealisedGainsTab.css'

const SORT_ACCESSORS: Record<string, SortAccessor<RealisedGainChain>> = {
  date: (chain) => new Date(chain.active.date),
  quantity: (chain) => chain.active.quantitySold,
  method: (chain) => COST_BASIS_METHOD_LABELS[chain.active.method],
  proceeds: (chain) => chain.active.proceeds,
  costBasis: (chain) => chain.active.costBasis,
  gainLoss: (chain) => chain.active.gainLoss,
  taxYear: (chain) => chain.active.taxYear,
}

interface HistoryRowProps {
  record: RealisedGainDto
}

function HistoryRow({ record }: HistoryRowProps) {
  return (
    <TableRow>
      <DataTableCell label="Computed on">{formatDateTime(record.createdAt)}</DataTableCell>
      <DataTableCell label="Method">{COST_BASIS_METHOD_LABELS[record.method]}</DataTableCell>
      <DataTableCell label="Proceeds" className="data-table__col--numeric">{formatN2(record.proceeds)}</DataTableCell>
      <DataTableCell label="Cost basis" className="data-table__col--numeric">{formatN2(record.costBasis)}</DataTableCell>
      <DataTableCell
        label="Gain/loss"
        className={`data-table__col--numeric ${signClass(record.gainLoss, 'realised-gains-tab__value')}`}
      >
        {formatN2(record.gainLoss)}
      </DataTableCell>
    </TableRow>
  )
}

interface RealisedGainRowProps {
  chain: RealisedGainChain
  isExpanded: boolean
  onToggleExpand: (id: string) => void
}

function RealisedGainRow({ chain, isExpanded, onToggleExpand }: RealisedGainRowProps) {
  const { active, history } = chain
  const hasHistory = history.length > 0
  const historyPanelId = `realised-gain-history-${active.id}`
  const historyTitleId = `${historyPanelId}-title`
  const dateLabel = formatShortDate(active.date)

  return (
    <>
      <TableRow>
        <TableCell className="realised-gains-tab__col--expand">
          {hasHistory && (
            <Button
              appearance="subtle"
              size="small"
              icon={isExpanded ? <ChevronDownRegular /> : <ChevronRightRegular />}
              aria-label={`${isExpanded ? 'Hide' : 'Show'} audit trail for the ${dateLabel} sale`}
              aria-expanded={isExpanded}
              aria-controls={historyPanelId}
              onClick={() => onToggleExpand(active.id)}
            />
          )}
        </TableCell>
        <DataTableCell label="Date">{dateLabel}</DataTableCell>
        <DataTableCell label="Quantity" className="data-table__col--numeric">{formatN8(active.quantitySold)}</DataTableCell>
        <DataTableCell label="Method">{COST_BASIS_METHOD_LABELS[active.method]}</DataTableCell>
        <DataTableCell label="Proceeds" className="data-table__col--numeric">{formatN2(active.proceeds)}</DataTableCell>
        <DataTableCell label="Cost basis" className="data-table__col--numeric">{formatN2(active.costBasis)}</DataTableCell>
        <DataTableCell
          label="Gain/loss"
          className={`data-table__col--numeric ${signClass(active.gainLoss, 'realised-gains-tab__value')}`}
        >
          {formatN2(active.gainLoss)}
        </DataTableCell>
        <DataTableCell label="Tax year">{active.taxYear}</DataTableCell>
      </TableRow>
      {hasHistory && isExpanded && (
        <TableRow className="realised-gains-tab__history-row">
          <TableCell className="data-table__cell--message" colSpan={8}>
            <div id={historyPanelId}>
              <p id={historyTitleId} className="realised-gains-tab__history-title">
                Audit trail for the {dateLabel} sale
              </p>
              <Table className="realised-gains-tab__history-table data-table" aria-labelledby={historyTitleId}>
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>Computed on</TableHeaderCell>
                    <TableHeaderCell>Method</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Proceeds</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Cost basis</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Gain/loss</TableHeaderCell>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {history.map((record) => (
                    <HistoryRow key={record.id} record={record} />
                  ))}
                </TableBody>
              </Table>
            </div>
          </TableCell>
        </TableRow>
      )}
    </>
  )
}

export default function RealisedGainsTab() {
  const {
    chains,
    filteredChains,
    taxYearOptions,
    isLoading,
    error,
    retry,
    selectedTaxYear,
    setTaxYear,
    expandedIds,
    toggleExpanded,
  } = useRealisedGains()

  const { sortedRows, sortState, requestSort } = useSortableRows(filteredChains, SORT_ACCESSORS, DATE_DESC_SORT)

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (chains.length === 0) {
    return (
      <div className="realised-gains-tab">
        <EmptyState>No realised gains yet — nothing has been sold from this holding.</EmptyState>
      </div>
    )
  }

  const filterOptions = [
    { value: ALL_TAX_YEARS, label: ALL_TAX_YEARS },
    ...taxYearOptions.map((year) => ({ value: year, label: year })),
  ]

  return (
    <div className="realised-gains-tab">
      <div className="realised-gains-tab__controls">
        <FilterTabList label="Tax year:" options={filterOptions} selected={selectedTaxYear} onSelect={setTaxYear} />
      </div>

      {filteredChains.length === 0 ? (
        <EmptyState>No realised gains in {selectedTaxYear}.</EmptyState>
      ) : (
        <div className="realised-gains-tab__table-wrapper">
          <Table className="realised-gains-tab__table data-table" aria-label="Realised gains">
            <TableHeader>
              <TableRow>
                <TableHeaderCell className="realised-gains-tab__col--expand" />
                <SortableColumnHeader
                  label="Date"
                  columnKey="date"
                  sortDirection={sortState?.columnKey === 'date' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Quantity"
                  columnKey="quantity"
                  numeric
                  sortDirection={sortState?.columnKey === 'quantity' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Method"
                  columnKey="method"
                  sortDirection={sortState?.columnKey === 'method' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Proceeds"
                  columnKey="proceeds"
                  numeric
                  sortDirection={sortState?.columnKey === 'proceeds' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Cost basis"
                  columnKey="costBasis"
                  numeric
                  sortDirection={sortState?.columnKey === 'costBasis' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Gain/loss"
                  columnKey="gainLoss"
                  numeric
                  sortDirection={sortState?.columnKey === 'gainLoss' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Tax year"
                  columnKey="taxYear"
                  sortDirection={sortState?.columnKey === 'taxYear' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((chain) => (
                <RealisedGainRow
                  key={chain.active.id}
                  chain={chain}
                  isExpanded={expandedIds.has(chain.active.id)}
                  onToggleExpand={toggleExpanded}
                />
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
