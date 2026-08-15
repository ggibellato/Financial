# Implementation Plan: F01. Price History Recording

**Prerequisites:**
- None — this is a Wave 1 feature with no dependencies on other PRD A features.

**PR sizing note:** each phase below is scoped to land as its own small, independently reviewable PR (roughly 4 files each), per the approved implementation plan's file-count constraint. The user reviews and approves each PR before it merges and before the next phase starts.

### Stage 1: Domain

**1. AssetPriceSnapshot Entity** - Introduce the domain type representing one day's recorded price for an asset, with its own creation validation.

**2. Asset Price History Collection** - Extend the `Asset` aggregate with a price-history collection supporting upsert-by-date, exact-date lookup, and manual-only removal, following the same shape as the existing Credits collection on `Asset`.

### Stage 2: Infrastructure

**3. Persistence Wiring** - Register the new entity with the Investment context's JSON serialization contract so an asset's price history persists and reloads correctly, following the existing reflection-based approach already used for Credits and Transactions.

### Stage 3: Application

**4. Price Service** - Add an application service that finds the target asset by its broker/portfolio/name key and applies a set-price or delete-price mutation, following the existing pattern used for Credit mutations.

**5. Price History on Asset Details** - Expose the asset's recorded price history on the existing asset details read model, so downstream features (the Price History tab, and the current-value/XIRR fallback) can consume it without a separate endpoint.

### Stage 4: API

**6. Price Endpoints** - Add endpoints to set and delete a price for an asset on a given date, returning the updated asset details, following the existing pattern used for updating an investment snapshot's value.
