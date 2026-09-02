import { Badge, Button, Text, makeStyles, tokens, type BadgeProps } from '@fluentui/react-components'
import { AlertFilled, CalendarRegular, ClockRegular, DismissRegular } from '@fluentui/react-icons'
import { usePaymentsDue } from '../hooks/usePaymentsDue'
import { formatShortDateUtc } from '../utils/formatters'
import type { PaymentDueDto } from '../api/types'

type UrgencyTier = 'today' | 'soon' | 'upcoming'

const TIER_BY_DAYS_REMAINING: Record<UrgencyTier, { color: BadgeProps['color']; icon: React.ReactElement; word: string }> = {
  today: { color: 'danger', icon: <AlertFilled />, word: 'urgent' },
  soon: { color: 'warning', icon: <ClockRegular />, word: 'soon' },
  upcoming: { color: 'informative', icon: <CalendarRegular />, word: 'upcoming' },
}

function urgencyTier(daysRemaining: number): UrgencyTier {
  if (daysRemaining === 0) return 'today'
  if (daysRemaining <= 2) return 'soon'
  return 'upcoming'
}

function daysRemainingText(daysRemaining: number): string {
  if (daysRemaining === 0) return 'Due today'
  if (daysRemaining === 1) return 'Due in 1 day'
  return `Due in ${daysRemaining} days`
}

function typeLabel(type: string): string {
  return type === 'CreditCard' ? 'Credit card' : type
}

const useStyles = makeStyles({
  banner: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  item: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
})

function PaymentRow({ payment }: { payment: PaymentDueDto }) {
  const styles = useStyles()
  const tier = urgencyTier(payment.daysRemaining)
  const { color, icon, word } = TIER_BY_DAYS_REMAINING[tier]
  const daysText = daysRemainingText(payment.daysRemaining)

  return (
    <div className={styles.item}>
      <Badge appearance="filled" color={color} icon={icon} aria-label={`${daysText} – ${word}`}>
        {daysText}
      </Badge>
      <Text>{typeLabel(payment.type)}</Text>
      <Text weight="semibold">{payment.name}</Text>
      <Text>{formatShortDateUtc(payment.dueDate)}</Text>
    </div>
  )
}

export default function PaymentDueBanner() {
  const styles = useStyles()
  const { payments, dismiss } = usePaymentsDue()

  if (!payments || payments.length === 0) return null

  return (
    <div className={styles.banner} role="alert">
      <div className={styles.header}>
        <Text weight="semibold" size={400}>
          Upcoming payments
        </Text>
        <Button
          appearance="subtle"
          size="small"
          icon={<DismissRegular />}
          onClick={dismiss}
          aria-label="Dismiss upcoming payments"
        />
      </div>
      <div className={styles.list}>
        {payments.map((payment, index) => (
          <PaymentRow key={`${payment.type}-${payment.name}-${index}`} payment={payment} />
        ))}
      </div>
    </div>
  )
}
