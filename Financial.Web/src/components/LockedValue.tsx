import { useId } from 'react'
import { makeStyles, Text, tokens } from '@fluentui/react-components'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
})

interface LockedValueProps {
  label: string
  value: string
}

export default function LockedValue({ label, value }: LockedValueProps) {
  const styles = useStyles()
  const labelId = useId()

  return (
    <div className={styles.root} role="group" aria-labelledby={labelId}>
      <Text id={labelId}>{label}</Text>
      <Text weight="semibold">{value}</Text>
    </div>
  )
}
