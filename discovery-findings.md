# Financial Project - Comprehensive Codebase Analysis

## 1. SOLUTION STRUCTURE

**Solution File:** `E:\dev\Projetos\Financial\Financial.sln`

### Core Projects (Clean Architecture Layers)
- **Domain Layer:** `FinancialModel/Financial.Model.csproj` (.NET 10.0)
- **Application Layer:** `Financial.Application/Financial.Application.csproj` (.NET 10.0)
- **Infrastructure Layer:** `Financial.Infrastructure/Financial.Infrastructure.csproj` (.NET 10.0)
- **Common Library:** `Financial.Common/Financial.Common.csproj` (.NET 10.0)
- **UI Layer:** `FinancialUI/FinancialUI.csproj` (WPF/XAML)
- **Tools:** `FinanacialTools/FinanacialTools.csproj` (WPF)

### Testing & Specs
- **Unit Tests:** `Financial.Infrastructure.Tests/Financial.Infrastructure.Tests.csproj` (xUnit, FluentAssertions)
- **BDD Specs:** `Financial.Model.Specs/Financial.Model.Specs.csproj` (SpecFlow, xUnit)

### Support Libraries
- `GoogleFinancialSupport` - Google Sheets integration
- `WebPageParser` - Web scraping support
- `FinancialToolSupport` - Common tools
- `ImportGoogleSpreadSheets` - XAML UI for imports

---

## 2. DOMAIN MODEL (Core Entities & Value Objects)

### Root Entity: `Investments`
**File:** `E:\dev\Projetos\Financial\FinancialModel\Investments.cs`
```csharp
public class Investments
{
    private List<Broker> _brokers;
    public IReadOnlyCollection<Broker> Brokers { get; }
    public void AddBroker(Broker broker);
    public string Serialize();  // JSON serialization
    public static Investments Deserialize(string json);
}
```
- **Pattern:** Aggregate Root following Domain-Driven Design
- **Private Constructor:** Enforced via `[JsonConstructor]` for immutability
- **Factory Method:** `Create()` for construction

### Entity: `Broker`
**File:** `E:\dev\Projetos\Financial\FinancialModel\Broker.cs`
```csharp
public class Broker
{
    public string Name { get; }
    public string Currency { get; }
    public IReadOnlyCollection<Portifolio> Portifolios { get; }
    public Portifolio AddPortifolio(string name);
    public static Broker Create(string name, string currency);
}
```
- Multi-currency support (Brazil/UK)
- Encapsulated portfolio management

### Entity: `Portifolio`
**File:** `E:\dev\Projetos\Financial\FinancialModel\Portifolio.cs`
```csharp
public class Portifolio
{
    public string Name { get; }
    public IReadOnlyCollection<Asset> Assets { get; }
    public void AddAsset(Asset asset);
    internal static Portifolio Create(string name);  // Internal factory
}
```
- Acts as portfolio category/organization

### Entity: `Asset`
**File:** `E:\dev\Projetos\Financial\FinancialModel\Asset.cs`
```csharp
public class Asset
{
    public string Name { get; }
    public string ISIN { get; }
    public string Exchange { get; }
    public string Ticker { get; }
    public decimal AveragePrice { get; }  // Calculated
    public decimal Quantity { get; }      // Calculated
    public bool Active { get; }           // Computed property
    
    public IReadOnlyCollection<Operation> Operations { get; }
    public IReadOnlyCollection<Credit> Credits { get; }
    
    public void AddOperation(Operation operation);
    public void AddOperations(IEnumerable<Operation> operations);
    public void AddCredit(Credit credit);
    public void AddCredits(IEnumerable<Credit> credits);
    public static Asset Create(string name, string isin, string exchange, string ticker);
}
```
- **Business Logic:** Average price calculation on buy operations
- **Quantity Tracking:** Incremented on buy, decremented on sell
- **Asset Classes:** Bitcoin, REITs, shares, ETFs, bonds, ISAs (covered via ISIN/Ticker)

### Value Object: `Operation`
**File:** `E:\dev\Projetos\Financial\FinancialModel\Operation.cs`
```csharp
public class Operation
{
    public enum OperationType { Buy, Sell }
    
    public DateTime Date { get; }
    public OperationType Type { get; }
    public decimal Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal Fees { get; }
    
    public decimal TotalPrice => UnitPrice * Quantity + Fees;  // Computed
    
    public static Operation Create(DateTime date, OperationType type, 
                                   decimal quantity, decimal unitPrice, decimal fees);
}
```
- Immutable value object
- Total price calculated from unit price + fees

### Value Object: `Credit`
**File:** `E:\dev\Projetos\Financial\FinancialModel\Credit.cs`
```csharp
public class Credit
{
    public enum CreditType { Dividend, Rent }
    
    public DateTime Date { get; }
    public CreditType Type { get; }
    public decimal Value { get; }
    
    public static Credit Create(DateTime date, CreditType type, decimal value);
}
```
- Represents dividends and rental income
- Used for income tracking across investments

---

## 3. APPLICATION LAYER

### Repository Pattern
**File:** `E:\dev\Projetos\Financial\Financial.Application\IRepository.cs`

```csharp
public interface IRepository
{
    // Query methods
    List<string> GetAllAssetsFullName();
    IEnumerable<Asset> GetAssetsByBroker(string name);
    IEnumerable<Asset> GetAssetsByBrokerPortifolio(string broker, string portfolio);
    IEnumerable<Asset> GetAssetsByPortfolio(string name);
    IEnumerable<Asset> GetAssetsByAssetName(string name);
    IEnumerable<Broker> GetBrokerList();
    
    // DTO queries
    BrokerInfoDTO GetBrokerInfo(string brokerName);
    AssetInfoDTO GetAssetInfo(string brokerName, string portfolio, string assetName);
}
```
- **Pattern:** Repository Pattern for data access abstraction
- **No CQRS:** Direct DTOs from repository queries

### Data Transfer Objects (DTOs)

**AssetInfoDTO**
```csharp
public class AssetInfoDTO
{
    public string Ticker { get; set; }
    public string Exchange { get; set; }
    public decimal TotalBought { get; set; }
    public decimal TotalSold { get; set; }
    public CreditInfoDTO Credits { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentValue { get; set; }
    public Dictionary<DateOnly, decimal> InvestedHistory { get; set; }
}
```
File: `E:\dev\Projetos\Financial\Financial.Application\DTO\AssetInfoDTO.cs`

**BrokerInfoDTO**
```csharp
public class BrokerInfoDTO
{
    public decimal TotalBought { get; set; }
    public decimal TotalSold { get; set; }
    public CreditInfoDTO TotalCredits { get; set; }
    public decimal TotalBoughtActive { get; set; }
    public decimal TotalSoldActive { get; set; }
    public CreditInfoDTO TotalCreditsActive { get; set; }
    public List<PortfolioDTO> PortfoliosActive { get; set; }
    public List<PortfolioDTO> PortfoliosInactive { get; set; }
}
```
File: `E:\dev\Projetos\Financial\Financial.Application\DTO\BrokerInfoDTO.cs`

**CreditInfoDTO**
```csharp
public class CreditInfoDTO
{
    public decimal Total { get; set; }
    public Dictionary<DateOnly, decimal> CreditsByMonth { get; set; }
}
```
File: `E:\dev\Projetos\Financial\Financial.Application\DTO\CreditInfoDTO.cs`

**PortfolioDTO**
```csharp
public class PortfolioDTO
{
    public required string Name { get; set; }
    public required List<string> Assets { get; set; }
}
```
File: `E:\dev\Projetos\Financial\Financial.Application\DTO\PortfolioDTO.cs`

---

## 4. INFRASTRUCTURE LAYER

### JSONRepository Implementation
**File:** `E:\dev\Projetos\Financial\Financial.Infrastructure\JSONRepository.cs`

```csharp
public class JSONRepository : IRepository
{
    private Investments _investments;
    
    public JSONRepository()
    {
        _investments = LoadModel();  // Loads embedded JSON on initialization
    }
    
    // Query implementations using LINQ
    public List<string> GetAllAssetsFullName();
    public IEnumerable<Asset> GetAssetsByBroker(string name);
    // ... other methods
    
    public BrokerInfoDTO GetBrokerInfo(string brokerName);
    public AssetInfoDTO GetAssetInfo(string brokerName, string portfolio, string assetName);
    
    private Investments LoadModel()
    {
        var modelJson = LoadEmbeddedResource("Data.data.json");
        return Investments.Deserialize(modelJson);
    }
    
    static string LoadEmbeddedResource(string resourceName);  // Assembly reflection
}
```

**Key Features:**
- **Data Source:** Embedded JSON file (`E:\dev\Projetos\Financial\Financial.Infrastructure\Data\data.json`)
- **Initialization:** Loads on first instantiation (Lazy Loading pattern)
- **Query Methods:** Pure LINQ-to-Objects queries over loaded investments
- **DTO Mapping:** Repository handles aggregation and DTO construction
- **Visibility:** `[assembly: InternalsVisibleTo("Financial.Infrastructure.Tests")]` for testability

**Business Logic in Repository:**
- Calculate total bought/sold filtered by active/inactive assets
- Group credits by month using `DateOnly`
- Build invested history timeline from operations
- Aggregate portfolio asset lists

---

## 5. COMMON LIBRARY

### Custom JSON Resolver
**File:** `E:\dev\Projetos\Financial\Financial.Common\PrivateConstructorContractResolver.cs`

```csharp
public class PrivateConstructorContractResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);
        
        if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && 
            jsonTypeInfo.Type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length == 0)
        {
            // Allow deserialization of types with private constructors
            jsonTypeInfo.CreateObject = () =>
                Activator.CreateInstance(jsonTypeInfo.Type, true);
        }
        return jsonTypeInfo;
    }
}
```
- **Purpose:** Enables deserialization of entities with private constructors
- **SOLID:** Respects encapsulation while allowing JSON serialization
- **Usage:** Registered in `JsonSerializerOptions` within `Investments.Deserialize()`

### Value Objects
**File:** `E:\dev\Projetos\Financial\Financial.Common\FinancialDataWeb.cs`

```csharp
public enum DividendType { Dividend, JCP }
public record AssetValue(string Ticker, string Name, decimal Price);
public record DividendValue(DividendType Type, DateTime Date, decimal Value);
```
- Records for immutable data transfer from external sources
- JCP = Juros Sobre Capital Próprio (Brazilian tax-advantaged dividend equivalent)

---

## 6. DEPENDENCY INJECTION & ARCHITECTURE PATTERNS

### DI Pattern
- **No Explicit DI Container:** Classes use constructor-based initialization
- **Repository:** Singleton pattern via `JSONRepository()` initialization
- **Factory Methods:** All entities use static `Create()` methods
- **Assembly Visibility:** `[assembly: InternalsVisibleTo(...)]` for test isolation

### SOLID Principles Applied
- **Single Responsibility:** Each entity has focused business logic
- **Open/Closed:** Interface `IRepository` allows multiple implementations
- **Liskov Substitution:** DTOs replace entities without changing contracts
- **Interface Segregation:** `IRepository` contains only necessary methods
- **Dependency Inversion:** Applications depend on `IRepository` abstraction, not `JSONRepository`

### Design Patterns
1. **Repository Pattern:** `IRepository` abstracts data access
2. **Factory Pattern:** Static `Create()` methods on entities
3. **Aggregate Pattern:** `Investments` → `Broker` → `Portfolio` → `Asset` hierarchy
4. **Value Object Pattern:** `Operation`, `Credit` as immutable values
5. **DTO Pattern:** Separate transfer objects for application layer

---

## 7. TESTING PATTERNS & FRAMEWORKS

### Unit Testing
**Framework:** xUnit with FluentAssertions
**File:** `E:\dev\Projetos\Financial\Financial.Infrastructure.Tests\JSONRepositoryTests.cs`

```csharp
public class JSONRepositoryTests
{
    private readonly JSONRepository _sut = new JSONRepository();  // System Under Test
    
    [Fact]
    public void GetAllAssetsFullName_ShouldReturn_Values()
    {
        var result = _sut.GetAllAssetsFullName();
        result.Should().NotBeEmpty();
    }
    
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("NOTEXIST", 0)]
    [InlineData("FreeTrade", 14)]
    public void GetAssets_By_BrokerTest(string name, int records)
    {
        var result = _sut.GetAssetsByBroker(name);
        result.Should().HaveCount(records);
    }
}
```

**Test Characteristics:**
- **Data-Driven:** Theory attribute with InlineData for parameterized tests
- **Fluent Assertions:** Clear, readable assertions
- **Embedded Data:** Tests against actual `data.json` loaded by repository
- **Edge Cases:** Tests null, empty string, and non-existent values

**Test Project Configuration:**
- **Framework:** .NET 10.0
- **IsTestProject:** true
- **Dependencies:** 
  - xUnit 2.6.4
  - FluentAssertions 6.12.0
  - Microsoft.NET.Test.Sdk 17.8.0
  - xunit.runner.visualstudio 2.5.6
  - coverlet.collector 6.0.0

### BDD/Acceptance Testing
**Framework:** SpecFlow + xUnit
**Feature File:** `E:\dev\Projetos\Financial\Financial.Model.Specs\Features\Calculator.feature`

```gherkin
Feature: Calculator
Scenario: Add two numbers
    Given the first number is 50
    And the second number is 70
    When the two numbers are added
    Then the result should be 120
```

**Step Definitions:** `E:\dev\Projetos\Financial\Financial.Model.Specs\StepDefinitions\CalculatorStepDefinitions.cs`

```csharp
[Binding]
public sealed class CalculatorStepDefinitions
{
    private int _firstNumber;
    private int _secondNumber;
    private int _result;
    
    [Given("the first number is (.*)")]
    public void GivenTheFirstNumberIs(int number) => _firstNumber = number;
    
    [When("the two numbers are added")]
    public void WhenTheTwoNumbersAreAdded() => _result = _firstNumber + _secondNumber;
    
    [Then("the result should be (.*)")]
    public void ThenTheResultShouldBe(int expectedResult) 
        => _result.Should().Be(expectedResult);
}
```

**BDD Configuration:**
- SpecFlow.xUnit 3.9.40
- SpecFlow.Plus.LivingDocPlugin 3.9.57
- Living Documentation generation enabled

---

## 8. KEY LIBRARIES & FRAMEWORKS

| Layer | Purpose | Library | Version |
|-------|---------|---------|---------|
| Serialization | JSON | System.Text.Json | Built-in (.NET 10.0) |
| Domain Model | Language | C# | 12+ (latest) |
| Testing | Unit Tests | xUnit | 2.6.4 |
| Testing | Assertions | FluentAssertions | 6.12.0 |
| Testing | BDD/Specs | SpecFlow.xUnit | 3.9.40 |
| Testing | Coverage | coverlet.collector | 6.0.0 |
| UI | Windows Desktop | WPF/XAML | Built-in |
| External | Google Integration | Custom GoogleFinancialSupport | Internal |
| External | Web Scraping | WebPageParser | Internal |

---

## 9. PROJECT STRUCTURE VISUALIZATION

```
Financial (Solution)
│
├── FinancialModel/ (Domain Layer)
│   ├── Investments.cs (Aggregate Root)
│   ├── Broker.cs
│   ├── Portifolio.cs
│   ├── Asset.cs
│   ├── Operation.cs (Value Object)
│   ├── Credit.cs (Value Object)
│   └── Financial.Model.csproj
│
├── Financial.Application/ (Application Layer)
│   ├── IRepository.cs (Interface)
│   └── DTO/
│       ├── AssetInfoDTO.cs
│       ├── BrokerInfoDTO.cs
│       ├── CreditInfoDTO.cs
│       └── PortfolioDTO.cs
│
├── Financial.Infrastructure/ (Infrastructure Layer)
│   ├── JSONRepository.cs (Implementation)
│   ├── Data/
│   │   └── data.json (Embedded JSON)
│   └── Financial.Infrastructure.csproj
│
├── Financial.Common/ (Shared)
│   ├── PrivateConstructorContractResolver.cs
│   ├── FinancialDataWeb.cs (Records)
│   └── Financial.Common.csproj
│
├── Financial.Infrastructure.Tests/ (Unit Tests)
│   ├── JSONRepositoryTests.cs
│   └── Financial.Infrastructure.Tests.csproj
│
├── Financial.Model.Specs/ (BDD Tests)
│   ├── Features/
│   │   └── Calculator.feature
│   ├── StepDefinitions/
│   │   └── CalculatorStepDefinitions.cs
│   └── Financial.Model.Specs.csproj
│
├── FinancialUI/ (UI Layer)
├── FinanacialTools/ (Tools/UI)
├── GoogleFinancialSupport/ (Support)
├── WebPageParser/ (Support)
├── FinancialToolSupport/ (Support)
├── ImportGoogleSpreadSheets/ (Tools)
│
└── Financial.sln (Solution File)
```

---

## 10. DATA FLOW & ARCHITECTURE SUMMARY

### Request → Response Flow
```
UI Layer (WPF)
    ↓
JSONRepository (implements IRepository)
    ↓
Queries over loaded Investments model (LINQ-to-Objects)
    ↓
Repository constructs DTOs from domain entities
    ↓
UI displays results
```

### Persistence Strategy
- **Format:** JSON (embedded in assembly)
- **Serialization:** System.Text.Json with custom resolver
- **Deserialization:** Factory pattern via `Investments.Create()` + custom resolver
- **Update Strategy:** In-memory modifications + serialize back to JSON (implied)

---

## 11. NOTABLE ARCHITECTURAL DECISIONS

1. **Private Constructors with JSON Serialization:** All entities enforce encapsulation via private constructors, requiring custom `PrivateConstructorContractResolver` for deserialization. This is a clean approach to preventing invalid state creation.

2. **Embedded JSON Data:** Using an embedded resource means zero database dependency and easy distribution.

3. **Repository Without Save/Update:** Repository only provides query methods. Implies save functionality is elsewhere or entities are modified in-memory and serialized back.

4. **No Service Layer:** Business logic lives directly on entities (rich domain model) rather than anemic entities + services.

5. **ReadOnlyCollection Encapsulation:** All collections are exposed as `IReadOnlyCollection` with private setters, preventing external modification while allowing iteration.

6. **Value Objects for Immutability:** Operations and Credits are designed as immutable values with factory methods.

7. **Multi-Currency Support:** Broker holds currency field supporting Brazil (BRL) and UK (GBP) trades.

---

## 12. POTENTIAL IMPROVEMENTS & NOTES

### Code Quality
- Well-structured, follows SOLID principles
- Consistent factory method pattern across entities
- Strong encapsulation with private fields

### Testing
- Good unit test coverage on repository layer
- BDD features in place (currently basic calculator example)
- Could expand domain entity tests

### Scalability Considerations
- Current JSON-based storage limits to medium datasets
- Could migrate to EF Core + database without changing repository interface
- LINQ query patterns are database-agnostic

---

## Discovery Completed: ✅

**Phases Completed:**
- ✅ Phase 1: Initial Reconnaissance
- ✅ Phase 2: Deep Dive Analysis
- ✅ Phase 3: Pattern Extraction
- ✅ Phase 4: Documentation

**Total Classes/Interfaces Found:** 26
**Test Coverage:** Unit tests + BDD specs present
**Architecture Style:** Clean Architecture with Domain-Driven Design
