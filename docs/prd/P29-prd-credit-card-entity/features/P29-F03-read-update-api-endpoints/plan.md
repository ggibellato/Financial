# Implementation Plan: Read & Update API Endpoints

**Prerequisites:**
- F01 (CreditCard domain entity, seed migration) merged to `main`
- F02 (CreditCard reference wiring on Expense/CardStatement) merged to `main`
- No new tools or packages

### Stage 1: Domain and Application

**1. Credit Card Update Behavior** - Add a domain method on `CreditCard` that sets its next invoice due date and active flag together, following the same private-setter/behavior-method style already used for `Bank`'s opening balance.

**2. Update Contract and Service Method** - Add the request DTO carrying the two mutable fields, extend the credit card service interface with an update operation, and implement it to resolve the card by Id (raising a not-found error otherwise), apply the update, and persist the change.

### Stage 2: Presentation

**3. Credit Cards Controller** - Add a new controller exposing the list and update endpoints, following the existing read-only list controller pattern (`IncomeSourcesController`/`ReserveBucketsController`) for the GET and the existing resolve-or-404 update pattern (`BanksController`'s opening-balance PUT) for the PUT.

### Stage 3: Tests

**4. Domain and Service Tests** - Add coverage for the new `CreditCard` update behavior and the service's update method, including the not-found path.

**5. API Endpoint Tests** - Add coverage for the list endpoint (including inactive cards), the update endpoint's success/404/400 paths, and the cross-feature acceptance criterion that an update is immediately visible on a subsequent read.
