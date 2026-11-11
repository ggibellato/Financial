export const PAID_BY_YOU = 'You'
export const PAID_BY_OTHER = 'Other'

export function paidByLabel(paidBy: string | null | undefined, otherParticipant: string): string {
  return paidBy === PAID_BY_YOU ? PAID_BY_YOU : otherParticipant
}
