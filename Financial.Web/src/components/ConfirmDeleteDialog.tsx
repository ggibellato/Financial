import type { ReactNode } from 'react'
import ConfirmDialog from './ConfirmDialog'

interface ConfirmDeleteDialogProps {
  entityLabel: string
  message: ReactNode
  open?: boolean
  confirmDisabled?: boolean
  error?: string | null
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmDeleteDialog({ entityLabel, open = true, ...props }: ConfirmDeleteDialogProps) {
  return <ConfirmDialog {...props} open={open} title={`Delete ${entityLabel}`} confirmLabel="Delete" cancelLabel="Cancel" />
}
