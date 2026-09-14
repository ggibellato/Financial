import { Button, Table, TableBody, TableCell, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import { ChevronDownRegular, ChevronRightRegular } from '@fluentui/react-icons'
import type { DisposalRecordDto } from '../api/types'
import ErrorState from './ErrorState'
import FilterTabList from './FilterTabList'
import LoadingState from './LoadingState'
import SortableColumnHeader from './grid/SortableColumnHeader'
import { useSortableRows, type SortAccessor } from '../hooks/useSortableRows'
import { ALL_TAX_YEARS, useDisposals, type DisposalChain } from '../hooks/useDisposals'
import { COST_BASIS_METHOD_LABELS } from '../utils/costBasisMethod'
import { formatDateTime, formatN2, formatN8, formatShortDate, signClass } from '../utils/formatters'
import './DisposalsTab.css'

const SORT_ACCESSORS: Record<string, SortAccessor<DisposalChain>> = {
  date: (chain) => new Date(chain.active.date),
  quantity: (chain) => chain.active.quantityDisposed,
  method: (chain) => COST_BASIS_METHOD_LABELS[chain.active.method],
  proceeds: (chain) => chain.active.proceeds,
  costBasis: (chain) => chain.active.costBasis,
  gainLoss: (chain) => chain.active.gainLoss,
  taxYear: (chain) => chain.active.taxYear,
}

interface HistoryRowProps {
  record: DisposalRecordDto
}

function HistoryRow({ record }: HistoryRowProps) {
  return (
    <TableRow>
      <TableCell>{formatDateTime(record.createdAt)}</TableCell>
      <TableCell>{COST_BASIS_METHOD_LABELS[record.method]}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(record.proceeds)}</TableCell>
      <TableCell className="data-table__col--numeric">{formatN2(record.costBasis)}</TableCell>
      <TableCell className={`data-table__col--numeric ${signClass(record.gainLoss, 'disposals-tab__value')}`}>
        {formatN2(record.gainLoss)}
      </TableCell>
    </TableRow>
  )
}

interface DisposalRowProps {
  chain: DisposalChain
  isExpanded: boolean
  onToggleExpand: (id: string) => void
}

function DisposalRow({ chain, isExpanded, onToggleExpand }: DisposalRowProps) {
  const { active, history } = chain
  const hasHistory = history.length > 0
  const historyPanelId = `disposal-history-${active.id}`
  const dateLabel = formatShortDate(active.date)

  return (
    <>
      <TableRow>
        <TableCell className="disposals-tab__col--expand">
          {hasHistory && (
            <Button
              appearance="subtle"
              size="small"
              icon={isExpanded ? <ChevronDownRegular /> : <ChevronRightRegular />}
              aria-label={`${isExpanded ? 'Hide' : 'Show'} audit trail for the ${dateLabel} disposal`}
              aria-expanded={isExpanded}
              aria-controls={historyPanelId}
              onClick={() => onToggleExpand(active.id)}
            />
          )}
        </TableCell>
        <TableCell>{dateLabel}</TableCell>
        <TableCell className="data-table__col--numeric">{formatN8(active.quantityDisposed)}</TableCell>
        <TableCell>{COST_BASIS_METHOD_LABELS[active.method]}</TableCell>
        <TableCell className="data-table__col--numeric">{formatN2(active.proceeds)}</TableCell>
        <TableCell className="data-table__col--numeric">{formatN2(active.costBasis)}</TableCell>
        <TableCell className={`data-table__col--numeric ${signClass(active.gainLoss, 'disposals-tab__value')}`}>
          {formatN2(active.gainLoss)}
        </TableCell>
        <TableCell>{active.taxYear}</TableCell>
      </TableRow>
      {hasHistory && isExpanded && (
        <TableRow className="disposals-tab__history-row">
          <TableCell colSpan={8}>
            <div id={historyPanelId}>
              <p className="disposals-tab__history-title">Audit trail for the {dateLabel} disposal</p>
              <Table className="disposals-tab__history-table data-table">
                <TableHeader>
                  <TableRow>
                    <TableHeaderCell>Computed On</TableHeaderCell>
                    <TableHeaderCell>Method</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Proceeds</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Cost Basis</TableHeaderCell>
                    <TableHeaderCell className="data-table__col--numeric">Gain/Loss</TableHeaderCell>
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

export default function DisposalsTab() {
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
  } = useDisposals()

  const { sortedRows, sortState, requestSort } = useSortableRows(filteredChains, SORT_ACCESSORS)

  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={error} onRetry={retry} />
  }

  if (chains.length === 0) {
    return (
      <div className="disposals-tab">
        <p className="disposals-tab__empty">No disposals yet — nothing has been sold from this holding.</p>
      </div>
    )
  }

  const filterOptions = [
    { value: ALL_TAX_YEARS, label: ALL_TAX_YEARS },
    ...taxYearOptions.map((year) => ({ value: year, label: year })),
  ]

  return (
    <div className="disposals-tab">
      <div className="disposals-tab__controls">
        <FilterTabList label="Tax year:" options={filterOptions} selected={selectedTaxYear} onSelect={setTaxYear} />
      </div>

      {filteredChains.length === 0 ? (
        <p className="disposals-tab__empty">No disposals in {selectedTaxYear}.</p>
      ) : (
        <div className="disposals-tab__table-wrapper">
          <Table className="disposals-tab__table data-table">
            <TableHeader>
              <TableRow>
                <TableHeaderCell className="disposals-tab__col--expand" />
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
                  label="Cost Basis"
                  columnKey="costBasis"
                  numeric
                  sortDirection={sortState?.columnKey === 'costBasis' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Gain/Loss"
                  columnKey="gainLoss"
                  numeric
                  sortDirection={sortState?.columnKey === 'gainLoss' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
                <SortableColumnHeader
                  label="Tax Year"
                  columnKey="taxYear"
                  sortDirection={sortState?.columnKey === 'taxYear' ? sortState.direction : undefined}
                  onSort={requestSort}
                />
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedRows.map((chain) => (
                <DisposalRow
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
