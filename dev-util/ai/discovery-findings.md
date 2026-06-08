# Discovery Findings – Financial Project

**Generated:** 2026-06-08  
**Updated:** 2026-06-08  
**Source:** Codebase analysis + context.md

---

## 0. Technology Stack

### Backend (.NET)

| Component | Version / Details |
|---|---|
| Language | C# 13 |
| Runtime | .NET 10.0 (`net10.0`) |
| Web framework | ASP.NET Core (Web API) |
| Desktop framework | WPF (Windows Presentation Foundation) |
| `Microsoft.AspNetCore.OpenApi` | 10.0.6 |
| `HtmlAgilityPack` | 1.11.57 |
| `Google.Apis` | 1.64.0 |
| `Google.Apis.Auth` | 1.64.0 |
| `Google.Apis.Core` | 1.64.0 |
| `Google.Apis.Drive.v3` | 1.64.0.3256 |
| `Google.Apis.Sheets.v4` | 1.64.0.3148 |
| `xunit` | 2.9.3 |
| `FluentAssertions` | 6.12.0 |
| `Microsoft.NET.Test.Sdk` | 17.14.1 |
| `coverlet.collector` | 6.0.4 |

### Frontend (JavaScript/TypeScript)

| Component | Version |
|---|---|
| React | 19.2.4 |
| TypeScript | 6.0.2 |
| Vite | 8.0.4 |
| react-router-dom | 7.14.1 |
| recharts | 3.8.1 |
| Vitest | 4.1.4 |
| @testing-library/react | 16.3.2 |
| @testing-library/user-event | 14.6.1 |
| jsdom | 29.0.2 |
| Node.js (CI) | 24.13.0 |

---

## 1. Project Overview

The Financial project is a **personal financial management tool** designed to consolidate investment transactions across multiple brokers and countries (Brazil and United Kingdom). It supports multiple currencies, multiple asset classes (Shares, ETFs, REITs/FIIs, Government Bonds, Bitcoin, ISAs), and is designed for annual tax reporting and investment performance tracking.

Three client applications share a common domain and infrastructure:

- `Financial.Api` — ASP.NET Core REST API
- `Financial.App` — WPF desktop application (Windows)
- `Financial.Web` — React (TypeScript + Vite) web frontend

---

## 2. Repository Structure

```
Financial/
├── Financial.Api/             ASP.NET Core Web API (entry point)
├── Financial.App/             WPF Desktop application (entry point)
├── Financial.Web/             React/TypeScript web frontend
├── Financial.Application/     Application layer (interfaces, DTOs, validation)
├── Financial.Domain/          Domain layer (entities, business logic)
├── Financial.Infrastructure/  Infrastructure layer (repositories, services, persistence)
├── Financial.Common/          Shared utilities (JSON serialization helpers)
├── Integrations/              External integrations (Google Finance, web scrapers, Drive)
├── Tests/
│   ├── Financial.Api.Tests/
│   ├── Financial.Application.Tests/
│   ├── Financial.Domain.Tests/
│   └── Financial.Infrastructure.Tests/
├── context.md                 Master prompt and project context
└── dev-util/                  Development utilities and prompt files
```

---

## 3. Architectural Patterns

### Clean Architecture (Layered)

The project follows **Clean Architecture** with strict inward dependency direction:

```
API / App / Web  →  Application  →  Domain
              Infrastructure  →  Application + Domain
```

- **`Financial.Domain`** — Pure domain model. No external dependencies. Contains entities and business rules only.
- **`Financial.Application`** — Interfaces (ports), DTOs, and validation. No implementations. References Domain only.
- **`Financial.Infrastructure`** — Implements Application interfaces. Contains repositories, services, and persistence. References Application + Domain.
- **`Financial.Api` / `Financial.App`** — Presentation/entry point. Wires dependencies via DI. References Application interfaces only (not Infrastructure directly).

### Data Hierarchy

The core domain model follows a strict containment hierarchy:

```
Investments → Broker(s) → Portfolio(s) → Asset(s) → Operations / Credits
```

### Storage Strategy

All data is stored as a **single JSON file** (local disk or Google Drive). The full `Investments` object graph is loaded in-memory at startup. No relational database is used.

---

## 4. Key Code Patterns

### 4.1 Domain Entity — Static Factory + Private Constructor

All domain entities use **private constructors** (for JSON deserialization) and **static factory methods** for controlled creation. Properties use `private set` with `[JsonInclude]`. Computed properties use `[JsonIgnore]`.

**`Financial.Domain/Entities/Operation.cs`**
```csharp
public class Operation
{
    [JsonInclude] public Guid Id { get; private set; }
    [JsonInclude] public DateTime Date { get; private set; }
    [JsonInclude] public OperationType Type { get; private set; }
    [JsonIgnore]  public decimal TotalPrice => UnitPrice * Quantity + Fees;

    [JsonConstructor]
    private Operation() { }

    public static Operation Create(DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(Guid.NewGuid(), date, type, quantity, unitPrice, fees);

    public static Operation CreateWithId(Guid id, DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(id, date, type, quantity, unitPrice, fees);
}
```

**`Financial.Domain/Entities/Asset.cs`** (overloaded factory)
```csharp
public static Asset Create(string name, string isin, string exchange, string ticker) =>
    new(name, isin, exchange, ticker, CountryCode.Unknown, string.Empty, GlobalAssetClass.Unknown);

public static Asset Create(string name, string isin, string exchange, string ticker,
    CountryCode country, string localTypeCode) { ... }

public static Asset Create(string name, string isin, string exchange, string ticker,
    CountryCode country, string localTypeCode, GlobalAssetClass assetClass) { ... }
```

### 4.2 Aggregate Root — Asset

`Asset` is the primary aggregate root. It owns `Operations` and `Credits` as private `List<T>` exposed as `IReadOnlyCollection<T>`. All mutations are encapsulated. Average price and quantity are derived state maintained by the entity itself. Rebuild-from-scratch is used for update/delete to keep invariants consistent.

**`Financial.Domain/Entities/Asset.cs`**
```csharp
private List<Operation> _operations = new List<Operation>();
[JsonInclude]
public IReadOnlyCollection<Operation> Operations
    { get => _operations.AsReadOnly(); set => SetOperations(value); }

public void AddOperation(Operation operation) { ... }        // recalculates AvargePrice + Quantity
public bool UpdateOperation(Operation updatedOperation) { ... } // rebuilds from scratch via RebuildOperations()
public bool RemoveOperation(Guid operationId) { ... }        // rebuilds from scratch via RebuildOperations()
```

### 4.3 Service Pattern — Interface in Application, Implementation in Infrastructure

All services are defined as interfaces in `Financial.Application.Interfaces` and implemented as `sealed` classes in `Financial.Infrastructure.Services`.

| Interface | Implementation | Responsibility |
|---|---|---|
| `IOperationService` | `OperationService` | Buy/Sell operation CRUD |
| `ICreditService` | `CreditService` | Dividend/rent credit CRUD |
| `IAssetPriceService` | `AssetPriceService` | Live price lookup via Google Finance |
| `IDividendService` | `DividendService` | Dividend history and yield analysis |
| `INavigationService` | `NavigationService` | Hierarchical tree navigation and DTO mapping |
| `IRepository` | `JSONRepository` | Data access |
| `IJsonStorage` | `LocalJsonStorage` / `GoogleDriveJsonStorage` | Storage backend |

### 4.4 Repository Pattern

`IRepository` is defined in Application. The single implementation `JSONRepository` loads the full `Investments` graph in-memory on construction. Mutations apply to the in-memory model and are flushed via `SaveChanges()`.

**`Financial.Infrastructure/Repositories/JSONRepository.cs`**
```csharp
public sealed class JSONRepository : IRepository
{
    private readonly IJsonStorage _storage;
    private readonly Investments _investiments;

    public JSONRepository(IJsonStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _investiments = LoadInvestments(_storage);
    }

    public void SaveChanges()
    {
        var json = _investiments.Serialize();
        _storage.WriteAsync(json).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
```

### 4.5 AssetServiceHelper — Shared Mutation Pipeline

A static internal helper class provides a reusable pipeline shared by all write services:
validate context → get asset → apply mutation lambda → save → return updated `AssetDetailsDTO`.

**`Financial.Infrastructure/Services/AssetServiceHelper.cs`**
```csharp
public static AssetDetailsDTO? ExecuteAssetMutation(
    IRepository repository,
    INavigationService navigationService,
    string? brokerName, string? portfolioName, string? assetName,
    Func<Asset, bool> mutation)
{
    if (IsInvalidContext(brokerName, portfolioName, assetName)) return null;
    var asset = repository.GetAsset(brokerName!, portfolioName!, assetName!);
    if (asset == null) return null;
    if (!mutation(asset)) return null;
    repository.SaveChanges();
    return navigationService.GetAssetDetails(brokerName!, portfolioName!, assetName!);
}
```

`ExecuteParsedMutation<TEnum>` extends this for operations requiring enum string parsing before mutation.

### 4.6 DTO Pattern

DTOs are plain C# classes in `Financial.Application.DTOs`. They serve as the contract between layers.

- **Create DTOs** (`OperationCreateDTO`, `CreditCreateDTO`) — include `required` broker/portfolio/asset context strings
- **Update DTOs** (`OperationUpdateDTO`, `CreditUpdateDTO`) — include `Guid Id` + context
- **Delete DTOs** (`OperationDeleteDTO`, `CreditDeleteDTO`) — minimal: context + `Guid Id`
- **Read/Info DTOs** (`AssetDetailsDTO`, `AssetInfoDTO`, `BrokerInfoDTO`) — rich display data
- **Tree Node DTOs** (`TreeNodeDTO`, `BrokerNodeDTO`, `PortfolioNodeDTO`, `AssetNodeDTO`) — hierarchical navigation

### 4.7 Controller Pattern

Controllers are thin wrappers over Application service interfaces. Null responses from services map to `BadRequest()`.

**`Financial.Api/Controllers/OperationsController.cs`**
```csharp
[ApiController]
[Route("operations")]
public sealed class OperationsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> AddOperation([FromBody] OperationCreateDTO request)
    {
        if (request is null) return BadRequest();
        var asset = _operationService.AddOperation(request);
        if (asset is null) return BadRequest();
        return Ok(asset);
    }
}
```

API base route prefix: `/api/v1/financial`

### 4.8 Validation Parsers

Enum values arriving as strings from the API or UI are normalized and parsed case-insensitively via helpers in `Financial.Application.Validation`.

**`Financial.Application/Validation/EnumParser.cs`**
```csharp
public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
    where TEnum : struct, Enum
{
    if (string.IsNullOrWhiteSpace(value)) { parsed = default; return false; }
    return Enum.TryParse(value, true, out parsed);
}
```

Specific parsers: `OperationTypeParser` (Buy/Sell), `CreditTypeParser` (Dividend/Rent).

### 4.9 WPF MVVM (Financial.App)

The desktop app uses the MVVM pattern:

- `ViewModelBase` — base class with `INotifyPropertyChanged`
- `RelayCommand` — `ICommand` implementation
- `MainNavigationViewModel` — top-level coordinator (inherits `MainNavigationViewModelBase<T>`)
- `AssetDetailsViewModel` — detail panel with operations/credits tabs
- `OperationActions` / `CreditActions` — separate command logic classes
- `TodayInfoTracker` — helper for live price display

---

## 5. Dependency and Integration Patterns

### DI Registration (ASP.NET Core)

**`Financial.Api/Program.cs`**
```csharp
builder.Services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
builder.Services.AddSingleton<IRepository>(sp => factory.Create(options)); // factory-driven
builder.Services.AddSingleton<INavigationService, NavigationService>();
builder.Services.AddSingleton<IOperationService, OperationService>();
builder.Services.AddSingleton<ICreditService, CreditService>();
builder.Services.AddSingleton<IAssetPriceService, AssetPriceService>();
builder.Services.AddSingleton<IDividendService, DividendService>();
```

All services registered as **singletons** (repository is shared in-memory state).

### Storage Backend Selection

Driven by the `Repository:Provider` config key via `RepositoryProvider` enum:

| Provider | Implementation | Required Config |
|---|---|---|
| `LocalJson` | `LocalJsonStorage` | `DataJsonFile` path |
| `GoogleDriveJson` | `GoogleDriveJsonStorage` | `GoogleDrive:CredentialsPath`, `GoogleDrive:FilePath` |

Selection implemented via a factory:
**`Financial.Infrastructure/Repositories/RepositoryFactory.cs`**
```csharp
return options.Provider switch
{
    RepositoryProvider.LocalJson       => new JSONRepository(new LocalJsonStorage(options.LocalDataPath)),
    RepositoryProvider.GoogleDriveJson => new JSONRepository(new GoogleDriveJsonStorage(...)),
    _ => throw new ArgumentOutOfRangeException(...)
};
```

### External Integrations (`Integrations/`)

| Folder | Purpose |
|---|---|
| `GoogleFinancialSupport` | Live stock price scraping via Google Finance |
| `WebPageParser` | Dividend history scraping (DadosMercado) |
| `GoogleDriveSupport` (FinancialToolSupport) | Google Drive file read/write for JSON storage |
| `ImportGoogleSpreadSheets` | Import portfolio data from Google Sheets |

### Asset Classification

`GlobalAssetClassMapping` in Domain maps `(CountryCode, LocalTypeCode)` → `GlobalAssetClass`. Case-insensitive comparison via custom `IEqualityComparer`. Extensible via dictionary entries.

**`Financial.Domain/Entities/AssetClassification.cs`** — complete mapping:

| CountryCode | LocalTypeCode | GlobalAssetClass |
|---|---|---|
| BR | Acoes | Equity |
| BR | FII | RealEstate |
| BR | ETF | ETF |
| BR | Fund | Fund |
| BR | Bond | Bond |
| BR | TesouroDireto | Bond |
| US | REIT | RealEstate |
| US | Stock | Equity |
| US | ETF | ETF |
| US | Fund | Fund |
| US | Bond | Bond |
| US | T-Bill | Bond |
| US | Cash | Cash |
| US | Pension | Pension |
| UK | REIT | RealEstate |
| UK | Stock | Equity |
| UK | ETF | ETF |
| UK | Fund | Fund |
| UK | Bond | Bond |
| UK | ConventionalGilt | Bond |
| UK | Cash | Cash |
| UK | Pension | Pension |

Lookup is case-insensitive. Returns `GlobalAssetClass.Unknown` for unrecognised combinations.

### JSON Serialization

- `PrivateConstructorContractResolver` (`Financial.Common`) — enables deserialization of entities with private constructors
- `[JsonInclude]` — marks private-set properties for serialization
- `[JsonIgnore]` — excludes computed properties (`TotalPrice`, `AvargePrice`, `Active`)
- `JsonStringEnumConverter` — serializes enums as strings
- `Investments.Serialize()` / `Investments.Deserialize()` own the full JSON round-trip

### React Web Frontend (`Financial.Web`)

- React 19 + TypeScript + Vite
- `react-router-dom` v7 for routing
- `recharts` for charts
- API client at `src/api/financialApiClient.ts`
- Pages: Navigation tree, Broker detail, Asset detail, Credits, Dividends, Current Values
- Tests via Vitest + `@testing-library/react`

---

## 6. Testing Patterns

### Test Projects

| Project | Focus |
|---|---|
| `Financial.Domain.Tests` | Unit tests for entity behavior and domain business rules |
| `Financial.Application.Tests` | Unit tests for validation parsers |
| `Financial.Infrastructure.Tests` | Integration tests for repository and services |
| `Financial.Api.Tests` | End-to-end HTTP endpoint integration tests |

### Frameworks

- **xUnit** — test runner
- **FluentAssertions** — assertion library (`Should().Be(...)`, `Should().NotBeNull()`, etc.)
- **`WebApplicationFactory<Program>`** — full API integration testing (ASP.NET Core in-process)

### Domain Test Example

**`Tests/Financial.Domain.Tests/Domain/AssetTests.cs`**
```csharp
[Fact]
public void AddOperation_Buy_UpdatesAveragePriceAndQuantity()
{
    var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
    var first  = Operation.Create(new DateTime(2024, 1, 1), Operation.OperationType.Buy, 10m, 5m, 0m);
    var second = Operation.Create(new DateTime(2024, 1, 2), Operation.OperationType.Buy, 10m, 7m, 0m);
    asset.AddOperation(first);
    asset.AddOperation(second);
    asset.Quantity.Should().Be(20m);
    asset.AvargePrice.Should().Be(6m);
    asset.Active.Should().BeTrue();
}
```

### Infrastructure Test Example

**`Tests/Financial.Infrastructure.Tests/Repositories/JsonRepositoryTests.cs`**
```csharp
private readonly JSONRepository _sut = new JSONRepository(new LocalJsonStorage(TestDataPaths.DataJsonFile));

[Theory]
[InlineData("XPI", 1)]
[InlineData("NOTEXIST", 0)]
public void GetAssets_By_BrokerTest(string? name, int records)
{
    _sut.GetAssetsByBroker(name ?? string.Empty).Should().HaveCount(records);
}
```

### API Integration Test Example

**`Tests/Financial.Api.Tests/ApiTestFactory.cs`** — each test gets an isolated temp copy of the JSON data:
```csharp
internal sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Repository:Provider"] = "LocalJson",
                ["DataJsonFile"] = _dataFilePath   // isolated temp copy per test factory
            }));
    }
}
```

Temp file is deleted in `Dispose`. Tests use `PostAsJsonAsync` / `PutAsJsonAsync` / `SendAsync` and assert with FluentAssertions.

---

## 7. Naming Conventions

| Element | Convention | Examples |
|---|---|---|
| Domain entities | PascalCase noun | `Asset`, `Operation`, `Credit`, `Broker`, `Portfolio` |
| Interfaces | `I` prefix | `IOperationService`, `IRepository`, `IJsonStorage` |
| Service implementations | `{Name}Service` `sealed` | `OperationService`, `NavigationService` |
| Controllers | `{Name}Controller` `sealed` | `OperationsController`, `CreditsController` |
| Create DTOs | `{Entity}CreateDTO` | `OperationCreateDTO`, `CreditCreateDTO` |
| Update DTOs | `{Entity}UpdateDTO` | `OperationUpdateDTO`, `CreditUpdateDTO` |
| Delete DTOs | `{Entity}DeleteDTO` | `OperationDeleteDTO`, `CreditDeleteDTO` |
| Read/info DTOs | `{Entity}InfoDTO` / `{Entity}DetailsDTO` | `AssetInfoDTO`, `AssetDetailsDTO` |
| Tree node DTOs | `{Entity}NodeDTO` | `BrokerNodeDTO`, `PortfolioNodeDTO`, `AssetNodeDTO` |
| Test classes | `{Subject}Tests` | `AssetTests`, `JsonRepositoryTests` |
| Test factories | `{Name}TestFactory` | `ApiTestFactory` |
| Private backing fields | `_camelCase` | `_operations`, `_brokers`, `_investiments` |
| Config key constants | `{Name}ConfigurationKey` in owning class | `DataJsonFileConfigurationKey`, `CredentialsPathConfigurationKey` |
| Enum parsers | `{EnumName}Parser` | `OperationTypeParser`, `CreditTypeParser` |

---

## 8. Notable Observations

1. **Single JSON file as database.** The entire portfolio is one JSON blob. All data is loaded in-memory at startup and flushed on each mutation. Designed for simplicity and portability (Google Drive sync).

2. **`SaveChanges()` is sync-over-async.** Storage operations are async (`IJsonStorage`) but `SaveChanges()` blocks via `.GetAwaiter().GetResult()`. Acceptable given the singleton in-memory model.

3. **`AvargePrice` typo in domain.** `Asset.AvargePrice` is missing the second 'e' in "Average". This typo is present in the property name, the DTO mappings, and tests. **Do not rename without a full coordinated refactor.**

4. **Domain owns its JSON round-trip.** `Investments.Serialize()` / `Investments.Deserialize()` live in the Domain entity using `PrivateConstructorContractResolver`. This pragmatically couples the domain to `System.Text.Json` but keeps serialization co-located with the aggregate root.

5. **"Encerradas" is a reserved portfolio name.** `NavigationService` contains a hard-coded convention that sorts any portfolio named "Encerradas" (Portuguese: "Closed") last in all navigation listings.

6. **`Asset.Active` is fully computed.** `Active => Quantity > 0`. An asset becomes inactive when fully sold. Operations are never deleted — full history is always preserved.

7. **Three presentation layers share the same Application/Infrastructure.** `Financial.Api`, `Financial.App` (WPF), and `Financial.Web` (React, separate process) all consume the same backend. The API and WPF each register the same DI bindings independently.

8. **`GlobalAssetClassMapping` is the primary extension point for new asset classes.** Adding a new asset type requires a new dictionary entry (country + local type code) and potentially a new `GlobalAssetClass` enum value.

9. **API test isolation via temp file copy.** Each `ApiTestFactory` instance copies the test JSON to a unique temp path. Isolation is guaranteed without any database cleanup logic. The temp file is deleted in `Dispose`.

10. **No MediatR, FluentValidation, or CQRS pipeline.** The project uses direct service calls and hand-written validation parsers. Simple and explicit by design.

11. **`DiagnosticsController` exposes a `GET /health` endpoint** and a development-only `GET /config/repository` endpoint that returns the active storage configuration. Both are at the root path (not under the `/api/v1/financial` prefix).

---

## 9. CI/CD Pipeline

**File:** `.github/workflows/build.yml`  
**Trigger:** push or pull_request to `main`

### Job: `build-and-test` (Windows)

Runs on `windows-latest` using .NET 10.0.x.

```
1. Checkout code          (actions/checkout@v6)
2. Setup .NET 10.0.x      (actions/setup-dotnet@v4)
3. Restore dependencies   (dotnet restore)
4. Build (Release)        (dotnet build --configuration Release --no-restore)
5. Run tests              (dotnet test --configuration Release --no-build)
```

### Job: `web-build-test` (Ubuntu)

Runs on `ubuntu-latest` using Node.js 24.13.0.

```
1. Checkout code          (actions/checkout@v6)
2. Setup Node 24.13.0     (actions/setup-node@v6)
3. Install dependencies   (npm ci, working-directory: Financial.Web)
4. Run tests              (npm test, working-directory: Financial.Web)
5. Build                  (npm run build = tsc -b && vite build)
```

The two jobs run in parallel. There is also a `semantic-pr.yml` workflow (PR title linting, not covered here).

---

## 10. API Endpoint Reference

All endpoints are grouped under the route prefix **`/api/v1/financial`** (`Financial.Api/Program.cs:83`).

### Navigation

| Method | Route | Request | Response | Notes |
|---|---|---|---|---|
| GET | `/navigation/tree` | — | `TreeNodeDTO` | Full hierarchical portfolio tree |
| GET | `/navigation/brokers` | — | `BrokerNodeDTO[]` | All brokers with their portfolios |

### Assets

| Method | Route | Request | Response | Notes |
|---|---|---|---|---|
| GET | `/assets/{brokerName}/{portfolioName}/{assetName}` | path params | `AssetDetailsDTO` | 404 if not found |

### Operations

| Method | Route | Request | Response | Notes |
|---|---|---|---|---|
| POST | `/operations` | `OperationCreateDTO` (body) | `AssetDetailsDTO` | 400 on null or invalid |
| PUT | `/operations` | `OperationUpdateDTO` (body) | `AssetDetailsDTO` | 400 on null or invalid |
| DELETE | `/operations` | `OperationDeleteDTO` (body) | `AssetDetailsDTO` | 400 on null or invalid |

### Credits

| Method | Route | Request | Response | Notes |
|---|---|---|---|---|
| POST | `/credits` | `CreditCreateDTO` (body) | `AssetDetailsDTO` | 400 on null or invalid |
| PUT | `/credits` | `CreditUpdateDTO` (body) | `AssetDetailsDTO` | 400 on null or invalid |
| DELETE | `/credits` | `CreditDeleteDTO` (body) | `AssetDetailsDTO` | 400 on null or invalid |
| GET | `/credits/broker/{brokerName}` | path param | `CreditDTO[]` | Credits by broker |
| GET | `/credits/portfolio/{brokerName}/{portfolioName}` | path params | `CreditDTO[]` | Credits by portfolio |

### Prices

| Method | Route | Request | Response | Notes |
|---|---|---|---|---|
| GET | `/prices/current` | `?exchange=&ticker=` (query) | `AssetPriceDTO` | 400 if params missing; scrapes Google Finance |

### Dividends

| Method | Route | Request | Response | Notes |
|---|---|---|---|---|
| GET | `/dividends/{ticker}/history` | `?exchange=` (optional, default: `BVMF`) | `DividendHistoryItemDTO[]` | Scrapes DadosMercado |
| GET | `/dividends/{ticker}/summary` | `?exchange=` (optional, default: `BVMF`) | `DividendSummaryDTO` | Dividend yield analysis |

### Diagnostics (root path — NOT under `/api/v1/financial`)

| Method | Route | Response | Notes |
|---|---|---|---|
| GET | `/health` | `{ status: "ok" }` | Always available |
| GET | `/config/repository` | Config object | Development environment only; 404 in Production |
