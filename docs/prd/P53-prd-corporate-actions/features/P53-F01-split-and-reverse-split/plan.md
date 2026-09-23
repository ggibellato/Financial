# Implementation Plan: F01. Split and Reverse Split

**Prerequisites:**
- No new tools/libraries — uses the existing .NET 8 / System.Text.Json / FluentAssertions / xUnit stack already in `Financial.Investment.*`
- No configuration or environment variables

**PR slicing note:** Phases 1-2 (7 non-test files) are the Domain layer and form the first PR — self-contained, unreachable from any API yet, fully covered by unit tests, safe to merge on its own per CLAUDE.md's "vertical slice"/8-file guideline. Phases 3-4 (8 non-test files) wire it up end-to-end through the Application, Infrastructure and API layers and form the second PR.

### Stage 1: Corporate Action Entity and Replay Math

**1. Corporate Action Entity** - Add the `CorporateAction` domain entity (Split type only), following the same construction/validation pattern `Transaction`'s `Create`/`CreateWithId` factories already use, with the ratio-factor and note validation rules from spec.md §4.

**2. Merged Replay and Rescale Rule** - Add the shared rule that produces one date-ordered sequence out of a holding's transactions and corporate actions (corporate actions sorting before any same-day transaction, consistent with the existing purchases-before-sales convention), plus the pure position- and lot-rescale math a split applies.

### Stage 2: Position, Lot and Disposal Replay Integration

**3. Position Replay** - Update the holding's `Quantity`/`AveragePrice` replay so it consumes the merged transaction/corporate-action sequence from Stage 1 instead of transactions alone, including the same-day ordering guarantee.

**4. Open Lot Rescaling** - Update FIFO/SpecificId open-lot bookkeeping so every currently open lot is proportionally rescaled at a split's position in the replay, keeping total lot cost unchanged.

**5. Disposal Calculation and Regeneration** - Update average-cost disposal calculation and the existing disposal-record regeneration pipeline so a disposal computed after a split reflects the split's effect, and so recording, editing or deleting a corporate action re-triggers the same supersede-never-rewrite regeneration an edited transaction already triggers.

**6. Asset Aggregate Wiring** - Add `RecordCorporateAction`/`ReviseCorporateAction`/`RetractCorporateAction` to the holding aggregate: validate a non-zero position at the effective date, apply the same rollback-on-failure shape the existing transaction mutation methods use, and produce the specific rejection message when deleting a split would invalidate a later disposal's lot allocation.

### Stage 3: Application Service and Contracts

**7. Corporate Action Service Contract and DTOs** - Add the Application-layer service interface and request DTOs for recording, updating and deleting a split, following the existing transaction-service request/response shape.

**8. Corporate Action Service Implementation** - Implement the service against the existing asset-mutation and cost-basis-resolution helpers, with the same span/logging/error-handling shape every other Application service in this bounded context already uses.

### Stage 4: Persistence and API Wiring

**9. JSON Persistence Registration** - Register the new entity with the JSON serialization type resolver so it persists and reloads exactly like the existing disposal-record and tax-classification entities.

**10. Dependency Injection Registration** - Register the new service interface in the Investment Application layer's composition extension.

**11. API Endpoint** - Add the controller exposing record/update/delete over HTTP, following the existing transactions-controller conventions (route shape, null-body handling, response type), and refresh the OpenAPI contract snapshot (and generated frontend types) for the new routes.
