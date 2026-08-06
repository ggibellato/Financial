# Implementation Plan: Income Sources API Endpoint

**Prerequisites:**
- F01 merged (seeded `IncomeSource` entity, `ICashFlowRepository.GetIncomeSources()`)
- No new NuGet packages required

### Stage 1: Application Layer

**1. IncomeSourceDTO and Service** - Add the read model (`Id`, `Name`, `IsActive`, `Group` as a string) and a read-only service that maps every seeded income source to it, unfiltered. Register the service in the application's dependency injection module alongside the existing bank service.

### Stage 2: API Endpoint

**2. IncomeSourcesController** - Add a new controller exposing a single `GET /income-sources` action that returns the full list from the service, mirroring `GET /banks`'s shape and behavior exactly. No create/update/delete actions.
