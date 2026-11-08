import { useEffect, useId, useRef } from 'react'
import { Button, Text } from '@fluentui/react-components'
import type { BankOption } from '../api/types'
import type { IncomeFunding } from '../hooks/useIncomeForm'
import { formatCurrency } from '../utils/formatters'
import FundingLinesField from './FundingLinesField'
import './FundingLinesField.css'

interface FundedFromSectionProps {
  funding: IncomeFunding
  banks: BankOption[]
}

export default function FundedFromSection({ funding, banks }: FundedFromSectionProps) {
  const headingId = useId()
  const containerRef = useRef<HTMLDivElement | null>(null)
  const wasHandSplit = useRef(funding.isHandSplit)

  useEffect(() => {
    if (funding.isHandSplit && !wasHandSplit.current) containerRef.current?.querySelector('select')?.focus()
    wasHandSplit.current = funding.isHandSplit
  }, [funding.isHandSplit])

  return (
    <div ref={containerRef} className="funding-lines" role="group" aria-labelledby={headingId}>
      <Text as="h3" block id={headingId} weight="semibold">
        Funded from
      </Text>
      <Text size={200}>The deposit is 90% of the net value.</Text>
      {funding.isHandSplit ? (
        <FundingLinesField
          lines={funding.lines}
          banks={banks}
          keptBankIds={funding.openedBankIds}
          mismatch={funding.mismatch}
          currency={funding.currency}
          errors={funding.errors}
          onChange={funding.changeLine}
          onAdd={funding.addLine}
          onRemove={funding.removeLine}
        />
      ) : (
        <div className="funding-lines__footer">
          <Text>
            {funding.bankName ?? 'Choose the bank above'}
            {funding.depositAmount !== null && (
              <>
                {': '}
                <Text weight="semibold">{formatCurrency(funding.depositAmount, funding.currency)}</Text>
              </>
            )}
          </Text>
          <Button appearance="primary" onClick={funding.splitAcrossBanks}>
            Split across banks
          </Button>
        </div>
      )}
    </div>
  )
}
