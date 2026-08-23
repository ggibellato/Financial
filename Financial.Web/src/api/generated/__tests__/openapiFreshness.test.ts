import { execFileSync } from 'node:child_process'
import { mkdtempSync, readFileSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

/**
 * `src/api/generated/openapi.ts` is committed (so a shape change is reviewable in a PR diff, matching
 * how `Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json` is handled) rather than generated at
 * build time. This is the test that notices when it falls out of sync with that snapshot - regenerate it
 * with `npm run generate-api-types` and commit the result.
 */
describe('generated OpenAPI types', () => {
  it('OpenApiTypes_MatchTheCommittedSnapshot', () => {
    const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../../..')
    const snapshotPath = resolve(webRoot, '../Tests/Financial.Api.Tests/Contract/openapi-v1.snapshot.json')
    const committedPath = resolve(webRoot, 'src/api/generated/openapi.ts')
    const cliPath = resolve(webRoot, 'node_modules/openapi-typescript/bin/cli.js')

    const tempDir = mkdtempSync(join(tmpdir(), 'openapi-types-'))
    const freshPath = join(tempDir, 'openapi.ts')

    try {
      execFileSync(
        process.execPath,
        [cliPath, snapshotPath, '-o', freshPath, '--empty-objects-unknown', '--properties-required-by-default'],
        { stdio: 'pipe' }
      )

      const fresh = readFileSync(freshPath, 'utf-8')
      const committed = readFileSync(committedPath, 'utf-8')

      expect(fresh, 'run `npm run generate-api-types` and commit the result').toBe(committed)
    } finally {
      rmSync(tempDir, { recursive: true, force: true })
    }
  })
})
