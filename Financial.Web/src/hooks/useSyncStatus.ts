import { useEffect, useState } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { SyncStatusResponseDto } from '../api/types'

const POLL_INTERVAL_MS = 15000

export interface SyncStatusData {
  status: SyncStatusResponseDto | null
}

export function useSyncStatus(): SyncStatusData {
  const [status, setStatus] = useState<SyncStatusResponseDto | null>(null)

  useEffect(() => {
    let cancelled = false

    const poll = () => {
      void apiClient
        .getSyncStatus()
        .then((result) => {
          if (!cancelled) {
            setStatus(result)
          }
        })
        .catch(() => {
          // A failed poll is retried on the next tick; the previous status is retained.
        })
    }

    poll()
    const intervalId = setInterval(poll, POLL_INTERVAL_MS)

    return () => {
      cancelled = true
      clearInterval(intervalId)
    }
  }, [])

  return { status }
}
