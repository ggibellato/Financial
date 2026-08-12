using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

public sealed class CategoryReferenceConverter(Dictionary<Guid, Category>? lookup)
    : ReferenceConverter<Category>(lookup, category => category.Id, "Category");
