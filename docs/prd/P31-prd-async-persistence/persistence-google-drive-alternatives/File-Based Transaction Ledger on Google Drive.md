# PRD: File-Based Transaction Ledger on Google Drive

## 1. Summary

A way to maintain a transactional ledger (bank balances, Move Money / Correct Balance operations, etc.) using Google Drive as the sole backing store — no message bus, no database, no paid backend service. Data mutations are written as small, immutable, append-only transaction files. A periodic Reconcile service folds these into snapshots. Clients read a snapshot + subsequent transactions to reconstruct current state, and stay in sync with each other via lightweight polling of Drive metadata.

## 2. Goals

- Persist all data mutations durably in Google Drive with no external DB/message bus.
- Make writes ("apply a transaction") fast and cheap — a single small file write.
- Allow any client to reconstruct current state by reading one snapshot + a bounded number of transaction files.
- Keep storage/API usage within Drive's free API quota.
- Support two (eventually more) clients editing concurrently, staying reasonably in sync (~5–15s) without paid infrastructure.
- Keep the system crash-safe: a failed write or a failed reconcile run must never corrupt or lose data.

## 3. Non-Goals (for this phase)

- Sub-second real-time sync (push notifications via a hosted webhook endpoint) — deferred to a future phase.
- Strong distributed locking / strict serializability across clients — deferred; see §8.
- Conflict resolution UI — out of scope for v1; conflicts are handled programmatically per rules in §7.

## 4. Components

### 4.1 Transaction Writer (client-side)
Runs inside each client app. On every data mutation (e.g. Move Money, Correct Balance):
1. Applies the change to the client's in-memory model immediately (optimistic local update).
2. Serializes the mutation as a small JSON transaction file and uploads it to the `transactions/` folder in Drive.
3. Does **not** wait for reconciliation — this write should be fast.

### 4.2 Reconcile Service
A background job (can run in any client, or as a scheduled task) that runs roughly every 10 minutes:
1. Reads the current pointer (`manifest.json`) to find the latest reconciled snapshot.
2. Lists all transaction files newer than that snapshot.
3. Applies them in order to the snapshot's state to compute a new state.
4. Writes the new state as a new, uniquely-named reconciled snapshot file (never overwrites the old one).
5. Updates `manifest.json` to point to the new snapshot **only after** the new snapshot file is confirmed written.
6. Leaves consumed transaction files in place (does not delete) — see §6 for why.

### 4.3 Reader / Bootstrap ("get latest version")
Used when a client starts up or needs to resync from scratch:
1. Read `manifest.json` to get the latest reconciled snapshot's filename/id.
2. Fetch that snapshot.
3. List/fetch transaction files newer than the snapshot's cutoff.
4. Apply them in order to produce current in-memory state.

### 4.4 Live Sync Loop (client-side, background)
While a client is running, it polls for new transactions from *other* clients and applies them incrementally (see §7).

## 5. File Formats & Naming

### 5.1 Transaction file
Path: `transactions/txn_{ISO8601Z}_{uuid}.json`

```json
{
  "id": "b3f1c2...-uuid",
  "type": "MoveMoney",
  "timestamp": "2026-08-03T14:22:05.123Z",
  "clientId": "device-A",
  "payload": {
    "fromBankId": "bank-1",
    "toBankId": "bank-2",
    "amount": 150.00
  }
}
```
- `id` (UUID) is the canonical unique identifier — used for idempotency, not the filename.
- Filename includes timestamp for human-sortability but must not be relied on as the sole ordering key (see §6, ordering).
- Once written, a transaction file is immutable and is never edited or deleted by normal operation.

### 5.2 Reconciled snapshot file
Path: `snapshots/recon_v{NNN}.json` (monotonically increasing version number)

```json
{
  "version": 42,
  "asOfTimestamp": "2026-08-03T14:20:00.000Z",
  "appliedTransactionIds": ["...", "..."],
  "banks": {
    "bank-1": { "balance": 1234.56 },
    "bank-2": { "balance": 789.00 }
  }
}
```
- `appliedTransactionIds` (or at minimum the max applied timestamp/sequence) lets the Reconcile job and readers determine exactly what's already folded in — needed for idempotent reconciliation (§6).

### 5.3 Manifest / pointer file
Path: `manifest.json`

```json
{
  "latestSnapshot": "snapshots/recon_v042.json",
  "updatedAt": "2026-08-03T14:20:03.500Z"
}
```
- Single small file, cheap to poll.
- Written **last**, after the snapshot it points to is confirmed on disk.

## 6. Reconcile Service — Detailed Behavior

**Ordering.** Transactions are ordered primarily by a client-generated monotonic sequence number if available; otherwise by timestamp, with tolerance for minor clock skew. Since balance mutations are commutative (sums), strict ordering only matters for operations with order-dependent business rules (see §7.1).

**Crash safety.** Sequence of operations, in order, so a crash at any point leaves the system in a recoverable state:
1. Write new snapshot file as a *new* file (old snapshot untouched).
2. Confirm write success (re-read or check response metadata).
3. Update `manifest.json` to point to the new snapshot.
4. Do **not** delete consumed transaction files immediately — retain them (optionally archive/move to `transactions/archived/` after N successful reconcile cycles, purely for folder hygiene, once confident no reader still needs them).

Reason for not deleting eagerly: if step 3 fails or a reader is mid-read against the old manifest, transaction files must still be present to reconstruct state.

**Idempotency.** If the Reconcile job runs twice over the same input (e.g. overlapping runs, retried after a timeout), it must not double-apply transactions. Achieved by:
- Naming/versioning the output snapshot deterministically from its inputs (e.g. snapshot version = previous version + 1, and the job checks whether a snapshot at that version already exists before writing — if so, treat as already done and skip).
- Every transaction has a globally unique `id`; the reconcile step tracks applied IDs and skips duplicates.

## 7. Multi-Client Sync

### 7.1 The concurrent-write problem
Because each mutation is its own file, two clients writing transactions "at the same time" never collide at the storage layer — there's no shared record being overwritten. The only real risk is a **business-invariant race**: e.g. two clients each independently pass a "balance can't go negative" check locally, then both write a Move Money that together violate it.

**v1 approach: optimistic, resync-before-check.**
Before executing a balance-sensitive mutation, the client does a quick sync pass (§7.2) to pull in any very-recent transactions from other clients, then evaluates the invariant against that freshly-synced state before writing. This narrows the race window to the time between "sync" and "write," which in practice (single-user, two-personal-devices scale) is small enough to accept.

**Fallback for missed violations:** the Reconcile service, when applying transactions, can detect a resulting negative/invalid balance and flag the snapshot (e.g. `"warnings": [...]`) for manual review rather than silently allowing bad state. No automatic rollback in v1.

### 7.2 Live sync loop (near-real-time, poll-based)
No push notifications, no hosted webhook endpoint — this stays entirely within free Drive API usage:

- Each client polls Drive roughly every 5–15 seconds using the **Changes API** (preferred over repeated `files.list`, since it returns only deltas since a saved `pageToken` — cheaper and avoids re-scanning the whole folder as it grows).
- On each poll, if new transaction files are found:
  1. Fetch just the new files.
  2. Apply each to the in-memory model using the same "apply transaction" function used by the Reconcile service (single shared implementation — avoid logic drift between live-apply and reconcile-apply).
  3. Skip any transaction `id` already applied (idempotency — protects against the client's own optimistic apply being re-applied when it sees its own file come back through the poll).
- A client's own writes are applied to its in-memory model **immediately and optimistically**, without waiting for its own poll cycle to pick them up.

Pseudocode:

```
onMutation(txn):
    applyToLocalModel(txn)        # optimistic, instant
    markApplied(txn.id)
    writeTransactionFile(txn)     # fire and forget-ish, but confirm success

onPollTick():
    newFiles = drive.changes.list(since=savedPageToken)
    for file in newFiles where file is a transaction and not already applied:
        txn = readTransactionFile(file)
        applyToLocalModel(txn)
        markApplied(txn.id)
    savedPageToken = newFiles.nextPageToken
```

### 7.3 Cost/quota consideration
Drive API free quota (thousands of requests/day per project) comfortably supports polling every 5–15s for a small number of personal clients. Using the Changes API instead of full folder listing keeps each poll to a single cheap metadata call in the common case (no changes).

## 8. Future Work (explicitly deferred)

- **True push notifications**: Drive supports push notification channels, but receiving them requires a publicly reachable HTTPS endpoint — i.e., hosting something (even free-tier: Cloudflare Worker, Google Cloud Function). Deferred until poll-based latency (~5–15s) proves insufficient in practice.
- **Optimistic concurrency on the manifest** for when the Reconcile service itself might run on more than one client at once: compare-and-swap style update using Drive's revision/ETag on `manifest.json` (read revision → reconcile → conditional write; on conflict, discard and retry against the new state). Not needed while Reconcile is single-owner.
- **Stronger invariant enforcement**: move from "detect and flag" to "detect and auto-correct/rollback" if business needs tighten.
- **Folder hygiene / archival**: move old transaction files out of the active listing path once several reconcile generations have safely passed, to keep poll/list operations fast as history grows.

## 9. Open Questions

- What business invariants (if any beyond non-negative balance) need protecting in the optimistic-write path?
- How many concurrent clients need to be supported long-term — does the "single Reconcile owner" assumption in §8 hold, or will two clients both attempt reconciliation?
- Retention policy for transaction files — keep forever, or archive/compact after some period?
- Should snapshot files also store per-bank running balances only, or a richer state (e.g. last N transactions inline) to reduce reader round-trips on cold start?