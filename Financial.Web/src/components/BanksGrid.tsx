import type { CurrencyTotalDto } from '../api/types'
import type { BankTotal } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'
import { currencyFooterItems } from '../utils/currencyFooterItems'
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

const CURRENCY_COLUMN = {
  key: 'currency',
  header: 'Currency',
  render: (b: BankTotal) => b.currency,
  sortAccessor: (b: BankTotal) => b.currency,
}

const FOOTER_FIGURES = [
  { label: 'Bank Balance', pick: (total: CurrencyTotalDto) => total.balance },
  { label: 'Round-Up', pick: (total: CurrencyTotalDto) => total.roundUpTotal },
]

export default function BanksGrid({ bankTotals, currencyTotals }: BanksGridProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered } =
    useColumnFilters(bankTotals, FILTER_ACCESSORS)
  const isMultiCurrency = currencyTotals.length > 1

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
        ...(isMultiCurrency ? [CURRENCY_COLUMN] : []),
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
      footerItems={currencyFooterItems(currencyTotals, FOOTER_FIGURES)}
    />
  )
}
