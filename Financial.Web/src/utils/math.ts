export function average(values: number[], monthsElapsed?: number): number {
  const completedValues = monthsElapsed === undefined ? values : values.slice(0, monthsElapsed)
  const divisor = monthsElapsed ?? completedValues.filter((v) => v !== 0).length
  return divisor === 0 ? 0 : completedValues.reduce((sum, v) => sum + v, 0) / divisor
}
