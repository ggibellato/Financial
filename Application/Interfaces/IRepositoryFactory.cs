namespace Financial.Application.Interfaces;

public interface IRepositoryFactory
{
    IRepository Create(RepositorySelectionOptions options);
}
