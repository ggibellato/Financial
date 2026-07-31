# Implementation Plan: Web Transfer Entry Form

**Prerequisites:**
- Existing Financial.Web toolchain (Vite, Vitest, React Testing Library) — no new dependencies

### Stage 1: API Client

**1. Transfer DTOs and Client Methods** - Add the transfer read/create/update type shapes to the frontend's type definitions, and add client methods for creating and updating a transfer, following the exact request/response pattern already used for income and expense.

### Stage 2: State and Error Mapping

**2. Transfer Error Field Mapping** - Add a small, independently testable function that maps the backend's known transfer validation error messages to the specific form field each one belongs to, with a safe fallback for messages it doesn't recognize.

**3. Transfer Form State Hook** - Add a dedicated hook that owns the create/edit form state for a transfer (open/close, field values, saving/error state) and calls the new client methods, defaulting the date to today when a new transfer is started and pre-filling every field when editing an existing one.

### Stage 3: Presentational Component

**4. Transfer Form Component** - Add the presentational form component: source and destination bank pickers (destination excludes the selected source), amount, date, and optional note fields, an immediate inline error when source and destination match, and backend errors rendered under the field the mapping identifies (or as a general banner otherwise).
