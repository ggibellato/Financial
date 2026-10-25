import { useMemo } from 'react'
import { Badge, Dropdown, Field, Input, Option, OptionGroup, Text } from '@fluentui/react-components'
import type { AccountDetailDto, AccountDto } from '../api/types'
import { accountTemplateLabel } from '../utils/accountTemplateLabel'
import { formatLongMonthYear } from '../utils/formatters'
import { balanceLabel, formatShownBalance } from '../utils/owedBalance'

interface AccountHeaderProps {
  account: AccountDetailDto
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
    const label = accountTemplateLabel(account.templateName)
    const members = groups.get(label)
    if (members) members.push(account)
    else groups.set(label, [account])
  }
  return [...groups.entries()]
}

export default function AccountHeader({
  account,
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
          </div>
          <Text>
            {accountTemplateLabel(account.templateName)} · {account.currency}
          </Text>
        </div>

        <div className="account-screen__balance">
          <Text size={200}>
            {balanceLabel(account.balanceIsOwed)} at end of {formatLongMonthYear(account.year, account.month)}
          </Text>
          <Text size={500} weight="semibold">
            {formatShownBalance(account.balance, account.balanceIsOwed)} {account.currency}
          </Text>
        </div>
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
