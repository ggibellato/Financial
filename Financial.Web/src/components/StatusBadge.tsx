import { Badge, type BadgeProps } from '@fluentui/react-components'
import { AlertFilled, CheckmarkCircleRegular, ClockRegular } from '@fluentui/react-icons'
import type { ReactElement } from 'react'

const STATUS_PRESENTATION: Record<string, { color: BadgeProps['color']; icon: ReactElement; label: string }> = {
  Final: { color: 'success', icon: <CheckmarkCircleRegular />, label: 'Final' },
  Incomplete: { color: 'warning', icon: <ClockRegular />, label: 'Incomplete' },
  RequiresReview: { color: 'danger', icon: <AlertFilled />, label: 'Requires review' },
}

export default function StatusBadge({ status }: { status: string | null }) {
  const presentation = status ? STATUS_PRESENTATION[status] : undefined
  if (!presentation) return <Badge appearance="filled">{status ?? 'Unknown'}</Badge>
  return (
    <Badge appearance="filled" color={presentation.color} icon={presentation.icon}>
      {presentation.label}
    </Badge>
  )
}
