# Implementation Plan: F02. Per-Expense Tithe Contribution Flag

**Prerequisites:**
- .NET SDK / existing Financial.slnx build toolchain
- Node/npm for Financial.Web
- No new libraries, no new environment variables, no data migration tool

### Stage 1: Domain and Application

**1. Expense Entity** - Add the `CountsAsTithe` flag to the `Expense` domain entity, defaulting to `true` both when explicitly created and when an existing record is deserialized without the field, so historical expenses keep counting toward tithe exactly as they do today.

**2. Tithe Calculation** - Update `TitheService`'s "already paid" total so it only sums an expense when its category is tithe-flagged AND its `CountsAsTithe` flag is set, leaving expenses outside a tithe-flagged category unaffected regardless of the flag's value.

**3. Expense DTOs and Service** - Extend the Create/Update/Read DTOs with the new flag (defaulting to `true` when omitted) and update `ExpenseService` to pass it through to the domain entity and back out to the read model.

### Stage 2: Presentation

**4. Web Expense Form** - Add a "Counts toward tithe" checkbox to the Expense create/edit form, visible and editable only when the selected category is tithe-flagged, defaulting to checked for a new expense and reflecting the existing value when editing; wire it into the form's submit payload.

**5. WPF Expense Form** - Mirror the same checkbox behavior in the WPF Expense form: visible only for a tithe-flagged category, defaulting to checked on create, populated from the edited expense, and included when saving.

### Stage 3: Testing

**6. Domain and Application Tests** - Add coverage for the flag's default and explicit values on the entity, its effect (and non-effect) on the tithe calculation across tithe- and non-tithe-flagged categories, and its round-trip through the expense service.

**7. API and WPF Presentation Tests** - Extend Expense endpoint integration tests for the flag's default and explicit values, and add WPF view-model tests for the category-driven show/hide behavior and the default-on-create/populate-on-edit/save flow.

**8. Cross-Feature Integration Test** - Add a test confirming a bank-less income (F01) and an offer expense with the flag unchecked (F02), recorded together in the same month, both correctly affect the same month's tithe summary.
