import { useEffect, useRef } from 'react'
import { Button, Field, Input, Select, Text } from '@fluentui/react-components'
import { AddRegular, DeleteRegular } from '@fluentui/react-icons'
import type { BankDto } from '../api/types'
import { fundingLineErrorKey, fundingLinesMismatch, MAX_FUNDING_LINES, type FundingLineValues } from '../utils/fundingLines'
import { selectableBanks } from '../utils/selectableBanks'
import './FundingLinesField.css'

export type FundingLineChange = Partial<Pick<FundingLineValues, 'bankId' | 'amount'>>

interface FundingLinesFieldProps {
  lines: readonly FundingLineValues[]
  banks: BankDto[]
  keptBankIds?: readonly string[]
  depositAmount: number | null
  currency: string
  errors: Partial<Record<string, string>>
  onChange: (key: string, change: FundingLineChange) => void
  onAdd: () => void
  onRemove: (key: string) => void
}

export default function FundingLinesField({
  lines,
  banks,
  keptBankIds = [],
  depositAmount,
  currency,
  errors,
  onChange,
  onAdd,
  onRemove,
}: FundingLinesFieldProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const addLineRef = useRef<HTMLButtonElement | null>(null)
  const previousLineCount = useRef(lines.length)
  const mismatch = fundingLinesMismatch(lines, depositAmount, currency)
  const isAtLimit = lines.length >= MAX_FUNDING_LINES

  useEffect(() => {
    if (lines.length > previousLineCount.current) {
      const selects = containerRef.current?.querySelectorAll('select')
      selects?.[selects.length - 1]?.focus()
    } else if (lines.length < previousLineCount.current) {
      addLineRef.current?.focus()
    }
    previousLineCount.current = lines.length
  }, [lines.length])

  return (
    <div ref={containerRef} className="funding-lines">
      {lines.map((line, index) => {
        const bankError = errors[fundingLineErrorKey(line, 'bankId')]
        const amountError = errors[fundingLineErrorKey(line, 'amount')]
        return (
          <div key={line.key} className="funding-lines__line" role="group" aria-label={`Funding line ${index + 1}`}>
            <Field label="Bank" required validationState={bankError ? 'error' : 'none'} validationMessage={bankError}>
              <Select value={line.bankId} onChange={(event) => onChange(line.key, { bankId: event.target.value })}>
                <option value="">Select a bank</option>
                {selectableBanks(banks, line.bankId, ...keptBankIds).map((bank) => (
                  <option key={bank.id} value={bank.id}>
                    {bank.isActive === false ? `${bank.name} (inactive)` : bank.name}
                  </option>
                ))}
              </Select>
            </Field>
            <Field
              className="funding-lines__amount"
              label={`Amount (${currency})`}
              required
              validationState={amountError ? 'error' : 'none'}
              validationMessage={amountError}
            >
              <Input
                type="number"
                step="0.01"
                min="0"
                value={line.amount}
                onChange={(event) => onChange(line.key, { amount: event.target.value })}
              />
            </Field>
            {lines.length > 1 && (
              <Button
                className="funding-lines__remove"
                appearance="subtle"
                size="small"
                icon={<DeleteRegular />}
                aria-label={`Remove funding line ${index + 1}`}
                onClick={() => onRemove(line.key)}
              />
            )}
          </div>
        )
      })}
      <div className="funding-lines__footer">
        <Button ref={addLineRef} appearance="primary" icon={<AddRegular />} disabled={isAtLimit} onClick={onAdd}>
          Add line
        </Button>
        {isAtLimit && <Text>A deposit has at most {MAX_FUNDING_LINES} funding lines.</Text>}
      </div>
      <p className="funding-lines__mismatch" role="status">
        {mismatch}
      </p>
    </div>
  )
}
