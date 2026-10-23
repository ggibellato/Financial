import { useState } from 'react'

export interface DiscardPrompt {
  confirmDiscard: (action: () => void) => void
  isPromptOpen: boolean
  keepEditing: () => void
  discard: () => void
}

export function useDiscardPrompt(isDirty: boolean): DiscardPrompt {
  const [pendingAction, setPendingAction] = useState<(() => void) | null>(null)

  const confirmDiscard = (action: () => void) => {
    if (isDirty) setPendingAction(() => action)
    else action()
  }

  const keepEditing = () => setPendingAction(null)

  const discard = () => {
    setPendingAction(null)
    pendingAction?.()
  }

  return { confirmDiscard, isPromptOpen: pendingAction !== null, keepEditing, discard }
}
