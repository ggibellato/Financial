import ts from 'typescript'

export interface DriftFinding {
  schemaName: string
  typeName: string
  /** Present on the OpenAPI schema but absent from the matching TypeScript type. */
  missingFromType: string[]
  /** Present on the TypeScript type but absent from the matching OpenAPI schema — usually a renamed or removed backend field the frontend still declares. */
  staleInType: string[]
}

interface TypeShape {
  name: string
  properties: Set<string>
}

interface OpenApiDocument {
  components?: {
    schemas?: Record<string, { properties?: Record<string, unknown> }>
  }
}

/**
 * Matches an OpenAPI schema name to a TypeScript type name across the "Xxx" + "DTO"/"Dto" convention
 * and the request-DTO word-order swap this codebase has (e.g. schema "ExpenseCreateDTO" vs
 * `CreateExpenseDto` in types.ts) — same words, different order, so a bag-of-words compare is used
 * instead of a straight suffix strip.
 */
const normalize = (name: string): string => {
  const withoutDto = name.replace(/dto/gi, '')
  const words = withoutDto.match(/[A-Z][a-z0-9]*|[a-z0-9]+/g) ?? [withoutDto]
  return words
    .map((word) => word.toLowerCase())
    .sort()
    .join('')
}

const extractSchemaShapes = (openApiDocument: OpenApiDocument): TypeShape[] => {
  const schemas = openApiDocument.components?.schemas ?? {}
  return Object.entries(schemas)
    .filter(([, schema]) => !!schema.properties)
    .map(([name, schema]) => ({
      name,
      properties: new Set(Object.keys(schema.properties!)),
    }))
}

const memberNames = (sourceFile: ts.SourceFile, members: ts.NodeArray<ts.TypeElement>): Set<string> =>
  new Set(
    members
      .filter((member): member is ts.PropertySignature => ts.isPropertySignature(member) && !!member.name)
      .map((member) => member.name.getText(sourceFile))
  )

const extractTypeScriptShapes = (typesSource: string): TypeShape[] => {
  const sourceFile = ts.createSourceFile('types.ts', typesSource, ts.ScriptTarget.Latest, true)
  const shapes: TypeShape[] = []

  sourceFile.forEachChild((node) => {
    if (ts.isInterfaceDeclaration(node)) {
      shapes.push({ name: node.name.text, properties: memberNames(sourceFile, node.members) })
    } else if (ts.isTypeAliasDeclaration(node) && ts.isTypeLiteralNode(node.type)) {
      shapes.push({ name: node.name.text, properties: memberNames(sourceFile, node.type.members) })
    }
  })

  return shapes
}

/**
 * Compares the backend's OpenAPI contract against the hand-written `types.ts` mirror, matching types
 * by name and reporting any field that only one side declares.
 *
 * Only matched pairs are compared — a schema or a TypeScript type with no counterpart on the other side
 * is skipped, since not every backend DTO is consumed by the frontend and not every frontend type
 * mirrors a wire DTO (e.g. `SelectedNode`). That is by design, not a gap this check is meant to close.
 */
export const findContractDrift = (openApiDocument: OpenApiDocument, typesSource: string): DriftFinding[] => {
  const typeByNormalizedName = new Map(
    extractTypeScriptShapes(typesSource).map((shape) => [normalize(shape.name), shape])
  )

  const findings: DriftFinding[] = []
  for (const schemaShape of extractSchemaShapes(openApiDocument)) {
    const typeShape = typeByNormalizedName.get(normalize(schemaShape.name))
    if (!typeShape) {
      continue
    }

    const missingFromType = [...schemaShape.properties].filter((property) => !typeShape.properties.has(property))
    const staleInType = [...typeShape.properties].filter((property) => !schemaShape.properties.has(property))

    if (missingFromType.length > 0 || staleInType.length > 0) {
      findings.push({ schemaName: schemaShape.name, typeName: typeShape.name, missingFromType, staleInType })
    }
  }

  return findings
}
