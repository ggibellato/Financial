using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Application.Services;

public sealed class ReportingCurrencySettingService : IReportingCurrencyProvider
{
    private readonly IInvestmentRepository _repository;

    public ReportingCurrencySettingService(IInvestmentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Currency GetReportingCurrency() => _repository.GetInvestments().ReportingCurrency;

    public Task SetReportingCurrencyAsync(Currency currency) =>
        _repository.ApplyAndSaveAsync(() =>
        {
            _repository.GetInvestments().SetReportingCurrency(currency);
            return true;
        });

    public bool IsReportingCurrencyEnabled() => _repository.GetInvestments().ReportingCurrencyEnabled;

    public Task SetReportingCurrencyEnabledAsync(bool enabled) =>
        _repository.ApplyAndSaveAsync(() =>
        {
            _repository.GetInvestments().SetReportingCurrencyEnabled(enabled);
            return true;
        });
}
