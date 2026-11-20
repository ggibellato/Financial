using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Validation;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

[Trait("Category", "Unit")]
public class EntityLookupExtensionsTests
{
    private sealed record Item(Guid Id, string Name);

    private static readonly Item Barclays = new(Guid.NewGuid(), "Barclays");
    private static readonly Item Chase = new(Guid.NewGuid(), "Chase");
    private static readonly Item[] Items = [Barclays, Chase];

    [Fact]
    public void FirstOrThrow_MatchingItem_IsReturned()
    {
        Items.FirstOrThrow(item => item.Id == Chase.Id, "Bank", Chase.Id).Should().Be(Chase);
    }

    [Fact]
    public void FirstOrThrow_NoMatch_ThrowsNamingTheEntityAndId()
    {
        var id = Guid.NewGuid();

        var act = () => Items.FirstOrThrow(item => item.Id == id, "Bank", id);

        act.Should().Throw<KeyNotFoundException>().WithMessage($"Bank '{id}' was not found.");
    }

    [Fact]
    public void EnsureNameIsUnique_SameNameInAnotherCase_IsRejected()
    {
        var act = () => Items.EnsureNameIsUnique("BARCLAYS", null, item => item.Name, item => item.Id, "A bank");

        act.Should().Throw<DuplicateNameException>().WithMessage("A bank named \"BARCLAYS\" already exists.");
    }

    [Fact]
    public void EnsureNameIsUnique_TheItemBeingRenamed_IsNotACollision()
    {
        var act = () => Items.EnsureNameIsUnique("Barclays", Barclays.Id, item => item.Name, item => item.Id, "A bank");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAccountNameIsUnique_NameWithSurroundingSpaces_IsRejectedAsDuplicate()
    {
        var accounts = new[] { TestAccounts.Bank("Barclays", roundUpEnabled: false) };

        var act = () => accounts.EnsureAccountNameIsUnique("  Barclays  ", null, "A bank");

        act.Should().Throw<DuplicateNameException>().WithMessage("A bank named \"Barclays\" already exists.");
    }
}
