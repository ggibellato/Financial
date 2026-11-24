import { Text } from '@fluentui/react-components'
import type { ReactNode } from 'react'

interface SummarySectionProps {
  heading: string
  headingId?: string
  children: ReactNode
}

export default function SummarySection({ heading, headingId, children }: SummarySectionProps) {
  return (
    <div className="accounts-page__section monthly-page__section--grid">
      <Text as="h2" id={headingId} weight="semibold" size={400}>
        {heading}
      </Text>
      {children}
    </div>
  )
}
