using System.Text.Json.Serialization;

namespace WEBFinanceApi;

public enum TransactionType { Income, Expense }

public class Transaction
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public Transaction() { }

    public Transaction(DateTime date, decimal amount, TransactionType type, string description = "")
    {
        if (amount < 0) throw new ArgumentException("Amount must be non-negative", nameof(amount));
        Id = Guid.NewGuid();
        Date = date;
        Amount = amount;
        Type = type;
        Description = description?.Trim() ?? string.Empty;
    }
}

public class Wallet
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; } = 0m;

    public List<Transaction> Transactions { get; set; } = new();

    public Wallet() { }

    public Wallet(string name, string currency, decimal initialBalance)
    {
        Id = Guid.NewGuid();
        Name = (name ?? string.Empty).Trim();
        Currency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        InitialBalance = initialBalance;
    }

    public decimal CurrentBalance =>
        InitialBalance
        + Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
        - Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

    public bool TryAddTransaction(Transaction tx, out string? error)
    {
        error = null;
        if (tx is null)
        {
            error = "Transaction is null";
            return false;
        }

        if (tx.Type == TransactionType.Expense && tx.Amount > CurrentBalance)
        {
            error = $"Недостаточно средств в кошельке '{Name}'. " +
                    $"Баланс: {CurrentBalance:F2} {Currency}, расход: {tx.Amount:F2} {Currency}";
            return false;
        }

        Transactions.Add(tx);
        return true;
    }

    public IEnumerable<Transaction> TransactionsInMonth(int year, int month) =>
        Transactions.Where(t => t.Date.Year == year && t.Date.Month == month);

    public (decimal income, decimal expense) MonthlySums(int year, int month)
    {
        var monthTx = TransactionsInMonth(year, month);
        var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return (income, expense);
    }
}
