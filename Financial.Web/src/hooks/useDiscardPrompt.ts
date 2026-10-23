import { useCallback, useState } from 'react'

export interface DiscardPrompt {
  confirmDiscard: (action: () => void) => void
  isPromptOpen: boolean
  keepEditing: () => void
  discard: () => void
}

export function useDiscardPrompt(isDirty: boolean): DiscardPrompt {
  const [pendingAction, setPendingAction] = useState<(() => void) | null>(null)

  const confirmDiscard = useCallback(
    (action: () => void) => {
      if (isDirty) setPendingAction(() => action)
      else action()
    },
    [isDirty],
  )

  const keepEditing = useCallback(() => setPendingAction(null), [])

  const discard = useCallback(() => {
    setPendingAction(null)
    pendingAction?.()
  }, [pendingAction])

  return { confirmDiscard, isPromptOpen: pendingAction !== null, keepEditing, discard }
}
