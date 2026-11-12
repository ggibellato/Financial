import type { ReactNode } from 'react'
import type { AccountGroupMemberDto, CurrencyTotalDto } from '../api/types'
import { useColumnFilters } from '../hooks/useColumnFilters'
import { currencyFooterItems } from '../utils/currencyFooterItems'
import { formatN2 } from '../utils/formatters'
import { orMissing } from '../utils/orMissing'
import { formatOwedOrCredit, nothingDueText } from '../utils/owedBalance'
import ColumnFilterMenu from './grid/ColumnFilterMenu'
import TotalsGrid from './TotalsGrid'
import TruncatedText from './TruncatedText'

interface CardsGridProps {
  cards: AccountGroupMemberDto[]
  currencyTotals: CurrencyTotalDto[]
  viewedMonth: string
}

const FILTER_ACCESSORS = {
  card: (card: AccountGroupMemberDto) => card.name,
}

const FOOTER_FIGURES = [{ label: 'Combined adjustment figure', pick: (total: CurrencyTotalDto) => total.cardOutstandingTotal }]

const CURRENCY_COLUMN = {
  key: 'currency',
  header: 'Currency',
  render: (card: AccountGroupMemberDto) => card.currency,
  sortAccessor: (card: AccountGroupMemberDto) => card.currency,
}

const outstandingOf =(card: AccountGroupMemberDto) => card.statement?.outstandingTotal ?? 0

function statusOf(card: AccountGroupMemberDto): string {
  if (card.statement?.isPaid) return 'Paid'
  return outstandingOf(card) > 0 ? 'Unpaid' : nothingDueText(0)
}

function formatAccumulated(card: AccountGroupMemberDto): ReactNode {
  return orMissing(card.totalOwed, formatOwedOrCredit)
}

export default function CardsGrid({ cards, currencyTotals, viewedMonth }: CardsGridProps) {
  const { filteredRows, availableValues, selectedValues, toggleValue, toggleAll, isColumnFiltered, filterSummary } =
    useColumnFilters(cards, FILTER_ACCESSORS)
  const isMultiCurrency = currencyTotals.length > 1

  return (
    <TotalsGrid
      label="Card statements"
      columns={[
        {
          key: 'card',
          header: 'Card',
          render: (card: AccountGroupMemberDto) => (
            <TruncatedText text={card.name} to={`/cashflow/accounts/${card.accountId}?month=${viewedMonth}`} />
          ),
          sortAccessor: (card: AccountGroupMemberDto) => card.name,
          filterSlot: (
            <ColumnFilterMenu
              columnKey="card"
              label="Card"
              availableValues={availableValues.card}
              selectedValues={selectedValues.card}
              onToggleValue={toggleValue}
              onToggleAll={toggleAll}
              isFiltered={isColumnFiltered('card')}
            />
          ),
        },
        ...(isMultiCurrency ? [CURRENCY_COLUMN] : []),
        {
          key: 'outstanding',
          header: 'Outstanding (period)',
          numeric: true,
          render: (card: AccountGroupMemberDto) => formatN2(outstandingOf(card)),
          sortAccessor: outstandingOf,
        },
        {
          key: 'accumulatedOutstanding',
          header: 'Accumulated outstanding',
          numeric: true,
          render: formatAccumulated,
          sortAccessor: (card: AccountGroupMemberDto) => card.totalOwed ?? undefined,
        },
        {
          key: 'status',
          header: 'Status',
          render: statusOf,
          sortAccessor: statusOf,
        },
      ]}
      rows={filteredRows}
      filter={{ ...filterSummary, itemsNoun: 'cards' }}
      rowKey={(card) => card.accountId}
      emptyMessage="No cards in the default group for this month."
      footerItems={currencyFooterItems(currencyTotals, FOOTER_FIGURES)}
    />
  )
}
