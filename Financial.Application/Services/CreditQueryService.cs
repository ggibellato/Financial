using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Financial.Application.Services;

public sealed class CreditQueryService : ICreditQueryService
{
    private readonly IRepository _repository;

    public CreditQueryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return Array.Empty<CreditDTO>();

        return _repository.GetAssetsByBroker(brokerName)
            .Where(asset => asset.Active)
            .SelectMany(asset => asset.Credits)
            .Select(NavigationMapper.MapCredit)
            .OrderByDescending(credit => credit.Date)
            .ToList();
    }

    public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return Array.Empty<CreditDTO>();

        return _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName)
            .Where(asset => asset.Active)
            .SelectMany(asset => asset.Credits)
            .Select(NavigationMapper.MapCredit)
            .OrderByDescending(credit => credit.Date)
            .ToList();
    }
}
