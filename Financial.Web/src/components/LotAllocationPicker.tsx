import { Input, Table, TableBody, TableHeader, TableHeaderCell, TableRow } from '@fluentui/react-components'
import type { OpenLotDto } from '../api/types'
import DataTableCell from './grid/DataTableCell'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { formatN2, formatN8, formatShortDate } from '../utils/formatters'
import { isAllocationExact, isLotOverAllocated, sumAllocations } from '../utils/lotAllocation'
import './LotAllocationPicker.css'

interface LotAllocationPickerProps {
  openLots: OpenLotDto[]
  isLoading: boolean
  error: string | null
  onRetry: () => void
  saleQuantity: number
  allocations: Record<string, string>
  onChange: (sourceTransactionId: string, value: string) => void
  errorMessage: string | null
}

export default function LotAllocationPicker({
  openLots,
  isLoading,
  error,
  onRetry,
  saleQuantity,
  allocations,
  onChange,
  errorMessage,
}: LotAllocationPickerProps) {
  if (isLoading) {
    return <LoadingState message="Loading open lots..." />
  }

  if (error) {
    return <ErrorState message={error} onRetry={onRetry} />
  }

  if (openLots.length === 0) {
    return <p className="lot-allocation-picker__empty">No open lots available for this holding.</p>
  }

  const allocated = sumAllocations(allocations)
  const matches = isAllocationExact(allocated, saleQuantity)

  return (
    <div className="lot-allocation-picker">
      <Table className="lot-allocation-picker__table data-table" aria-label="Open lots">
        <TableHeader>
          <TableRow>
            <TableHeaderCell>Purchase Date</TableHeaderCell>
            <TableHeaderCell className="data-table__col--numeric">Remaining</TableHeaderCell>
            <TableHeaderCell className="data-table__col--numeric">Unit Cost</TableHeaderCell>
            <TableHeaderCell className="data-table__col--numeric">Allocate</TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {openLots.map((lot) => {
            const value = allocations[lot.sourceTransactionId] ?? ''
            const overAllocated = isLotOverAllocated(lot, value)
            const dateLabel = formatShortDate(lot.date)
            const rowErrorId = `lot-allocation-error-${lot.sourceTransactionId}`
            return (
              <TableRow key={lot.sourceTransactionId}>
                <DataTableCell label="Purchase Date">{dateLabel}</DataTableCell>
                <DataTableCell label="Remaining" className="data-table__col--numeric">
                  {formatN8(lot.remainingQuantity)}
                </DataTableCell>
                <DataTableCell label="Unit Cost" className="data-table__col--numeric">
                  {formatN2(lot.unitCost)}
                </DataTableCell>
                <DataTableCell label="Allocate" className="data-table__col--numeric">
                  <Input
                    type="number"
                    step="0.0001"
                    min="0"
                    max={lot.remainingQuantity}
                    value={value}
                    onChange={(e) => onChange(lot.sourceTransactionId, e.target.value)}
                    className={overAllocated ? 'lot-allocation-picker__input--error' : undefined}
                    aria-label={`Allocate quantity from the lot purchased ${dateLabel}`}
                    aria-invalid={overAllocated || undefined}
                    aria-describedby={overAllocated ? rowErrorId : undefined}
                  />
                  {overAllocated && (
                    <p id={rowErrorId} className="lot-allocation-picker__row-error">
                      Exceeds the {formatN8(lot.remainingQuantity)} remaining on this lot.
                    </p>
                  )}
                </DataTableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>

      {saleQuantity <= 0 ? (
        <p className="lot-allocation-picker__summary--mismatch">Enter a sale quantity, then allocate it across the lots above.</p>
      ) : (
        <p className={matches ? 'lot-allocation-picker__summary--match' : 'lot-allocation-picker__summary--mismatch'}>
          Allocated {formatN8(allocated)} of {formatN8(saleQuantity)}
          {!matches && ` (${formatN8(Math.abs(saleQuantity - allocated))} ${allocated < saleQuantity ? 'remaining' : 'over'})`}
        </p>
      )}

      {errorMessage && <p className="lot-allocation-picker__error" role="alert">{errorMessage}</p>}
    </div>
  )
}
