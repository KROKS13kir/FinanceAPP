using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Finance.Domain.Abstractions;
using Finance.Domain.Enums;
using Finance.Domain.Errors;

namespace Finance.Domain.Entities;

internal sealed class Wallet : Entity
{
    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    [JsonInclude]
    public string Currency { get; private set; } = string.Empty;

    [JsonInclude]
    public decimal InitialBalance { get; private set; }

    private readonly List<Transaction> _transactions = new();

    [JsonIgnore]
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    [JsonInclude]
    [JsonPropertyName("Transactions")]
    public List<Transaction> TransactionsMutable
    {
        get => _transactions;
        private set
        {
            _transactions.Clear();
            if (value != null) _transactions.AddRange(value);
        }
    }

    public Wallet() { }

    private Wallet(string name, string currency, decimal initialBalance)
    {
        Name = name ?? string.Empty;
        Currency = currency ?? string.Empty;
        InitialBalance = initialBalance;
    }

    public static Wallet Create(string name, string currency, decimal initialBalance) =>
        new(name, currency, initialBalance);

    public decimal CurrentBalance =>
        InitialBalance
        + _transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
        - _transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

    public void AddTransaction(Transaction tx)
    {
        if (tx.Type == TransactionType.Expense && tx.Amount > CurrentBalance)
            throw new DomainException($"Недостаточно средств в '{Name}' ({Currency}). Баланс: {CurrentBalance:F2}, расход: {tx.Amount:F2}");

        _transactions.Add(tx);
    }

    public IEnumerable<Transaction> TransactionsInMonth(int year, int month) =>
        _transactions.Where(t => t.Date.Year == year && t.Date.Month == month);

    public (decimal income, decimal expense) MonthlySums(int year, int month)
    {
        var monthTx = TransactionsInMonth(year, month);
        var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return (income, expense);
    }

    public override string ToString() =>
        $"{Name} ({Currency}) — начальный: {InitialBalance:F2}, текущий: {CurrentBalance:F2}";
}
