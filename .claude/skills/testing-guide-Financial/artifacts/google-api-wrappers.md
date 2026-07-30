> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Google API Wrappers (`GoogleDriveClient`, `GoogleFileClientFactory`, `GoogleService`, `GoogleSheetsClient`, `GoogleCredentialFactory`)

Located in `Integrations/GoogleFinancialSupport`. These are thin wrappers directly around Google's own .NET SDK (Drive API, Sheets API) for OAuth credential loading and file/sheet access.

## Accepted gap — no automated tests

This is a **deliberate, documented decision**, not an oversight:

- Exercising these classes for real requires live Google OAuth credentials — infeasible and undesirable in CI for a personal single-user project
- The classes have essentially no branching logic of their own; they configure and delegate to the Google SDK, which has its own test suite
- Per CLAUDE.md's explicit "does not over-engineer" guidance for this project, introducing a seam (e.g., wrapping the SDK behind a hand-rolled interface purely to enable fakes, the way `GoogleDriveJsonStorage` does with its download/upload delegates) was considered and rejected as disproportionate effort for a low-risk, low-change surface

**What *is* tested instead:** the parsing/resolution logic that sits next to these wrappers — `AssetClassificationLookupTests`, `AssetMetadataResolverTests`, `CountryCodeResolverTests`, `GoogleSheetValueParserTests` all cover pure logic with no SDK dependency. `GoogleFinancialSupportServiceCollectionExtensionsTests` covers DI wiring (see `artifacts/dependency-injection-modules.md`). Together these cover everything around the SDK boundary except the boundary crossing itself.

## If this ever needs to change

If a bug is ever traced to one of these wrapper classes specifically (not the logic around them), the right fix is the same seam `GoogleDriveJsonStorage` already uses: accept simple delegates (`Func`/`Action`) or a narrow interface in the constructor instead of the concrete Google SDK type, so a test can inject a fake without needing real credentials. Don't add this seam speculatively — only when a concrete bug or new requirement demands it (see CLAUDE.md: avoid designing for hypothetical future requirements).

## Examples from project

- `GoogleDriveClient`, `GoogleFileClientFactory`, `GoogleService`, `GoogleSheetsClient`, `GoogleCredentialFactory` — no test files exist for these, by design
- Contrast with `GoogleDriveJsonStorage` (`artifacts/infrastructure-persistence.md`), which already has the delegate seam and *is* tested
