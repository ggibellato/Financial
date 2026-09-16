import { Skeleton, SkeletonItem } from '@fluentui/react-components'

export default function KpiTileSkeleton() {
  return (
    <Skeleton aria-label="Loading">
      <SkeletonItem size={24} />
    </Skeleton>
  )
}
