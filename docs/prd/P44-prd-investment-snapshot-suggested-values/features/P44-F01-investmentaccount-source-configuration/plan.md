# Implementation Plan: F01. InvestmentAccount Source Configuration

**Prerequisites:**
- .NET 8 SDK and Node/npm toolchains, existing solution and `Financial.Web` building cleanly
- No new NuGet or npm packages required

### Stage 1: Domain and Application Layer

**1. Source Enum and Domain Invariant** - Add the `Source` classification enum and extend `InvestmentAccount` with the `Source`/linked-`CreditCard` properties, enforcing that the link is present only when the source is credit-card-backed, on both creation and update. Reference the spec for the exact shape guard.

**2. DTOs and Service Validation** - Extend the investment account read/create/update DTOs with the two new fields, and extend the service to resolve and validate the linked credit card (unresolvable or inactive both rejected) only when the source requires one, clearing it for every other source regardless of what was submitted.

### Stage 2: Persistence and Contract

**3. Persistence Wiring** - Register the new linked-credit-card reference in the CashFlow persistence type resolver so it serializes/deserializes through the existing credit card reference mechanism, and refresh the example data file with one account per non-default source.

**4. Domain, Application, and Infrastructure Tests** - Write the entity, service, and serializer test coverage described in the spec's testing strategy, including the shape guard, the inactive/unresolvable rejection, and the no-migration-needed default for a pre-existing record.

**5. Api Endpoint Tests and Contract Regeneration** - Extend the investment accounts endpoint test suite for the new server-side rejection and happy path, then regenerate the OpenAPI snapshot and the frontend's generated API types from it.

### Stage 3: Web

**6. Admin Dialog and List** - Add the Source dropdown and conditional credit-card picker/caption to the Add/Edit Investment Account dialog, and the Source column to the investment accounts list page, following the spec's UX flows for each source state and the inline validation rule.

**7. Web Component and Hook Tests** - Write the dialog, list page, and hook test coverage described in the spec's testing strategy, covering every source state, the inline validation block, and the inactive-linked-card display case.

### Stage 4: WPF

**8. Admin Dialog ViewModel and View** - Add the equivalent Source dropdown and conditional credit-card picker to the WPF Investment Account dialog and its ViewModel, wiring the credit-card list fetch and the same validation rule as the Web dialog.

**9. List ViewModel, View, and WPF Tests** - Add the Source column to the WPF investment accounts list (ViewModel + view), and write the ViewModel test coverage mirroring the Web test set for the same acceptance criteria.
