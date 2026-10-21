import type { CurrencyTotalDto } from '../api/types'
import type { BankTotal } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'
import TotalsGrid from './TotalsGrid'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useColumnFilters } from '../hooks/useColumnFilters'

interface BanksGridProps {
  bankTotals: BankTotal[]
  currencyTotals: CurrencyTotalDto[]
}

const FILTER_ACCESSORS = {
  bank: (b: BankTotal) => b.bank,
}

function footerItemsFor(currencyTotals: CurrencyTotalDto[]) {
  if (currencyTotals.length <= 1) {
    const [total] = currencyTotals
    return [
      { label: 'Bank Balance', value: formatN2(total?.balance ?? 0) },
      { label: 'Round-Up', value: formatN2(total?.roundUpTotal ?? 0) },
    ]
  }
  return currencyTotals.flatMap((total) => [
    { label: `Bank Balance ${total.currency}`, value: formatN2(total.balance) },
    { label: `Round-Up ${total.currency}`, value: formatN2(total.roundUpTotal) },
  ])
}

export default function BanksGrid({ bankTotals, currencyTotals }: BanksGridProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(bankTotals, FILTER_ACCESSORS)
  const currencyColumn = {
    key: 'currency',
    header: 'Currency',
    render: (b: BankTotal) => b.currency,
    sortAccessor: (b: BankTotal) => b.currency,
  }

  return (
    <TotalsGrid
      label="Bank balances"
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
        ...(currencyTotals.length > 1 ? [currencyColumn] : []),
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
      rowKey={(b) => b.bankId}
      emptyMessage="No accounts in the default group for this month"
      footerItems={footerItemsFor(currencyTotals)}
    />
  )
}
