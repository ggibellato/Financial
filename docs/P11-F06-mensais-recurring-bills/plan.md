# Implementation Plan: F06. Mensais Recurring Bills

**Prerequisites:**
- Existing `Financial.CashFlow.Domain`/`.Application`/`.Infrastructure` projects and `data-cashflow.json` repository pattern from F02
- No new NuGet packages
- No repository interface changes needed — F02's existing `GetRecurringBillTemplates`/`AddRecurringBillTemplate`/`GetRecurringBillInstances`/`AddRecurringBillInstance` are sufficient

### Stage 1: Domain Layer

**1. Area and BillStatus enums** - Add the 2-member `Area` enum (Brasil, UK) and the 3-member `BillStatus` enum (Unset, Scheduled, Paid).

**2. Flesh out RecurringBillTemplate** - Replace the placeholder with its real fields (due day, description, value, area, note, optional NIT number and minimum-wage value, active flag) and a `Create` factory.

**3. Flesh out RecurringBillInstance** - Replace the placeholder with its real fields (template reference, year, month, value, status) and both a `Create` factory (defaulting status to unset) and an `Update` method for in-place status/value edits.

**4. Domain tests** - Add tests for both entities' factories and the instance's update method.

### Stage 2: Application Layer

**5. Mensais DTOs** - Add the template read/create DTOs, the joined instance read model, and the instance update request DTO.

**6. Enum parsers** - Add parsers for `Area` and `BillStatus` string values, following the existing enum-parsing convention.

**7. Mensais service** - Add `IMensaisService`/`MensaisService` covering template creation, listing templates, idempotent lazy generation of a month's instances (joined with template fields for display), and independent instance status/value updates.

**8. Register the new service** - Add `IMensaisService` to the existing `CashFlowApplicationServiceCollectionExtensions`.

**9. Application-layer tests** - Add service tests covering template validation, the generation idempotency guarantee (including inactive templates being skipped), instance updates leaving the template and other months untouched, and the enum parsers.

### Stage 3: Presentation Layer

**10. Mensais controller** - Add HTTP endpoints for creating a template, listing templates, getting (and lazily generating) a month's instances, and updating a single instance, translating validation and not-found failures to 400/404.

**11. API integration tests** - Add endpoint tests covering the full create-template → get-month → update-instance round trip over HTTP, including the validation and not-found error responses.

### Stage 4: Verification

**12. Full-suite validation** - Build the entire solution and run every test project, confirming no regression to Investments or the existing CashFlow features from F01-F05.

**13. Manual verification** - Run `Financial.Api` locally and exercise the new endpoints directly (create a Brasil and a UK template, fetch a month to trigger generation, confirm exactly one instance per template, update an instance's status/value, re-fetch the template and other months to confirm nothing else changed) to confirm the behavior matches the acceptance criteria end-to-end.
