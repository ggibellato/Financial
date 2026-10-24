import ConfirmDialog from './ConfirmDialog'

interface DiscardChangesDialogProps {
  open: boolean
  onKeepEditing: () => void
  onDiscard: () => void
}

export default function DiscardChangesDialog({ open, onKeepEditing, onDiscard }: DiscardChangesDialogProps) {
  return (
    <ConfirmDialog
      open={open}
      title="Discard unsaved changes?"
      message="Your changes to this form will be lost."
      confirmLabel="Discard"
      cancelLabel="Keep editing"
      onConfirm={onDiscard}
      onCancel={onKeepEditing}
    />
  )
}
