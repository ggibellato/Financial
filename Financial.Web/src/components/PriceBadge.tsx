import { useVisuallyHiddenStyles } from '../styles/visuallyHidden'

interface PriceBadgeProps {
  abbreviation: string
  meaning: string
  className: string
}

export default function PriceBadge({ abbreviation, meaning, className }: PriceBadgeProps) {
  const hidden = useVisuallyHiddenStyles()
  return (
    <span className={className} title={meaning}>
      <span aria-hidden="true"> ({abbreviation})</span>
      <span className={hidden.visuallyHidden}> {meaning}</span>
    </span>
  )
}
