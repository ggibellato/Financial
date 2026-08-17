# Data & Persistence

See legend in [README.md](README.md).

## No database

**CONFIRMED — no relational database anywhere in the system.** Each bounded context persists to exactly one JSON document:

- Investment → `data/data-investment.json`
- CashFlow → `data/data-cashflow.json`

Only `*.example.json` templates are tracked in git; the real data files are gitignored.

## Load-once-at-startup model

**CONFIRMED, load-bearing across the whole system (`CLAUDE.md`).** Each context's JSON document is fully deserialized into memory **once**, at process startup, and held for the process's entire lifetime. Reads are pure in-memory LINQ over that object graph — no I/O per query. **Consequence:** after any migration tool run or manual edit to a data file, every running process (`Financial.Api`, `Financial.App`) must be **restarted**, not just re-queried, for the change to take effect. Restarting the container/process alone does not run a migration — migration tools must be invoked separately, or the JSON simply gets new-but-empty fields.

## Write behavior — full-document rewrite

**CONFIRMED — OBSERVED implementation.** `SaveChangesAsync` re-serializes and rewrites the **entire in-memory aggregate** to a JSON string on every save, regardless of how small the actual mutation was. Not an incremental/patch write.

- `LocalJsonStorage` — direct `File.ReadAllTextAsync`/`WriteAllTextAsync`. No file locking, no transactional semantics.
- `GoogleDriveJsonStorage` — wraps an `IRemoteFileClient` download/upload pair. When the `GoogleDrive` provider is selected, it's wrapped by `DebouncedJsonStorage`: a 10-second write-coalescing debounce window, retrying transient failures via `TransientRetryPolicy` (up to 5 retries). Save state is tracked via `ISyncStatusProvider`/`SyncState` (`Idle`/`Pending`/`Saving`/`Failed`) and surfaced to both UIs (`SyncStatusBanner` in Web, `SyncStatusViewModel` in WPF), polled via `SyncStatusController`.

**CONFIRMED — `Financial.Shared.Infrastructure/Sync` is status-reporting only, not conflict resolution.** There is no merge/CRDT/multi-writer handling anywhere in the persistence stack. Single-process, single-writer is an architecturally load-bearing assumption, not just a documented convention — and it does not account for two separate *processes* (`Financial.Api` and `Financial.App`) both writing to the same target concurrently, since there is no coordination between them (see [02-architecture.md](02-architecture.md)).

## Storage provider selection

**CONFIRMED, fully independent per bounded context** (`README.md`):

| Setting | Values | Default |
|---|---|---|
| `Investment:Repository:Provider` | `LocalJson`, `GoogleDrive` | `LocalJson` |
| `CashFlow:Repository:Provider` | `LocalJson`, `GoogleDrive` | `LocalJson` |

`GoogleDrive` requires `<Context>:GoogleDrive:CredentialsPath` (service-account credentials JSON) and `<Context>:GoogleDrive:FilePath` (Drive file ID or path). Provider resolution is handled by `RepositoryProviderResolver` + each context's own repository factory/provider.

## Operational guidance carried forward from prior incidents

- **Never run import/migration tools against the live data file** — always verify against a temp copy first.
- Restarting a container/process never substitutes for running a migration tool.
- When smoke-testing locally, check `netstat` first — the live Docker deployment binds port 8080 by default, and a local smoke test can collide with it.
