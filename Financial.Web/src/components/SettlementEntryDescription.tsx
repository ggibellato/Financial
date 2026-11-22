import { Badge, makeStyles, tokens } from '@fluentui/react-components'
import { todayIsoDate } from '../utils/formatters'
import TruncatedText from './TruncatedText'

const useStyles = makeStyles({
  planned: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    minWidth: 0,
  },
  text: { minWidth: 0 },
  badge: { flexShrink: 0 },
})

interface SettlementEntryDescriptionProps {
  text: string | null | undefined
  date: string
}

export default function SettlementEntryDescription({ text, date }: SettlementEntryDescriptionProps) {
  const styles = useStyles()
  if (date <= todayIsoDate()) return <TruncatedText text={text} />
  return (
    <span className={styles.planned}>
      <TruncatedText text={text} className={styles.text} />
      <Badge appearance="outline" color="informative" className={styles.badge}>
        Planned
      </Badge>
    </span>
  )
}
