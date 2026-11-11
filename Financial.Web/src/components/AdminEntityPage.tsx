import { useId, useRef, type ReactNode } from 'react'
import { Button, Text } from '@fluentui/react-components'
import { AddRegular } from '@fluentui/react-icons'
import EmptyState from './EmptyState'
import ActionErrorAlert from './ActionErrorAlert'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import { useRowFocusRecovery } from '../hooks/useRowFocusRecovery'
import './AdminEntityPage.css'

interface AdminEntityPageProps {
  title: string
  createLabel: string
  onCreate: () => void
  createDisabledReason?: string
  belowHeader?: ReactNode
  actionError?: string | null
  belowActionError?: ReactNode
  isLoading: boolean
  loadError: string | null
  onRetry: () => void
  isEmpty: boolean
  emptyMessage: ReactNode
  dialogs?: ReactNode
  children: ReactNode
}

export default function AdminEntityPage({
  title,
  createLabel,
  onCreate,
  createDisabledReason,
  belowHeader,
  actionError,
  belowActionError,
  isLoading,
  loadError,
  onRetry,
  isEmpty,
  emptyMessage,
  dialogs,
  children,
}: AdminEntityPageProps) {
  const sectionRef = useRef<HTMLElement | null>(null)
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  const reasonId = useId()
  const recordRowFocus = useRowFocusRecovery(sectionRef, headingRef, isLoading)

  return (
    <section ref={sectionRef} className="admin-entity-page" onFocus={recordRowFocus}>
      <header className="admin-entity-page__header">
        <h2 ref={headingRef} tabIndex={-1}>
          {title}
        </h2>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={onCreate}
          disabledFocusable={createDisabledReason !== undefined}
          aria-describedby={createDisabledReason === undefined ? undefined : reasonId}
        >
          {createLabel}
        </Button>
        {createDisabledReason !== undefined && (
          <Text id={reasonId}>{createDisabledReason}</Text>
        )}
      </header>

      {belowHeader}

      <ActionErrorAlert message={actionError} />

      {belowActionError}

      <div className="admin-entity-page__body">
        {isLoading ? (
          <LoadingState />
        ) : loadError ? (
          <ErrorState message={loadError} onRetry={onRetry} />
        ) : isEmpty ? (
          <EmptyState>{emptyMessage}</EmptyState>
        ) : (
          children
        )}
      </div>

      {dialogs}
    </section>
  )
}
