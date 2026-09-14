import type { CostBasisMethod } from '../api/types'

export const COST_BASIS_METHOD_LABELS: Record<CostBasisMethod, string> = {
  AverageCost: 'Average Cost',
  FIFO: 'FIFO',
  SpecificId: 'Specific ID',
}
