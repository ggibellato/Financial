import type { BankTotal } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useColumnFilters } from '../hooks/useColumnFilters'

interface BanksGridProps {
  bankTotals: BankTotal[]
  bankTotalsSum: number
  roundUpTotalsSum: number
}

const FILTER_ACCESSORS = {
  bank: (b: BankTotal) => b.bank,
}

export default function BanksGrid({ bankTotals, bankTotalsSum, roundUpTotalsSum }: BanksGridProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(bankTotals, FILTER_ACCESSORS)

  return (
    <TotalsGrid
      columns={[
        {
          key: 'bank',
          header: 'Bank',
          render: (b: BankTotal) => b.bank,
          sortAccessor: (b: BankTotal) => b.bank,
          filterSlot: (
            <ColumnFilterMenu
              columnKey="bank"
              label="Bank"
              availableValues={availableValues.bank}
              selectedValues={selectedValues.bank}
              onToggleValue={toggleValue}
              onToggleAll={toggleAll}
              isFiltered={isColumnFiltered('bank')}
            />
          ),
        },
        {
          key: 'balance',
          header: 'Bank Balance',
          numeric: true,
          render: (b: BankTotal) => formatN2(b.balance),
          sortAccessor: (b: BankTotal) => b.balance,
        },
        {
          key: 'roundUp',
          header: 'Round-Up',
          numeric: true,
          render: (b: BankTotal) => formatN2(b.roundUpTotal),
          sortAccessor: (b: BankTotal) => b.roundUpTotal,
        },
      ]}
      rows={filteredRows}
      isFiltered={isColumnFiltered('bank')}
      rowKey={(b) => b.bank}
      footerItems={[
        { label: 'Bank Balance', value: formatN2(bankTotalsSum) },
        { label: 'Round-Up', value: formatN2(roundUpTotalsSum) },
      ]}
    />
  )
}
