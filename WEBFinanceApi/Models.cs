namespace WEBFinanceApi;

public enum TransactionType { Income, Expense }

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
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
        Description = (description ?? string.Empty).Trim();
    }

    public static Transaction CreateIncome(DateTime date, decimal amount, string? description = "") =>
        new(date, amount, TransactionType.Income, description ?? "");

    public static Transaction CreateExpense(DateTime date, decimal amount, string? description = "") =>
        new(date, amount, TransactionType.Expense, description ?? "");
}

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "RUB";
    public decimal InitialBalance { get; set; }
    public List<Transaction> Transactions { get; set; } = new();

    public Wallet() { }

    public Wallet(string name, string currency, decimal initialBalance)
    {
        Id = Guid.NewGuid();
        Name = (name ?? string.Empty).Trim();
        Currency = (currency ?? "RUB").Trim().ToUpperInvariant();
        InitialBalance = initialBalance;
    }

    public decimal CurrentBalance =>
        InitialBalance
        + Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
        - Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

    public void AddTransaction(Transaction tx)
    {
        if (tx.Type == TransactionType.Expense && tx.Amount > CurrentBalance)
            throw new InvalidOperationException($"Недостаточно средств в '{Name}'. Баланс {CurrentBalance:F2} {Currency}, расход {tx.Amount:F2} {Currency}");
        Transactions.Add(tx);
    }

    public IEnumerable<Transaction> TransactionsInMonth(int year, int month) =>
        Transactions.Where(t => t.Date.Year == year && t.Date.Month == month);
}
