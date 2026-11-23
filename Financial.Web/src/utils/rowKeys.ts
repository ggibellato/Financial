let lastRowKey = 0

export function nextRowKey(prefix: string): string {
  lastRowKey += 1
  return `${prefix}-${lastRowKey}`
}

export function withChangedRow<Row extends { key: string }>(rows: readonly Row[], key: string, change: Partial<Row>): Row[] {
  return rows.map((row) => (row.key === key ? { ...row, ...change } : row))
}
