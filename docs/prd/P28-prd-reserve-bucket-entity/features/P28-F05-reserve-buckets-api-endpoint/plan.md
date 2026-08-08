# Implementation Plan: Reserve Buckets API Endpoint

**Prerequisites:**
- F01 (ReserveBucket entity + seed migration) merged
- Mirrors the existing Bank/IncomeSource reference-data endpoint pattern

### Stage 1: Application Layer

**1. ReserveBucketDTO and Service** - Add the read model and a service that maps every seeded bucket to it, unfiltered, following `IncomeSourceDTO`/`IncomeSourceService` exactly. Register the service in DI.

### Stage 2: Presentation Layer

**2. ReserveBucketsController** - Add the read-only `GET /reserve-buckets` endpoint following `IncomeSourcesController`'s shape.

### Stage 3: Tests

**3. Service and Endpoint Tests** - Unit tests for the service's mapping behavior, and E2E tests for the endpoint's response shape, parameter-free contract, and read-only enforcement.
