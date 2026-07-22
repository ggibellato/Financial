using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Services;

public sealed class CardStatementService : ICardStatementService
{
    private static readonly CreditCard[] AllCards = Enum.GetValues<CreditCard>();

    private readonly ICashFlowRepository _repository;

    public CardStatementService(ICashFlowRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<CardStatementDTO>> GetStatementsForMonthAsync(int year, int month)
    {
        var existingStatements = _repository.GetCardStatements()
            .Where(s => s.Year == year && s.Month == month)
            .ToList();

        var created = false;
        foreach (var card in AllCards)
        {
            if (existingStatements.Any(s => s.Card == card))
            {
                continue;
            }

            var statement = CardStatement.Create(card, year, month);
            _repository.AddCardStatement(statement);
            existingStatements.Add(statement);
            created = true;
        }

        if (created)
        {
            await _repository.SaveChangesAsync().ConfigureAwait(false);
        }

        return existingStatements.Select(ToDto).ToList();
    }

    public async Task<CardStatementDTO> MarkStatementPaidAsync(Guid id)
    {
        var statement = _repository.GetCardStatements().FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Card statement '{id}' was not found.");

        if (statement.IsPaid)
        {
            return ToDto(statement);
        }

        statement.MarkPaid();

        try
        {
            await _repository.SaveChangesAsync().ConfigureAwait(false);
        }
        catch
        {
            statement.MarkUnpaid();
            throw;
        }

        return ToDto(statement);
    }

    private CardStatementDTO ToDto(CardStatement statement)
    {
        var outstandingTotal = statement.IsPaid ? 0m : _repository.GetExpenses()
            .Where(e => e.CardTag == statement.Card && e.Date.Year == statement.Year && e.Date.Month == statement.Month)
            .Sum(e => e.Value);

        return new CardStatementDTO
        {
            Id = statement.Id,
            Card = statement.Card.ToString(),
            Year = statement.Year,
            Month = statement.Month,
            IsPaid = statement.IsPaid,
            OutstandingTotal = outstandingTotal
        };
    }
}
