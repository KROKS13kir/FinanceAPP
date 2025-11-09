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

                bool changed = false;
                foreach (var w in data)
                {
                    if (w.Id == Guid.Empty) { w.Id = Guid.NewGuid(); changed = true; }
                    w.Name = (w.Name ?? string.Empty).Trim();
                    w.Currency = (w.Currency ?? "RUB").Trim().ToUpperInvariant();

                    if (w.Transactions != null)
                    {
                        foreach (var t in w.Transactions)
                        {
                            if (t.Id == Guid.Empty) { t.Id = Guid.NewGuid(); changed = true; }
                            t.Description = (t.Description ?? string.Empty).Trim();
                        }
                    }
                }
                if (changed) Save(data);
                return data;
            }
            catch { return new(); }
        }
    }

    public static void Save(List<Wallet> wallets)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var tmpPath = FilePath + ".tmp";
            var bakPath = FilePath + ".bak";
            var txt = JsonSerializer.Serialize(wallets, JsonOptions);

            File.WriteAllText(tmpPath, txt, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            try
            {
                if (File.Exists(FilePath))
                {
                    File.Replace(tmpPath, FilePath, bakPath, ignoreMetadataErrors: true);
                    try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { }
                }
                else
                {
                    File.Move(tmpPath, FilePath);
                }
            }
            finally
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
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

        w1.AddTransaction(Transaction.CreateIncome(new DateTime(y, m, 2), 1000m, "Подработка"));
        w1.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 3), 200m, "Кофе и перекус"));
        w1.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 10), 1200m, "Покупка одежды"));
        w1.AddTransaction(Transaction.CreateExpense(new DateTime(y, Math.Max(1, m - 1), 20), 500m, "Прошлый месяц трата"));

        w2.AddTransaction(Transaction.CreateIncome(new DateTime(y, m, 1), 50000m, "Зарплата"));
        w2.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 5), 10000m, "Оплата аренды"));
        w2.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 20), 8000m, "Ремонт техники"));
        w2.AddTransaction(Transaction.CreateExpense(new DateTime(y, Math.Max(1, m - 2), 12), 1500m, "Давняя трата"));

        w3.AddTransaction(Transaction.CreateIncome(new DateTime(y, m, 3), 50m, "Фриланс USD"));
        w3.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 12), 75m, "Покупка в магазине"));

        return new List<Wallet> { w1, w2, w3 };
    }
}
