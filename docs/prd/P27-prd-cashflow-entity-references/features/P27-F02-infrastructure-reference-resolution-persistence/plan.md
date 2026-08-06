# Implementation Plan: F02. Infrastructure Reference-Resolution Persistence

**Prerequisites:**
- .NET SDK matching the existing `Financial.CashFlow.*` projects
- No new NuGet packages or environment variables

### Stage 1: Reference Resolution Building Blocks

**1. Reference Resolution Context** - Add the lookup-table type that holds the resolved `Bank`/`IncomeSource`/`InvestmentAccount` collections keyed by Id for the duration of a single deserialize call.

**2. Per-Type Reference Converters** - Add the three small converters that read a Guid and resolve it against the context (throwing a descriptive error when unresolved) and write a reference-typed value back down to its `Id`.

### Stage 2: Top-Level Wiring

**3. CashFlowData Converter** - Add the converter that buffers the full document, resolves the three owning collections first regardless of JSON property order, builds the context, deserializes every other collection through it, and assembles the result via the existing `CashFlowData.Create()`/`Add*` API so reference identity holds by construction. Mirror the same collection-by-collection approach for writing.

**4. Type Resolver and Serializer Adapter Updates** - Extend `CashFlowTypeInfoResolver` to rename the 7 reference properties to their `*Id` wire names and attach the matching converter, then wire the new `CashFlowData` converter into `CashFlowSerializerAdapter`.

### Stage 3: Test Coverage

**5. Reference Converter and Context Tests** - Cover each reference converter's resolve/unresolved/write behavior and the context's lookup shape in isolation.

**6. Round-Trip and Error-Handling Tests** - Cover the full `CashFlowData` round-trip (Id-only wire shape, reference equality across all 5 referencing entities, order-independence) and both error paths (missing Id in the seeded collection, pre-F01/interim legacy shape), updating the existing serializer and repository tests to the new shape.
