import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { findContractDrift, type DriftFinding } from '../contractDrift'

const openApiDocument = (schemas: Record<string, { properties?: Record<string, unknown> }>) => ({
  components: { schemas },
})

const stripBom = (text: string): string => (text.charCodeAt(0) === 0xfeff ? text.slice(1) : text)

describe('findContractDrift', () => {
  it('MatchingSchemaAndType_ReturnsNoFindings', () => {
    const document = openApiDocument({
      ExpenseDTO: { properties: { id: {}, value: {} } },
    })
    const typesSource = `export interface ExpenseDto {\n  id: string\n  value: number\n}\n`

    expect(findContractDrift(document, typesSource)).toEqual([])
  })

  it('SchemaFieldMissingFromType_IsReportedAsMissingFromType', () => {
    const document = openApiDocument({
      ExpenseDTO: { properties: { id: {}, value: {}, categoryId: {} } },
    })
    const typesSource = `export interface ExpenseDto {\n  id: string\n  value: number\n}\n`

    expect(findContractDrift(document, typesSource)).toEqual([
      { schemaName: 'ExpenseDTO', typeName: 'ExpenseDto', missingFromType: ['categoryId'], staleInType: [] },
    ] satisfies DriftFinding[])
  })

  it('TypeFieldNoLongerInSchema_IsReportedAsStale', () => {
    const document = openApiDocument({
      ExpenseDTO: { properties: { id: {} } },
    })
    const typesSource = `export interface ExpenseDto {\n  id: string\n  legacyValue: number\n}\n`

    expect(findContractDrift(document, typesSource)).toEqual([
      { schemaName: 'ExpenseDTO', typeName: 'ExpenseDto', missingFromType: [], staleInType: ['legacyValue'] },
    ] satisfies DriftFinding[])
  })

  it('WordOrderDiffersBetweenSchemaAndTypeName_StillMatches', () => {
    // Real-world case: backend schema is "ExpenseCreateDTO", types.ts calls it "CreateExpenseDto".
    const document = openApiDocument({
      ExpenseCreateDTO: { properties: { value: {} } },
    })
    const typesSource = `export interface CreateExpenseDto {\n  value: number\n}\n`

    expect(findContractDrift(document, typesSource)).toEqual([])
  })

  it('SchemaWithNoTypeScriptCounterpart_IsSkippedRatherThanFlagged', () => {
    const document = openApiDocument({
      HealthStatusDTO: { properties: { state: {} } },
    })
    const typesSource = `export interface SelectedNode {\n  nodeType: string\n}\n`

    expect(findContractDrift(document, typesSource)).toEqual([])
  })

  it('SchemaWithoutProperties_IsSkipped', () => {
    // Enum-shaped schemas (PositionType, CountryCode, ...) have no `properties` object at all.
    const document = openApiDocument({
      PositionType: {},
    })
    const typesSource = `export type PositionType = 'Long' | 'Flat' | 'Short'\n`

    expect(findContractDrift(document, typesSource)).toEqual([])
  })
})

describe('contract drift against the committed OpenAPI snapshot', () => {
  const currentDir = dirname(fileURLToPath(import.meta.url))
  const snapshotPath = resolve(
    currentDir,
    '../../../../Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json'
  )
  const typesPath = resolve(currentDir, '../types.ts')

  it('OpenApiSnapshot_MatchesHandWrittenTypes_NoDriftFindings', () => {
    const snapshotRaw = readFileSync(snapshotPath, 'utf-8')
    const openApiDocument = JSON.parse(stripBom(snapshotRaw))
    const typesSource = readFileSync(typesPath, 'utf-8')

    const findings = findContractDrift(openApiDocument, typesSource)

    expect(findings, describeFindings(findings)).toEqual([])
  })
})

const describeFindings = (findings: DriftFinding[]): string =>
  findings
    .map((finding) => {
      const parts: string[] = []
      if (finding.missingFromType.length > 0) {
        parts.push(`missing from ${finding.typeName}: ${finding.missingFromType.join(', ')}`)
      }
      if (finding.staleInType.length > 0) {
        parts.push(
          `stale in ${finding.typeName} (no longer in ${finding.schemaName}): ${finding.staleInType.join(', ')}`
        )
      }
      return `${finding.schemaName} <-> ${finding.typeName}: ${parts.join('; ')}`
    })
    .join('\n')
