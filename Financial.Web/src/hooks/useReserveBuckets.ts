import { useCallback } from 'react'
import { apiClient } from '../api/financialApiClient'
import type { ReserveBucketCreateDto, ReserveBucketDto, ReserveBucketUpdateDto } from '../api/types'
import { useCrudResource } from './useCrudResource'

export interface ReserveBucketsData {
  reserveBuckets: ReserveBucketDto[]
  isLoading: boolean
  error: string | null
  retry: () => void
  createReserveBucket: (request: ReserveBucketCreateDto) => Promise<ReserveBucketDto>
  updateReserveBucket: (id: string, request: ReserveBucketUpdateDto) => Promise<ReserveBucketDto>
  savingId: string | null
  saveError: string | null
  deactivateReserveBucket: (bucket: ReserveBucketDto) => void
  activeSplitWarning: string | null
}

interface ReserveBucketsSnapshot {
  reserveBuckets: ReserveBucketDto[]
  activeSplitWarning: string | null
}

const NO_RESERVE_BUCKETS: ReserveBucketsSnapshot = { reserveBuckets: [], activeSplitWarning: null }

async function loadReserveBuckets(): Promise<ReserveBucketsSnapshot> {
  const [reserveBuckets, activeSplitWarning] = await Promise.all([
    apiClient.getReserveBuckets(),
    apiClient.getReserveSplitStatus().then((status) => status.warning ?? null).catch(() => null),
  ])
  return { reserveBuckets, activeSplitWarning }
}

export function useReserveBuckets(): ReserveBucketsData {
  const resource = useCrudResource({
    load: loadReserveBuckets,
    initialData: NO_RESERVE_BUCKETS,
    loadErrorMessage: 'Unable to load reserve buckets',
  })
  const { mutateAndRefresh, runPendingMutation } = resource

  const createReserveBucket = useCallback(
    (request: ReserveBucketCreateDto) => mutateAndRefresh(() => apiClient.createReserveBucket(request)),
    [mutateAndRefresh],
  )

  const updateReserveBucket = useCallback(
    (id: string, request: ReserveBucketUpdateDto) => mutateAndRefresh(() => apiClient.updateReserveBucket(id, request)),
    [mutateAndRefresh],
  )

  const deactivateReserveBucket = useCallback(
    (bucket: ReserveBucketDto) =>
      runPendingMutation(
        bucket.id,
        () =>
          apiClient.updateReserveBucket(bucket.id, {
            name: bucket.name,
            splitPercentage: bucket.splitPercentage,
            isActive: false,
          }),
        'Failed to deactivate reserve bucket',
      ),
    [runPendingMutation],
  )

  return {
    reserveBuckets: resource.data.reserveBuckets,
    isLoading: resource.isLoading,
    error: resource.error,
    retry: resource.retry,
    createReserveBucket,
    updateReserveBucket,
    savingId: resource.pendingId,
    saveError: resource.pendingError,
    deactivateReserveBucket,
    activeSplitWarning: resource.data.activeSplitWarning,
  }
}
