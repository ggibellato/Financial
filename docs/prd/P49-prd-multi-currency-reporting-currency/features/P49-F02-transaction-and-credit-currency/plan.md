# Implementation Plan: F02. Transaction and Credit Currency

**Prerequisites:**
- P49 F01 (Shared Exchange Rate Provider) merged — this feature consumes
  `Financial.Shared.Abstractions.Currencies.{Currency,IExchangeRateProvider}` and the
  `Integrations/Frankfurter` implementation already registered in `Financial.Investment.Infrastructure`'s
  DI container.
- No new external services or configuration.

### Stage 1: Domain Model

**1. Add the FX provenance types** - Create `FxRateSource` and `FxRateSnapshot` in
`Financial.Investment.Domain.Entities`, following the existing `AssetPriceSnapshot` shape
(private constructor, static `Create` factory, validated rate).

**2. Extend Transaction and Credit** - Add a required `Currency` and a nullable `FxRateSnapshot`
to both entities, threading both through their `Create`/`CreateWithId` factory methods.

**3. Reference the shared kernel from Investment.Domain** - Add the project reference to
`Financial.Shared.Abstractions` needed for the shared `Currency` type, mirroring the same
addition F01 made to `Financial.CashFlow.Domain`.

### Stage 2: Application Layer — Entry-Time Capture

**4. Add the reporting-currency seam** - Introduce `IReportingCurrencyProvider` and its interim
fixed-`GBP` implementation, registered in the Application DI extension, so later work (F03) only
has to swap the registration.

**5. Add the shared FX-capture helper** - Introduce a helper that resolves an Active broker's
currency by name and, when it differs from the reporting currency, calls the shared
`IExchangeRateProvider` for the record's own date to build an `FxRateSnapshot`.

**6. Wire capture into TransactionService and CreditService** - Update both services' add paths to
resolve currency/snapshot before their existing synchronous mutation runs, and update both
services' update paths to carry the original record's currency/snapshot forward unchanged rather
than recapturing them.

**7. Update the JSON type-info resolver** - Register the new `FxRateSnapshot` type so it
round-trips through the existing private-constructor JSON handling.

### Stage 3: Legacy Import Tool Compatibility

**8. Thread currency through the Google Sheets asset reader** - Update
`Tools/InvestmentSpreadsheetImport`'s broker-onboarding path to pass the already-resolved broker
currency into every `Transaction`/`Credit` it creates, without calling the FX provider.

### Stage 4: One-Time Migration Tool

**9. Create the migration tool project** - Add `Tools/InvestmentCurrencyBackfill`, following the
existing `Tools/InvestmentDataQualityReport` load/operate/report shape, extended here to also
write back.

**10. Implement the backfill migrator** - Walk every broker/portfolio/asset in the loaded
`Investments` aggregate and, for each transaction/credit missing a `Currency`, resolve it from the
parent broker and backfill an `FxRateSnapshot` via the shared exchange-rate provider (idempotent:
records that already have both fields are left untouched).

**11. Implement the migration summary report** - Produce counts of backfilled/already-set records
per entity type, and name every record left with a null snapshot because no rate was obtainable.

### Stage 5: Tests and Verification

**12. Unit-test the domain additions** - Cover `FxRateSnapshot`'s validation and the extended
`Transaction`/`Credit` factories.

**13. Unit-test the capture helper and both services** - Cover the same-currency, differing-currency,
and provider-returns-null cases, and confirm the update path never calls the provider.

**14. Add the AC-tracing Integration suite** - One tagged test per provable §9 criterion, through
the real API host with the exchange-rate provider stubbed, asserting directly against the
repository since no DTO exposes the new fields yet.

**15. Add an Infrastructure round-trip test** - Confirm a transaction/credit with a populated
`FxRateSnapshot` serializes and deserializes losslessly.

**16. Test the migration tool** - Cover backfill, idempotent skip, and the no-rate-obtainable
report path, plus one Integration test round-tripping a temp copy of a data file end-to-end.

**17. Full-suite verification** - Run the complete test suite and a full solution build to confirm
no regressions, then check the migration tool's report format is legible.
