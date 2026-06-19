> Part of the `testing-guide-Financial` skill (see `../SKILL.md`).

# Infrastructure Services (`*Service.cs` in `Financial.Infrastructure/`)

## What to test

- **Add**: created entity appears in subsequent read result with correct field values
- **Update**: mutated fields are reflected in subsequent read; non-mutated fields are unchanged
- **Delete**: removed entity is absent from subsequent read; other entities are unaffected
- **Query**: reads return the expected subset of data given known test data
- **Error conditions**: operations on non-existent entities behave as specified (return null, throw, etc.)

## Layer assignment

**Integration via real temp file** — Infrastructure services coordinate the repository and domain layer. Use real implementations with a temporary copy of `TestData/data.test.json`. No mocking framework.

This is still considered "unit" for this project because the persistence layer is a local file with no external complexity.

## Setup pattern

```csharp
[Fact]
public async Task OperationName_Condition_ExpectedOutcome()
{
    var (service, tempFile) = CreateService();
    try
    {
        var request = new OperationCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            // ... other fields matching the seed data
        };

        var result = await service.DoOperationAsync(request);

        result.Should().NotBeNull();
        result!.SomeCollection.Should().ContainSingle(item =>
            item.Field == request.Field &&
            item.Id != Guid.Empty);
    }
    finally
    {
        File.Delete(tempFile);  // ALWAYS in finally — never in Assert block
    }
}

// Delete scenario — assert absence
[Fact]
public async Task RemoveItem_ItemIsAbsentAfterDeletion()
{
    var (service, tempFile) = CreateService();
    try
    {
        // Get a known ID from the seed data first
        var existing = await service.GetAsync(/* known broker/portfolio/asset */);
        var idToDelete = existing!.Items.First().Id;

        await service.RemoveAsync(new RemoveDTO { Id = idToDelete, /* locators */ });

        var updated = await service.GetAsync(/* same locators */);
        updated!.Items.Should().NotContain(item => item.Id == idToDelete);
    }
    finally
    {
        File.Delete(tempFile);
    }
}

// Factory method — copy this pattern for each service under test
private static (SomeService Service, string TempFile) CreateService()
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
    File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

    var storage = new LocalJsonStorage(tempFile);
    var serializer = new InvestmentsSerializerAdapter();
    var repository = new JSONRepository(
        InvestmentsLoader.LoadSync(storage, serializer),
        storage,
        serializer);
    var navigationService = new NavigationService(repository);
    var service = new SomeService(repository, navigationService);

    return (service, tempFile);
}
```

**Why `finally`**: if the assertion throws, code after the assertion is skipped. `finally` runs regardless, preventing temp file accumulation. See `references/gotchas.md`.

## When to skip

- Service methods that are pure delegation to a single repository call with no branching or domain logic (tested by the repository tests)

## Examples from project

| Service | Operations to test |
|---|---|
| `CreditService` | AddCredit (appears in result, Id not empty), UpdateCredit (fields updated), RemoveCredit (absent after delete) |
| `NavigationService` | GetNavigationTree returns hierarchy with expected brokers/portfolios/assets |
