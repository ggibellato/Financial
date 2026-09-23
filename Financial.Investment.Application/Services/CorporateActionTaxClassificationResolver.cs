using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Services;

internal static class CorporateActionTaxClassificationResolver
{
    internal static TaxClassification? FindActive(Asset asset, Guid corporateActionId) =>
        asset.TaxClassifications.FirstOrDefault(c =>
            c.SourceType == SourceType.CorporateAction
            && c.SourceId == corporateActionId
            && c.Status == TaxClassificationStatus.Active);
}
