import { useMemo } from 'react'
import { Badge, Combobox, Field, Input, Option, OptionGroup, Text } from '@fluentui/react-components'
import type { AccountDetailDto, AccountDto } from '../api/types'
import { formatLongMonthYear, formatN2 } from '../utils/formatters'

interface AccountHeaderProps {
  account: AccountDetailDto
  accounts: AccountDto[]
  monthInputValue: string
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
}

export default function AccountHeader({ account, accounts, monthInputValue, onMonthChange, onAccountSelect }: AccountHeaderProps) {
  const pickerGroups = useMemo(() => groupActiveByTemplate(accounts), [accounts])

  return (
    <header className="account-screen__header">
      <div className="account-screen__identity">
        <div className="account-screen__title">
          <h1>{account.name}</h1>
          {!account.isActive && <Badge appearance="outline" color="informative">Inactive</Badge>}
        </div>
        <Text>
          {account.templateName} · {account.currency}
        </Text>
      </div>

      <div className="account-screen__balance">
        <Text size={200}>Balance at end of {formatLongMonthYear(account.year, account.month)}</Text>
        <Text size={500} weight="semibold">
          {formatN2(account.balance)} {account.currency}
        </Text>
      </div>

      <div className="account-screen__controls">
        <Field label="Account">
          <Combobox
            key={account.id}
            defaultValue={account.name}
            defaultSelectedOptions={[account.id]}
            onOptionSelect={(_event, data) => {
              if (data.optionValue && data.optionValue !== account.id) onAccountSelect(data.optionValue)
            }}
          >
            {pickerGroups.map(([templateName, members]) => (
              <OptionGroup key={templateName} label={templateName}>
                {members.map((member) => (
                  <Option key={member.id} value={member.id} text={member.name}>
                    {member.name}
                  </Option>
                ))}
              </OptionGroup>
            ))}
          </Combobox>
        </Field>
        <Field label="Month">
          <Input type="month" value={monthInputValue} onChange={(_event, data) => onMonthChange(data.value)} />
        </Field>
      </div>
    </header>
  )
}
