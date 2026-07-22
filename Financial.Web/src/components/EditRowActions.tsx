interface EditRowActionsProps {
  isSaving: boolean
  onSave: () => void
  onCancel: () => void
}

export default function EditRowActions({ isSaving, onSave, onCancel }: EditRowActionsProps) {
  return (
    <>
      <button type="button" disabled={isSaving} onClick={onSave}>
        {isSaving ? 'Saving...' : 'Save'}
      </button>
      <button type="button" onClick={onCancel}>
        Cancel
      </button>
    </>
  )
}
