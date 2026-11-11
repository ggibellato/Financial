import type { CurrencyTotalDto } from '../api/types'
import type { BankTotal } from '../hooks/useMonthly'
import { formatN2 } from '../utils/formatters'
import { currencyFooterItems } from '../utils/currencyFooterItems'
import TotalsGrid from './TotalsGrid'
import TruncatedText from './TruncatedText'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import { useColumnFilters } from '../hooks/useColumnFilters'

interface BanksGridProps {
  bankTotals: BankTotal[]
  currencyTotals: CurrencyTotalDto[]
  viewedMonth: string
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
  { label: 'Bank balance', pick: (total: CurrencyTotalDto) => total.balance },
  { label: 'Round-up', pick: (total: CurrencyTotalDto) => total.roundUpTotal },
]

export default function BanksGrid({ bankTotals, currencyTotals, viewedMonth }: BanksGridProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered, filterSummary } =
    useColumnFilters(bankTotals, FILTER_ACCESSORS)
  const isMultiCurrency = currencyTotals.length > 1

  return (
    <TotalsGrid
      label="Bank balances"
      columns={[
        {
          key: 'bank',
          header: 'Bank',
          render: (b: BankTotal) => <TruncatedText text={b.bank} to={`/cashflow/accounts/${b.bankId}?month=${viewedMonth}`} />,
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
          header: 'Bank balance',
          numeric: true,
          render: (b: BankTotal) => formatN2(b.balance),
          sortAccessor: (b: BankTotal) => b.balance,
        },
        {
          key: 'roundUp',
          header: 'Round-up',
          numeric: true,
          render: (b: BankTotal) => formatN2(b.roundUpTotal),
          sortAccessor: (b: BankTotal) => b.roundUpTotal,
        },
      ]}
      rows={filteredRows}
      filter={{ ...filterSummary, itemsNoun: 'banks' }}
      rowKey={(b) => b.bankId}
      emptyMessage="No accounts in the default group for this month."
      footerItems={currencyFooterItems(currencyTotals, FOOTER_FIGURES)}
    />
  )
}
