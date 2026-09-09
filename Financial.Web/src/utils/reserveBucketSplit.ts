interface SplitPercentageBucket {
  isActive: boolean
  splitPercentage: number
}

const SPLIT_PERCENTAGE_TOLERANCE = 0.01

export function computeActiveSplitPercentage(buckets: readonly SplitPercentageBucket[]): number {
  return buckets.reduce((sum, bucket) => (bucket.isActive ? sum + bucket.splitPercentage : sum), 0)
}

export function isActiveSplitBalanced(activeSum: number): boolean {
  return Math.abs(activeSum - 100) <= SPLIT_PERCENTAGE_TOLERANCE
}
