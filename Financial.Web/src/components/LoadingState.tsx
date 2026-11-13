import { useEffect, useRef } from 'react'
import { beginReload } from '../utils/retryOutcomeFocus'

interface LoadingStateProps {
  message?: string
}

export default function LoadingState({ message = 'Loading…' }: LoadingStateProps) {
  const statusRef = useRef<HTMLParagraphElement>(null)

  useEffect(() => {
    const region = statusRef.current?.parentElement
    const endReload = beginReload()
    return () => {
      if (region) endReload(region)
    }
  }, [])

  return (
    <p ref={statusRef} role="status">
      {message}
    </p>
  )
}
