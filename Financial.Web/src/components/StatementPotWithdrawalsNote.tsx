import { makeStyles, MessageBar, MessageBarBody, tokens } from '@fluentui/react-components'
import type { StatementPotWithdrawalDto } from '../api/types'
import { formatCurrency } from '../utils/formatters'

const UNCHOSEN_BANK = 'the paying bank'

const useStyles = makeStyles({
  list: {
    margin: 0,
    paddingLeft: tokens.spacingHorizontalL,
  },
})

interface StatementPotWithdrawalsNoteProps {
  withdrawals: readonly StatementPotWithdrawalDto[]
  bankName?: string
  currency: string
  withCardPayment?: boolean
}

export default function StatementPotWithdrawalsNote({ withdrawals, bankName, currency, withCardPayment = true }: StatementPotWithdrawalsNoteProps) {
  const styles = useStyles()
  if (withdrawals.length === 0) return null
  return (
    <MessageBar intent="info" layout="multiline">
      <MessageBarBody>
        <ul className={styles.list}>
          {withdrawals.map((withdrawal) => (
            <li key={withdrawal.operationId}>
              {withCardPayment ? 'Also takes ' : 'Takes '}
              <strong>{formatCurrency(withdrawal.amount, currency)}</strong>
              {` from ${withdrawal.potsAccountName} (${withdrawal.potName}) on ${bankName ?? UNCHOSEN_BANK}.`}
            </li>
          ))}
        </ul>
      </MessageBarBody>
    </MessageBar>
  )
}
