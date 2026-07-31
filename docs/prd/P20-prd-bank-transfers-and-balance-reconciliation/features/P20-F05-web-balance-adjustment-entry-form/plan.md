# Implementation Plan: Web Balance Adjustment Entry Form

**Prerequisites:**
- Existing Financial.Web toolchain (Vite, Vitest, React Testing Library) — no new dependencies

### Stage 1: API Client

**1. Balance Adjustment DTOs and Client Methods** - Add the read/create/update type shapes for a balance adjustment, and add client methods for creating and updating one against a specific bank, following the request/response pattern already used for transfers.

### Stage 2: State and Error Mapping

**2. Balance Adjustment Error Field Mapping** - Add a small, independently testable function that maps the backend's known balance adjustment validation error message to the target-balance field, with a safe fallback for messages it doesn't recognize.

**3. Balance Adjustment Form State Hook** - Add a dedicated hook that owns the create/edit form state for a balance adjustment (open/close, field values, saving/error state, the post-save delta), scoped to a specific bank and its current reference balance passed in by the caller rather than fetched again, and calls the new client methods.

### Stage 3: Presentational Component

**4. Balance Adjustment Form Component** - Add the presentational form component: a read-only current-balance reference line, target balance and date fields, an optional note, a post-save confirmation showing the backend-returned delta, and backend errors rendered under the field the mapping identifies (or as a general banner otherwise).
