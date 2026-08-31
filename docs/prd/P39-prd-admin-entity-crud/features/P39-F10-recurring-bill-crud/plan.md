# Implementation Plan: F10. Recurring Bill CRUD

**Prerequisites:**
- F01 (Admin Navigation Foundation) merged — provides the Admin > CashFlow > Recurring Bills nav leaf and placeholder route.
- `UPDATE_OPENAPI_SNAPSHOT=1 dotnet test Tests/Financial.Api.Tests` and `npm run generate-api-types` (Financial.Web) available for the API-contract phase.
- Unlike F02/F05-F09, Create/List/Delete already work end to end (`MensaisService`/`MensaisController`, route `/mensais`) — only Update needs widening, plus the two new Admin screens.

### Stage 1: Domain and Application

**1. RecurringBill domain update rule** - Replace `RecurringBill.Update(status, value)` with a full-field `Update(dueDay, description, value, area, note, nitNumber, minimumWageValue, status)`, reusing the existing `Validate(dueDay, description)` guard.

**2. RecurringBill Application service and DTO** - Extend `RecurringBillUpdateDTO` with `DueDay`, `Description`, `Area`, `Note`, `NitNumber`, `MinimumWageValue`. Extend `MensaisService.UpdateBillAsync` to parse `Area` (mirroring `CreateBillAsync`) alongside the existing `Status` parse, and pass every field to the widened `RecurringBill.Update`.

### Stage 2: API and Contract

**3. Recurring Bill API contract** - Update `MensaisController.UpdateBill`'s XML doc to reflect full-field editing (route/status codes unchanged). Regenerate the pinned OpenAPI snapshot and the generated frontend TypeScript types, and confirm `tsc -b` is clean.

### Stage 3: Web UI

**4. Recurring Bills admin screen (Web)** - Build `RecurringBillsPage`, `RecurringBillFormDialog` (DueDay, Description, Value, Area select, Note, NitNumber, MinimumWageValue, Status select), and a new `useRecurringBills` hook over the existing `/mensais` endpoints, following the other Admin screens' structure, states, and Fluent UI components; sortable by DueDay. Wire the Recurring Bills nav leaf to this page in place of the F01 placeholder. Update `useMensais.ts`'s existing inline Status/Value update call to thread the bill's current DueDay/Description/Area/Note/NitNumber/MinimumWageValue through the now-required fields, with no UX change to the Monthly page.

### Stage 4: WPF UI

**5. Recurring Bills admin screen (WPF)** - Build `RecurringBillsViewModel`, `RecurringBillFormDialogViewModel`, `RecurringBillsView`, and `RecurringBillFormDialog`, mirroring the Web screen's workflow, field order, and validation. Add `ShowRecurringBillFormDialog` to `IDialogService`/`DialogService`. Register the view in `MainWindow.xaml.cs`/`App.xaml.cs`. Update `MensaisViewModel`'s existing inline update call to thread the same six fields through, with no UX change to the Monthly page.

### Stage 5: Verification

**6. Cross-feature and final verification** - Confirm the existing Monthly-page inline Status/Value update workflow (Web and WPF) still passes its existing tests with every other field now included in the request payload, and that Create/Delete/Get/Reset remain unaffected. Run the full solution build and test suite (all .NET projects, Financial.Web lint/build/vitest) and confirm every F10 acceptance criterion holds before marking the feature complete.
