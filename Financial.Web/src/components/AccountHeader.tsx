import { useMemo } from 'react'
import { Badge, Dropdown, Field, Input, Option, OptionGroup, Text } from '@fluentui/react-components'
import type { AccountDetailDto, AccountDto, AccountStatementDto } from '../api/types'
import { accountTemplateLabel } from '../utils/accountTemplateLabel'
import { formatLongMonthYear, formatN2 } from '../utils/formatters'
import { balanceLabel, formatOwedOrCredit, formatShownBalance, nothingDueText, statementStatusText, UNPAID_STATEMENT_STATUS } from '../utils/owedBalance'
import { settlementBalanceText, settlementCounterparty } from '../utils/settlementBalance'
import SavingsTermsSummary from './SavingsTermsSummary'
import { destinationsSummary } from '../utils/moneyDestination'

interface AccountHeaderProps {
  account: AccountDetailDto
  hasParticipants: boolean
  accounts: AccountDto[]
  selectedAccountId: string
  monthInputValue: string
  isBusy: boolean
  onMonthChange: (value: string) => void
  onAccountSelect: (accountId: string) => void
}

function groupActiveByTemplate(accounts: AccountDto[]): [string, AccountDto[]][] {
  const groups = new Map<string, AccountDto[]>()
  for (const account of accounts.filter((candidate) => candidate.isActive)) {
    const members = groups.get(account.templateName)
    if (members) members.push(account)
    else groups.set(account.templateName, [account])
  }
  return [...groups.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([templateName, members]) => [accountTemplateLabel(templateName), members])
}

function viewedStatementAmount(statement: AccountStatementDto | null): number {
  if (statement?.status === UNPAID_STATEMENT_STATUS) return statement.outstandingTotal
  return statement?.paidAmount ?? 0
}

function potLinkedText(amount: number | null | undefined, currency: string): string | null {
  return amount == null ? null : `${formatN2(amount)} ${currency} from Pots`
}

function firstFigure(account: AccountDetailDto, hasParticipants: boolean): { label: string; value: string; status: string | null } {
  if (hasParticipants) {
    return {
      label: account.hasPlannedEntries ? 'Balance (includes planned)' : 'Current balance',
      value: settlementBalanceText(settlementCounterparty(account), account.currentBalances),
      status: null,
    }
  }
  const viewedMonth = formatLongMonthYear(account.year, account.month)
  if (!account.hasStatements) {
    return {
      label: `${balanceLabel(account.balanceIsOwed)} at end of ${viewedMonth}`,
      value: `${formatShownBalance(account.balance, account.balanceIsOwed)} ${account.currency}`,
      status: null,
    }
  }
  const statement = account.viewedStatement
  return {
    label: `${viewedMonth} statement`,
    value: `${formatN2(viewedStatementAmount(statement))} ${account.currency}`,
    status: statement ? statementStatusText(statement) : nothingDueText(0),
  }
}

export default function AccountHeader({
  account,
  hasParticipants,
  accounts,
  selectedAccountId,
  monthInputValue,
  isBusy,
  onMonthChange,
  onAccountSelect,
}: AccountHeaderProps) {
  const pickerGroups = useMemo(() => groupActiveByTemplate(accounts), [accounts])
  const selectedName =
    accounts.find((candidate) => candidate.id.toLowerCase() === selectedAccountId.toLowerCase())?.name ?? account.name
  const figure = firstFigure(account, hasParticipants)
  const statementFromPots = potLinkedText(account.viewedStatement?.potLinkedTotal, account.currency)
  const owedFromPots = potLinkedText(account.potLinkedOwed, account.currency)
  const destinations = destinationsSummary(account.roundUpDestination, account.cashbackDestination)
  const currencies = hasParticipants ? account.currentBalances.map(({ currency }) => currency).join(', ') : account.currency

  return (
    <header className="account-screen__header" aria-busy={isBusy}>
      <div className={`account-screen__summary${isBusy ? ' account-screen__summary--busy' : ''}`}>
        <div className="account-screen__identity">
          <div className="account-screen__title">
            <Text as="h1" size={600} weight="semibold" className="account-screen__name">
              {account.name}
            </Text>
            {!account.isActive && (
              <Badge appearance="outline" color="informative">
                Inactive
              </Badge>
            )}
            {account.savings?.isTaxFree && (
              <Badge appearance="outline" color="informative">
                Tax-free
              </Badge>
            )}
            {account.savings?.roundUpOnly && (
              <Badge appearance="outline" color="informative">
                Round-up only
              </Badge>
            )}
          </div>
          <Text>
            {accountTemplateLabel(account.templateName)} · {currencies}
          </Text>
          {account.savings && <SavingsTermsSummary savings={account.savings} />}
          {destinations && <Text>{destinations}</Text>}
        </div>

        <div className={`account-screen__balance${account.hasStatements ? ' account-screen__balance--top-aligned' : ''}`}>
          <Text size={200}>{figure.label}</Text>
          <Text size={500} weight="semibold">
            {figure.value}
          </Text>
          {figure.status && <Text size={300}>{figure.status}</Text>}
          {statementFromPots && <Text size={300}>{statementFromPots}</Text>}
        </div>

        {account.totalOwed !== null && (
          <div className="account-screen__balance account-screen__balance--top-aligned">
            <Text size={200}>Total owed</Text>
            <Text size={500} weight="semibold">
              {formatOwedOrCredit(account.totalOwed)} {account.currency}
            </Text>
            {owedFromPots && <Text size={300}>{owedFromPots}</Text>}
          </div>
        )}
      </div>

      <div className="account-screen__controls">
        <Field label="Account">
          <Dropdown
            value={selectedName}
            selectedOptions={[selectedAccountId]}
            onOptionSelect={(_event, data) => {
              if (data.optionValue && data.optionValue !== selectedAccountId) onAccountSelect(data.optionValue)
            }}
          >
            {pickerGroups.map(([templateLabel, members]) => (
              <OptionGroup key={templateLabel} label={templateLabel}>
                {members.map((member) => (
                  <Option key={member.id} value={member.id} text={member.name}>
                    {member.name}
                  </Option>
                ))}
              </OptionGroup>
            ))}
          </Dropdown>
        </Field>
        <Field label="Month">
          <Input type="month" value={monthInputValue} onChange={(_event, data) => onMonthChange(data.value)} />
        </Field>
      </div>
    </header>
  )
}
