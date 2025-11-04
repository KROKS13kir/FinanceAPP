using System.Text;
using System.Text.Json;

namespace WEBFinanceApi;

public static class Repo
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "finance.json");
    private static readonly object _lock = new();

    public static List<Wallet> Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(FilePath)) return new();

                var txt = File.ReadAllText(FilePath, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<List<Wallet>>(txt, JsonOptions) ?? new();

                foreach (var w in data)
                {
                    w.Name = (w.Name ?? string.Empty).Trim();
                    w.Currency = (w.Currency ?? string.Empty).Trim().ToUpperInvariant();
                    if (w.Transactions is { Count: > 0 })
                    {
                        foreach (var t in w.Transactions)
                            t.Description = (t.Description ?? string.Empty).Trim();
                    }
                }

                return data;
            }
            catch
            {
                return new();
            }
        }
    }

    public static void Save(List<Wallet> wallets)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            foreach (var w in wallets)
            {
                w.Name = (w.Name ?? string.Empty).Trim();
                w.Currency = (w.Currency ?? string.Empty).Trim().ToUpperInvariant();
                if (w.Transactions is { Count: > 0 })
                {
                    foreach (var t in w.Transactions)
                        t.Description = (t.Description ?? string.Empty).Trim();
                }
            }

            var tmpPath = FilePath + ".tmp";
            var bakPath = FilePath + ".bak";
            var txt = JsonSerializer.Serialize(wallets, JsonOptions);

            File.WriteAllText(tmpPath, txt, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            try
            {
                if (File.Exists(FilePath))
                {
                    File.Replace(tmpPath, FilePath, bakPath, ignoreMetadataErrors: true);
                    try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { /* игнор */ }
                }
                else
                {
                    File.Move(tmpPath, FilePath);
                }
            }
            finally
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { /* игнор */ }
            }
        }
    }

    public static List<Wallet> GenerateSample()
    {
        var w1 = new Wallet("Наличные", "RUB", 5000m);
        var w2 = new Wallet("Карта зарплата", "RUB", 20000m);
        var w3 = new Wallet("USD счёт", "USD", 150m);

        var now = DateTime.Now;
        int y = now.Year, m = now.Month;

        _ = w1.TryAddTransaction(new Transaction(new DateTime(y, m, 2), 1000m, TransactionType.Income, "Подработка"), out _);
        _ = w1.TryAddTransaction(new Transaction(new DateTime(y, m, 3), 200m, TransactionType.Expense, "Кофе и перекус"), out _);
        _ = w1.TryAddTransaction(new Transaction(new DateTime(y, m, 10), 1200m, TransactionType.Expense, "Покупка одежды"), out _);
        _ = w1.TryAddTransaction(new Transaction(new DateTime(y, Math.Max(1, m - 1), 20), 500m, TransactionType.Expense, "Прошлый месяц трата"), out _);

        _ = w2.TryAddTransaction(new Transaction(new DateTime(y, m, 1), 50000m, TransactionType.Income, "Зарплата"), out _);
        _ = w2.TryAddTransaction(new Transaction(new DateTime(y, m, 5), 10000m, TransactionType.Expense, "Оплата аренды"), out _);
        _ = w2.TryAddTransaction(new Transaction(new DateTime(y, m, 20), 8000m, TransactionType.Expense, "Ремонт техники"), out _);
        _ = w2.TryAddTransaction(new Transaction(new DateTime(y, Math.Max(1, m - 2), 12), 1500m, TransactionType.Expense, "Давняя трата"), out _);

        _ = w3.TryAddTransaction(new Transaction(new DateTime(y, m, 3), 50m, TransactionType.Income, "Фриланс USD"), out _);
        _ = w3.TryAddTransaction(new Transaction(new DateTime(y, m, 12), 75m, TransactionType.Expense, "Покупка в магазине"), out _);

        foreach (var w in new[] { w1, w2, w3 })
            w.Currency = (w.Currency ?? string.Empty).Trim().ToUpperInvariant();

        return new List<Wallet> { w1, w2, w3 };
    }
}
