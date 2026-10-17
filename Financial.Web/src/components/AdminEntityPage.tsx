import type { ReactNode } from 'react'
import { Button, MessageBar, MessageBarBody } from '@fluentui/react-components'
import { AddRegular } from '@fluentui/react-icons'
import ErrorState from './ErrorState'
import LoadingState from './LoadingState'
import './AdminEntityPage.css'

interface AdminEntityPageProps {
  title: string
  createLabel: string
  onCreate: () => void
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
  return (
    <section className="admin-entity-page">
      <header className="admin-entity-page__header">
        <h2>{title}</h2>
        <Button appearance="primary" icon={<AddRegular />} onClick={onCreate}>
          {createLabel}
        </Button>
      </header>

      {belowHeader}

      {actionError && (
        <MessageBar intent="error">
          <MessageBarBody>{actionError}</MessageBarBody>
        </MessageBar>
      )}

      {belowActionError}

      {isLoading ? (
        <LoadingState />
      ) : loadError ? (
        <ErrorState message={loadError} onRetry={onRetry} />
      ) : isEmpty ? (
        <p className="admin-entity-page__empty">{emptyMessage}</p>
      ) : (
        children
      )}

      {dialogs}
    </section>
  )
}
