> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# API Contract Snapshot and Generated Types (`Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json`, `Financial.Web/src/api/generated/openapi.ts`, `Financial.Web/src/api/types.ts`)

Domain-facing controllers return Application DTOs directly, so any DTO reshape is a wire-format
change. Three tests plus one build step keep the two sides honest.

## What to test

- **Snapshot equality**: the OpenAPI document the host generates equals the committed
  snapshot, line-diffed (`OpenApiContractTests.OpenApiDocument_MatchesTheCommittedSnapshot`).
- **Numeric schema hygiene**: no decimal/int property advertises a `string` alternative
  (`OpenApiDocument_NumericProperties_DoNotAdvertiseAStringFallback`) — pins the schema
  transformer in `Program.cs`.
- **Generated-type freshness**: regenerating `openapi.ts` from the snapshot with
  `openapi-typescript` yields byte-equal content ignoring line endings
  (`src/api/generated/__tests__/openapiFreshness.test.ts`).
- **Downstream compiles**: `tsc -b` (part of `npm run build`) fails at every call site that
  reads a renamed/removed field — the "regenerate + typecheck" step the fundamentals require.
- **Runtime shape agreement**: the smoke journey seeds data through the API and checks a
  computed value renders (`Financial.Web/scripts/smoke-test.mjs`) — catches a drift that
  compiles but renders blank.
- Negative: an intentional snapshot change without regeneration must fail the freshness test;
  an unintentional DTO rename must fail the snapshot test naming the endpoint.

## Layer assignment

- **Integration**: the snapshot test boots the real host; the freshness test shells out to the
  real generator over the real committed files. Both are the "contract/generated-type drift" row.
- The `tsc -b` step is a build gate, not a test — it runs in the `web` CI job and locally via
  `npm run build` (`feedback_typescript_build_validation` in project memory: vitest alone does
  not catch type errors).
- **E2E**: the smoke job is the only place runtime drift is caught.

## Setup pattern

There is nothing to write per feature — the workflow is the test. When an API change is intended:

```powershell
$env:UPDATE_OPENAPI_SNAPSHOT=1; dotnet test Tests/Financial.Api.Tests; Remove-Item Env:\UPDATE_OPENAPI_SNAPSHOT
cd Financial.Web; npm run generate-api-types; npm run build
```

or in bash `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests`. Review the
snapshot diff: anything removed or renamed is a breaking change for the SPA. Leaving
`UPDATE_OPENAPI_SNAPSHOT` set makes every later run silently rewrite the snapshot instead of
checking it. Commit the snapshot and `openapi.ts` in the same PR as the DTO change.

Hand-written frontend types with no backend counterpart (`SelectedNode`, `NodeType`,
`InvestmentScope`) stay above the aliases in `src/api/types.ts`; everything else is
`export type XDto = Schema<'XDTO'>`.

## When to skip

- Never skip the regeneration when a DTO, route, verb or `[ProducesResponseType]` changes.
- Do not hand-edit `openapi.ts` or the snapshot to make a test pass.

## Examples from project

- `Tests/Financial.Api.Tests/OpenApiContractTests.cs` — Integration; snapshot + numeric hygiene.
- `Financial.Web/src/api/generated/__tests__/openapiFreshness.test.ts` — Integration; generator round-trip.
- `.github/scripts/detect-changes.sh` — routes `types.ts`/snapshot changes to `web` + `smoke` jobs.
