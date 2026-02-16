# Discovery Findings - Task 3: Make data.json configurable

**Date**: 2026-02-16
**Analyzed by**: Codebase Analyzer Subagent

## Summary
Data loading currently relies on embedded `Data\data.json` inside `Financial.Infrastructure`, and `JSONRepository` reads it as an assembly resource at construction time. Both WPF apps register `JSONRepository` via DI using `Host.CreateDefaultBuilder`, but no environment-variable or appsettings-based configuration is present. `GoogleGenerator` writes a `data.json` file to a caller-provided path, which is the only existing pattern for externalizing the file.

## 1. Similar Components Found
- `E:\dev\Projetos\Financial\Financial.Infrastructure\JSONRepository.cs` (lines 160-173): Loads `Data.data.json` as an embedded resource via `Assembly.GetExecutingAssembly()`.
- `E:\dev\Projetos\Financial\Financial.Infrastructure\Financial.Infrastructure.csproj` (lines 9-16): Marks `Data\data.json` as an `EmbeddedResource` and copies to output.
- `E:\dev\Projetos\Financial\GoogleFinancialSupport\GoogleGenerator.cs` (lines 45-49, 165-169): Accepts an output path in the constructor and writes `data.json` to disk.
- `E:\dev\Projetos\Financial\FinancialUI\App.xaml.cs` (lines 16-28) and `E:\dev\Projetos\Financial\FinanacialTools\App.xaml.cs` (lines 19-28): DI registration for `IRepository`/`JSONRepository` using `Host.CreateDefaultBuilder()`.

## 2. API Integration Pattern
- `GoogleFinancialSupport` uses an injected service (`GoogleService`) with async methods and progress reporting.
- Example: `E:\dev\Projetos\Financial\GoogleFinancialSupport\GoogleGenerator.cs` (lines 47-76) uses `await _service.GetFilesNameAsync()` and `IProgress<string>` to orchestrate data collection.

## 3. Navigation Pattern
- Navigation is domain/data navigation rather than UI routing. `NavigationService` builds a hierarchical `TreeNodeDTO` from the repository.
- Example: `E:\dev\Projetos\Financial\Financial.Infrastructure\NavigationService.cs` (lines 23-92) creates a root node and broker/portfolio/asset children based on repository data.

## 4. State Management Approach
- Repository is singleton-scoped and loads all data into memory on construction (`JSONRepository` keeps `_investiments`).
- WPF apps use MVVM view models registered via DI (e.g., `MainNavigationViewModel` in `FinancialUI\App.xaml.cs` lines 23-28).

## 5. UI Components to Reuse
- WPF apps already use `Host.CreateDefaultBuilder()` with DI registration (`FinancialUI\App.xaml.cs` lines 16-28; `FinanacialTools\App.xaml.cs` lines 19-28), which is the standard integration point for configuration in this codebase.

## 6. Test Patterns
- Tests use xUnit (`[Fact]`, `[Theory]`) and FluentAssertions.
- Example: `E:\dev\Projetos\Financial\Financial.Infrastructure.Tests\JSONRepositoryTests.cs` (lines 6-47) directly instantiates `JSONRepository` and asserts results without DI.

## 7. Code Examples
### Example 1: Embedded resource loading in repository
**Path**: `E:\dev\Projetos\Financial\Financial.Infrastructure\JSONRepository.cs` (lines 160-173)
```csharp
    private Investments LoadModel()
    {
        var modelJson = LoadEmbeddedResource("Data.data.json");
        return Investments.Deserialize(modelJson);
    }

    static string LoadEmbeddedResource(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string fullResourceName = $"{assembly.GetName().Name}.{resourceName}";

        using Stream stream = assembly.GetManifestResourceStream(fullResourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
```

### Example 2: `data.json` embedded in project
**Path**: `E:\dev\Projetos\Financial\Financial.Infrastructure\Financial.Infrastructure.csproj` (lines 9-20)
```xml
  <ItemGroup>
    <None Remove="Data\data.json" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Data\data.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </EmbeddedResource>
  </ItemGroup>

  <ItemGroup>
```

### Example 3: File system output for `data.json`
**Path**: `E:\dev\Projetos\Financial\GoogleFinancialSupport\GoogleGenerator.cs` (lines 165-176)
```csharp
    private void Save(Investments data)
    {
        string json = data.Serialize();
        File.WriteAllText(Path.Combine(_path, "data.json"), json);
    }

    private async Task<List<Operation>> CreateOperationsAsync(string id, string spreadSheetName)
    {
        var operations = new List<Operation>();
        // Use open-ended range to get all rows with data dynamically
        var values = await _service.GetSpreadSheetDataAsync(id, $"{spreadSheetName}!A3:G");
        var previousDate = 0L;
```

## 8. Recommendations
- Use the existing `Host.CreateDefaultBuilder()` DI setup in `FinancialUI\App.xaml.cs` and `FinanacialTools\App.xaml.cs` to access `context.Configuration` when registering the repository, since this is the current configuration entry point.
- Keep `JSONRepository` as the `IRepository` implementation but inject a configurable path (constructor parameter or options) so tests can still instantiate it explicitly (similar to how tests currently construct `JSONRepository` directly).
- Replace the current embedded resource setup in `Financial.Infrastructure.csproj` with external file loading, since the embedded resource pattern is the main hard-coded dependency.
- Align any file-writing path behavior with the existing `GoogleGenerator` pattern (caller-provided directory, `Path.Combine(_path, "data.json")`) to keep file handling consistent.
