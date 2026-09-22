# Implementation Plan: F01. Persistent FX Rate Store

**Prerequisites:**
- .NET 10 SDK, existing `Financial.Shared.Abstractions` / `Financial.Shared.Infrastructure` projects
- No new NuGet packages — reuses `System.Text.Json`, existing `IJsonStorage`/`IJsonStorageFactory`/`DebouncedJsonStorage`/`ShutdownFlushHostedService<T>` primitives
- No environment variables; new configuration keys are added under the existing `appsettings*.json` files

### Stage 1: Domain Model and Serialization

**1. FX Rate Record and Store Contract** - Add the immutable stored-rate value type and the `IFxRateStore` interface to `Financial.Shared.Abstractions`, giving future features (F02+) a stable contract to depend on without knowing about JSON storage details.

**2. Serializer and Loader** - Add the JSON serializer adapter (with the `Version` marker convention) and a startup loader that tolerates a missing file or corrupted JSON by starting from an empty document, following the existing `CashFlowSerializerAdapter`/`CashFlowLoader` patterns.

### Stage 2: Store Implementation

**3. FxRateJsonStore** - Implement `IFxRateStore` over the loaded in-memory dictionary: a synchronous read path, and a gated, atomic write path that rejects today's date, persists the whole document, and never lets a storage failure propagate to the caller.

**4. Repository Selection and Factory** - Add the provider enum, selection options, and store factory that choose between local-file and Google Drive-backed storage exactly like the existing `InvestmentRepositoryFactory`/`CashFlowRepositoryFactory`, defaulting to local JSON with `data-fx-rates.json`.

### Stage 3: Configuration and Composition Root Wiring

**5. Configuration Keys and Options** - Add the `FxRates:*` configuration key constants and the corresponding settings options POCO, then register a DI extension method that binds configuration and exposes `IFxRateStore` as a singleton.

**6. Composition Root Registration** - Call the new DI extension method and register `ShutdownFlushHostedService<IFxRateStore>` in both `Financial.Api/Program.cs` and `Financial.App/App.xaml.cs`, and add the `FxRates` configuration block to every `appsettings*.json` file in both hosts.

**7. Example Data File and Gitignore** - Add the tracked `data/data-fx-rates.example.json` template matching the document shape, and extend `.gitignore` so the real `data/data-fx-rates.json` stays untracked like the other two data files.

### Stage 4: Tests

**8. Unit and Integration Test Coverage** - Add unit tests for the serializer, loader, store (including the today's-date rejection and write-failure-tolerance rules), and factory, plus an integration test for the DI extension method and an extension of the existing shutdown-flush registration test to cover the new store.
