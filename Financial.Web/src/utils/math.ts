export function average(values: number[], monthsElapsed?: number): number {
  const relevantValues = monthsElapsed === undefined ? values : values.slice(0, monthsElapsed)
  const divisor = monthsElapsed ?? relevantValues.length
  return divisor === 0 ? 0 : relevantValues.reduce((sum, v) => sum + v, 0) / divisor
}
