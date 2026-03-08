namespace FinancialModel.Application;

public interface IRepositoryFactory
{
    IRepository Create(RepositorySelectionOptions options);
}
