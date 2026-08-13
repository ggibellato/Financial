# Persistence Alternatives Considered: Async Write-Behind vs. File-Based Transaction Ledger

**Status**: Async Write-Behind was adopted and formalized as [`P31-prd-async-persistence`](../prd-async-persistence.md). The File-Based Transaction Ledger draft in this folder was **not** adopted — it is kept here as a documented alternative, not a rejected/dead idea, because it is the right direction if multi-client access ever becomes a real requirement (see "When to revisit" below).

## Why two PRDs existed

Both drafts attack the same symptom: every mutation in CashFlow/Investment blocks on a full synchronous re-upload of a ~5.45MB JSON document to Google Drive before the API responds. They solve it in fundamentally different ways:

- **Async Write-Behind** (adopted, P31) is a **latency fix**. It keeps the current single-JSON-document-per-context model exactly as-is and moves the Drive upload off the request path (debounce + background push + retry + status banner).
- **File-Based Transaction Ledger** (this folder) is an **architecture replacement**. It discards the single-JSON-document model in favor of an append-only transaction log + periodic snapshot reconciliation + polling-based multi-client sync.

## Grounding from the codebase (verified, not assumed)

- `GoogleDriveClient` (Integrations/GoogleFinancialSupport) is a thin whole-file blob store — resolves file by name, full `Files.Get`/`Files.Update` download/upload, in-memory fileId cache. No ETags, revisions, or preconditions.
- `GoogleRetryPolicy` retries **only** HTTP 429, 5 attempts, 2/4/8/16/32s backoff — no 5xx/timeout/network retry today (this is what the adopted PRD's F02 explicitly fixes).
- Both CashFlow (`CashFlowJsonRepository`, 14 entity collections in `CashFlowData`) and Investment (`JSONRepository`) serialize their *entire* aggregate on every `SaveChangesAsync()` — no diffing, no per-entity writes.
- No Changes API, ETag, revision, or pageToken usage anywhere in the codebase today — the Transaction Ledger's polling/idempotency design would be entirely new infrastructure, not an extension of anything existing.
- Neither `Financial.Api/Program.cs` nor `Financial.App` currently hooks shutdown for a flush — both PRDs' shutdown-safety stories start from zero.
- **In-memory model lifecycle (verified directly in code)**: `ICashFlowRepository` and `IRepository` are registered as DI singletons; the backing `CashFlowData`/`Investments` object is loaded exactly once at startup via `CashFlowLoader.LoadSync` / `InvestmentsLoader` (both call `IJsonStorage.ReadAsync()` — the *only* production call sites for that method in the whole codebase). `SaveChangesAsync()` only serializes and writes; it never reads back or reloads. The in-memory model is the sole source of truth from startup until process restart.

## Side-by-side: PROS and CONS

### Async Write-Behind (adopted — P31)

**Pros**
- Minimal blast radius: a decorator (`IJsonStorage` wrapping `IJsonStorage`) — zero changes to `ICashFlowRepository`, `IRepository`, domain, or the data file format. Fully reversible.
- Small, incremental, reviewable — matches this project's "no over-engineering" ethos and single-maintainer reality.
- Directly fixes the measured pain: p95 mutation latency, with a concrete target (<100ms).
- Reuses the exact same storage abstraction (`IJsonStorage`) that already exists and is tested against — no new infrastructure class.
- Failure becomes visible instead of silent, via a sync-status banner in both front ends (doesn't exist today at all).
- Per-context isolation is explicit and enforced — CashFlow and Investment get fully independent write-behind instances.
- **Preserves the in-memory-model contract exactly**: `ReadAsync()` still only ever runs once at startup; the decorator changes *when* the background upload happens, never re-reads Drive to refresh memory.

**Cons**
- Does not remove the core scaling ceiling — every save is still a full whole-document re-upload, just relocated in time; grows heavier as the dataset grows.
- Durability gap during the debounce window is a real, accepted risk (hard kill/OOM loses the edit; only graceful shutdown is protected).
- **Explicitly out of scope for multi-client** — the PRD states the app "remains single-active-writer... never coordinate." Two concurrent writers → last debounced save silently clobbers the other, undetected.
- 8-second `FlushAsync` timeout tied to Docker's default SIGTERM→SIGKILL grace period is a fragile edge case if that default ever changes.
- No built-in "rebuild the model from scratch" recovery path beyond a full process restart (which re-triggers the existing `LoadSync`).

### File-Based Transaction Ledger (not adopted, archived here)

**Pros**
- Actually solves the scaling problem: writes become O(1) — a single small transaction file — regardless of overall dataset size.
- Built for multi-client from day one: optimistic per-client apply + poll-based sync (5–15s) + idempotent transaction IDs + resync-before-check for invariant-sensitive mutations.
- Strong crash-safety by construction: immutable, append-only transaction files; snapshot written as a new file before the manifest ever points to it; manifest updated last.
- No externally paid infrastructure — stays within Drive's free API quota using the Changes API instead of naive polling.
- Has an explicit Bootstrap/Reader component that doubles as a "rebuild state from scratch" recovery mechanism — no equivalent exists in the write-behind approach.

**Cons**
- Order-of-magnitude larger scope — effectively a rewrite of the persistence layer, none of it reuses `IJsonStorage`/`GoogleDriveJsonStorage`/`GoogleDriveClient` as they exist today. In tension with the project's "does not require to scale... does not over-engineer" guidance for what is currently a single-user app.
- Reads get slower, not faster: bootstrap means manifest → snapshot → list/fetch every transaction newer than the snapshot → replay — several sequential Drive calls versus today's single blob download.
- Business-invariant races are explicitly unsolved, only narrowed: the negative-balance race is mitigated (resync-before-check) but not closed; the fallback is "flag for manual review," i.e. bad state can land in Drive and stay there until a human notices.
- New failure surface with no current precedent in this codebase: reconcile-job idempotency, transaction ordering under clock skew, manifest/snapshot crash sequencing, folder growth/hygiene, pageToken persistence.
- Reconcile-service ownership is an open question, not a decision (its own §9: "does the single Reconcile owner assumption hold?"). The compare-and-swap protection needed for two clients both reconciling isn't built in v1 — a real gap in the exact scenario (multi-client) this PRD targets.
- No described migration path from the current 14 CashFlow entity collections + Investment aggregate into an initial snapshot + zero transactions.
- Domain mismatch: example payloads (`MoveMoney`, `Correct Balance`) are balance-shaped; no articulation of how the other 13 CashFlow entity types (Expenses, RecurringBills, Categories, etc.) map onto "transactions" in the same sense.
- **Introduces continuous background mutation of the in-memory model.** The Live Sync Loop applies incoming transactions from *other* clients to the running model every poll tick (5–15s). A single client's own writes never trigger a reload, but the "load once, only my own actions touch memory" invariant that holds today (and under the adopted PRD) no longer holds the moment a second client exists — by design, since that's the point of the approach.

## Multi-client fit (the deciding factor)

This is the sharpest dividing line between the two:

- **Async Write-Behind is a dead end for multi-client.** Its debounce/dirty-flag machinery is built on and scoped to single-writer usage. Adopting it now means a second migration project later if multi-client ever becomes real — none of it generalizes to concurrent writers.
- **Transaction Ledger is aimed at multi-client but ships v1 with known, acknowledged gaps**: no strict serializability, no distributed lock, "detect and flag" instead of "prevent" for invariant violations, and an unresolved single-Reconcile-owner assumption. It's a real foundation that can be hardened later (its own §8: CAS on manifest via ETag/revision, push notifications) without a second rewrite — but it is not multi-client-safe today either, just closer.

## Concrete failure scenarios

**Async Write-Behind**
1. Container OOM-killed or `docker kill` (not a graceful stop) during the debounce window → in-flight edit lost silently; the process dies before it can even report `Failed`.
2. User edits on Web, then within the same debounce window switches to WPF and edits the same entity → whichever debounced save lands last on Drive wins, silently discarding the other.
3. Sustained Drive outage beyond the retry cap → context sits in `Failed` indefinitely until the *next* mutation re-triggers a save cycle.

**Transaction Ledger**
1. Two clients both start reconciling near-simultaneously (no CAS in v1) → possible TOCTOU race on the "does this snapshot version already exist" idempotency check.
2. A balance-invariant race lands a negative balance in a snapshot → surfaces only as a `warnings` field a human must notice; no automatic correction.
3. Unbounded transaction-folder growth (archival deferred to future work) → every bootstrap/cold-start read gets slower over time if Reconcile ever lags.
4. Clock skew between devices → transaction ordering (timestamp-based when no sequence number is available) could apply mutations out of order for operations where order matters.

## Decision and rationale

Given this is currently a **single-user, self-hosted personal app** (per `CLAUDE.md`: "does not require to scale," "does not over-engineer") and the confirmed usage pattern is genuinely single-active-writer (one device active at a time, never concurrent edits):

- **Async Write-Behind was adopted as P31.** It fixes the actual, measured, current pain (blocking saves) with a small, reversible, well-scoped change that reuses existing abstractions and matches the project's engineering philosophy. It also happens to preserve the "load once, memory is truth, no reload after write" invariant exactly as it works today.
- **The Transaction Ledger approach is kept on file, not discarded.** Its core ideas (append-only transactions, snapshot+manifest, Changes-API polling) are sound and are the right foundation *when* multi-client access becomes real — but building it now, before there are two concurrent clients, would be exactly the over-engineering the project's `CLAUDE.md` warns against, trading a known small latency problem for a large amount of new, untested failure surface.

## When to revisit

Reopen the Transaction Ledger draft if multi-client access moves from hypothetical to a concrete near-term plan — e.g. a second household member gets their own device/login, or the WPF and Web front ends need to be used concurrently rather than one-at-a-time. At that point, P31's write-behind mechanism should be treated as something to be *replaced*, not extended — its single-writer assumption is load-bearing throughout.
