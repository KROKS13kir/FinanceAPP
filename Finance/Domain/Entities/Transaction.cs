using System;
using System.Text.Json.Serialization;
using Finance.Domain.Abstractions;
using Finance.Domain.Enums;
using Finance.Domain.Errors;

namespace Finance.Domain.Entities;

internal sealed class Transaction : Entity
{
    [JsonInclude]
    public DateTime Date { get; private set; }

    [JsonInclude]
    public decimal Amount { get; private set; }

    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionType Type { get; private set; }

    [JsonInclude]
    public string Description { get; private set; } = string.Empty;

    public Transaction() { }

    private Transaction(DateTime date, decimal amount, TransactionType type, string? description)
    {
        if (amount < 0m)
            throw new DomainException("Сумма должна быть неотрицательной.");

        Date = date;
        Amount = amount;
        Type = type;
        Description = description ?? string.Empty;
    }

    public static Transaction CreateIncome(DateTime date, decimal amount, string? description = "") =>
        new(date, amount, TransactionType.Income, description);

    public static Transaction CreateExpense(DateTime date, decimal amount, string? description = "") =>
        new(date, amount, TransactionType.Expense, description);

    public override string ToString()
    {
        var sign = Type == TransactionType.Income ? "+" : "-";
        return $"{Date:yyyy-MM-dd} {sign}{Amount:F2} ({Type}) {Description}";
    }
}
