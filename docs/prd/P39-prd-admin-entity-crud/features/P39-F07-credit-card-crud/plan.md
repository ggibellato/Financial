# Implementation Plan: F07. Credit Card CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) merged — provides the Admin > CashFlow > Credit Cards nav leaf and placeholder route.
- `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` and `npm run generate-api-types` (Financial.Web) available for the API-contract phase.

### Stage 1: Domain and Application

**1. CreditCard domain update rule** - Rename `CreditCard.UpdateDetails` to `Update(name, isActive, nextInvoiceDueDate)`, adding the blank-name guard `Create` already enforces, mirroring `Bank.Update`. Add `RemoveCreditCard` to `CashFlowData`, mirroring `RemoveBank`.

**2. CreditCard repository plumbing** - Add `AddCreditCard`/`DeleteCreditCard` to `ICashFlowRepository` and implement them in `CashFlowJsonRepository`, mirroring the existing Bank pair.

**3. CreditCard Application service and DTOs** - Extend `ICreditCardService`/`CreditCardService` with Create/Delete, name-uniqueness and reference-guard checks (CreditCard referenced by an Expense or a CardStatement), and a `HasReferences`-bearing `CreditCardDTO`. Add `CreditCardCreateDTO`; extend `CreditCardUpdateDTO` with a required `Name`.

### Stage 2: API and Contract

**4. Credit Cards API endpoints** - Extend `CreditCardsController` with POST/DELETE and update the existing PUT to accept `Name`, following the established `BanksController` conventions, including 400/404/409 responses.

**5. OpenAPI contract regeneration** - Regenerate the pinned OpenAPI snapshot and the generated frontend TypeScript types, and confirm `tsc -b` is clean.

### Stage 3: Web UI

**6. Credit Cards admin screen (Web)** - Build `CreditCardsPage`, `CreditCardFormDialog`, and `useCreditCards`, following the Bank/Category admin screens' structure, states (loading/empty/validation/server-error/saving/success), and Fluent UI components. Wire the Credit Cards nav leaf to this page in place of the F01 placeholder. Update `CardsGrid.tsx`'s existing inline due-date/active update calls to pass the card's current `name` through the now-required field.

### Stage 4: WPF UI

**7. Credit Cards admin screen (WPF)** - Build `CreditCardsViewModel`, `CreditCardFormDialogViewModel`, `CreditCardsView`, and `CreditCardFormDialog`, mirroring the Web screen's workflow, field order, and validation. Register the view in `MainWindow.xaml.cs`. Update `CardsWorkflowViewModel`'s existing inline update call to pass `card.Name` through the now-required field.

### Stage 5: Verification

**8. Cross-feature and final verification** - Add integration coverage proving a CreditCard referenced by an Expense or a CardStatement blocks deletion and an unreferenced one deletes cleanly; confirm the existing Monthly-page inline card update workflow (Web and WPF) still passes its existing tests with `name` now included in the request payload. Run the full solution build and test suite (all .NET projects, Financial.Web lint/build/vitest) and confirm every F07 acceptance criterion holds before marking the feature complete.
