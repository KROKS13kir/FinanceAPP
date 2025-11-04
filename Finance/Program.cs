#nullable enable

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finance;

internal enum TransactionType
{
    Income,
    Expense
}

internal class Transaction
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public decimal Amount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionType Type { get; set; }

    public string Description { get; set; } = string.Empty;

    public Transaction()
    {
    }

    public Transaction(DateTime date, decimal amount, TransactionType type, string description = "")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть неотрицательным");
        }

        Id = Guid.NewGuid();
        Date = date;
        Amount = amount;
        Type = type;
        Description = description ?? string.Empty;
    }

    public override string ToString()
    {
        var sign = Type == TransactionType.Income ? "+" : "-";
        return $"{Date:yyyy-MM-dd} {sign}{Amount:F2} ({Type}) {Description}";
    }
}

internal class Wallet
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; }

    public List<Transaction> Transactions { get; set; } = new();

    public Wallet()
    {
    }

    public Wallet(string name, string currency, decimal initialBalance)
    {
        Id = Guid.NewGuid();
        Name = name ?? string.Empty;
        Currency = currency ?? string.Empty;
        InitialBalance = initialBalance;
    }

    public decimal CurrentBalance =>
        InitialBalance
        + Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount)
        - Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

    public bool TryAddTransaction(Transaction tx, out string? error)
    {
        error = null;

        if (tx.Type == TransactionType.Expense && tx.Amount > CurrentBalance)
        {
            error =
                $"Недостаточно средств в кошельке '{Name}'. Текущий баланс: {CurrentBalance:F2} {Currency}, попытка расхода: {tx.Amount:F2} {Currency}";
            return false;
        }

        Transactions.Add(tx);
        return true;
    }

    public IEnumerable<Transaction> TransactionsInMonth(int year, int month)
    {
        return Transactions.Where(t => t.Date.Year == year && t.Date.Month == month);
    }

    public (decimal income, decimal expense) MonthlySums(int year, int month)
    {
        var monthTx = TransactionsInMonth(year, month);
        var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return (income, expense);
    }

    public override string ToString()
    {
        return $"{Name} ({Currency}) — начальный: {InitialBalance:F2}, текущий: {CurrentBalance:F2}";
    }
}

internal static class Program
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Учёт личных финансов — консольное приложение\n");

        var wallets = SelectDataSource();

        while (true)
        {
            Console.WriteLine("\nДоступные действия:");
            Console.WriteLine("1) Показать список кошельков");
            Console.WriteLine("2) Добавить транзакцию");
            Console.WriteLine("3) Сохранить данные в JSON-файл");
            Console.WriteLine("4) Построить отчёт за месяц");
            Console.WriteLine("5) Загрузить другие данные (начать заново)");
            Console.WriteLine("6) Выйти");
            Console.Write("Выберите действие: ");

            var act = Console.ReadLine()?.Trim();

            switch (act)
            {
                case "1":
                    PrintWallets(wallets);
                    break;

                case "2":
                    AddTransactionInteractive(wallets);
                    break;

                case "3":
                    SaveToFileInteractive(wallets);
                    break;

                case "4":
                    BuildMonthlyReport(wallets);
                    break;

                case "5":
                    wallets = SelectDataSource();
                    break;

                case "6":
                    Console.WriteLine("Возвращайтесь поскорее.");
                    return;

                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }
    }

    private static List<Wallet> SelectDataSource()
    {
        while (true)
        {
            Console.WriteLine("Выберите источник данных:");
            Console.WriteLine("1) Сгенерировать пример данных");
            Console.WriteLine("2) Загрузить из JSON-файла");
            Console.WriteLine("3) Ввод вручную (создать кошелёк и транзакции)");
            Console.WriteLine("4) Выйти");
            Console.Write("Введите номер варианта: ");

            var choice = Console.ReadLine()?.Trim();

            if (choice == "1")
            {
                var wallets = GenerateSampleData();
                Console.WriteLine("Данные сгенерированы.\n");
                return wallets;
            }

            if (choice == "2")
            {
                Console.Write("Путь к JSON-файлу: ");
                var path = Console.ReadLine() ?? string.Empty;

                if (!File.Exists(path))
                {
                    Console.WriteLine("Файл не найден.\n");
                    continue;
                }

                try
                {
                    var txt = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<List<Wallet>>(txt, s_jsonOptions);
                    if (loaded is not null)
                    {
                        Console.WriteLine("Данные загружены из файла.\n");
                        return loaded;
                    }

                    Console.WriteLine("Файл пуст или имеет неверный формат.\n");
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"Файл не найден: {ex.Message}\n");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Нет доступа к файлу: {ex.Message}\n");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Ошибка ввода/вывода: {ex.Message}\n");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Ошибка формата JSON: {ex.Message}\n");
                }

                continue;
            }

            if (choice == "3")
            {
                var wallets = ManualInput();
                Console.WriteLine("Данные введены вручную.\n");
                return wallets;
            }

            if (choice == "4")
            {
                Console.WriteLine("Выход.");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Неверный выбор. Попробуйте снова.\n");
            }
        }
    }

    private static List<Wallet> GenerateSampleData()
    {
        var w1 = new Wallet("Наличные", "RUB", 5000m);
        var w2 = new Wallet("Карта зарплата", "RUB", 20000m);
        var w3 = new Wallet("USD счёт", "USD", 150m);

        var now = DateTime.Now;
        var y = now.Year;
        var m = now.Month;

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

        return new List<Wallet> { w1, w2, w3 };
    }

    private static List<Wallet> ManualInput()
    {
        var wallets = new List<Wallet>();
        Console.WriteLine("Создаём кошельки вручную. Введите пустое имя для завершения.");

        while (true)
        {
            Console.Write("Название кошелька (пусто — готово): ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                break;
            }

            Console.Write("Валюта (например RUB, USD): ");
            var currency = (Console.ReadLine() ?? "RUB").Trim();

            var initial = ReadDecimal("Начальный баланс: ");

            var w = new Wallet(name, currency, initial);
            wallets.Add(w);

            Console.WriteLine("Добавим несколько транзакций для этого кошелька. Введите пустую дату, чтобы перейти к следующему кошельку.");
            while (true)
            {
                Console.Write("Дата (YYYY-MM-DD, пусто — закончить): ");
                var sdate = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(sdate))
                {
                    break;
                }

                if (!DateTime.TryParseExact(sdate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    Console.WriteLine("Неверный формат даты.");
                    continue;
                }

                Console.Write("Тип (Income/Expense): ");
                var stype = Console.ReadLine()?.Trim();
                if (!Enum.TryParse(stype, true, out TransactionType type))
                {
                    Console.WriteLine("Неверный тип. Попробуйте снова.");
                    continue;
                }

                var amount = ReadDecimal("Сумма: ");
                Console.Write("Описание (опционально): ");
                var desc = Console.ReadLine() ?? string.Empty;

                var tx = new Transaction(date, amount, type, desc);
                if (!w.TryAddTransaction(tx, out var err))
                {
                    Console.WriteLine($"Ошибка: {err}");
                }
                else
                {
                    Console.WriteLine("Транзакция добавлена.");
                }
            }
        }

        return wallets;
    }

    private static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            var s = Console.ReadLine();

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
            {
                return val;
            }

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out val))
            {
                return val;
            }

            Console.WriteLine("Неверная сумма. Используйте цифры, разделитель — точка или запятая.");
        }
    }

    private static void PrintWallets(List<Wallet> wallets)
    {
        if (!wallets.Any())
        {
            Console.WriteLine("Кошельков нет.");
            return;
        }

        Console.WriteLine("\nСписок кошельков:");
        for (var i = 0; i < wallets.Count; i++)
        {
            var w = wallets[i];
            Console.WriteLine($"{i + 1}. {w} (ID: {w.Id})");

            if (w.Transactions.Any())
            {
                Console.WriteLine($"   Транзакции ({w.Transactions.Count}):");
                foreach (var t in w.Transactions.OrderBy(t => t.Date))
                {
                    Console.WriteLine($"     {t.Date:yyyy-MM-dd} {t.Type} {t.Amount:F2} — {t.Description}");
                }
            }
        }
    }

    private static void AddTransactionInteractive(List<Wallet> wallets)
    {
        if (!wallets.Any())
        {
            Console.WriteLine("Нет кошельков. Добавьте кошелёк сначала.");
            return;
        }

        Console.WriteLine("Выберите кошелёк:");
        for (var i = 0; i < wallets.Count; i++)
        {
            Console.WriteLine($"{i + 1}) {wallets[i].Name} ({wallets[i].Currency}), баланс: {wallets[i].CurrentBalance:F2}");
        }

        Console.Write("Номер кошелька: ");
        if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 1 || idx > wallets.Count)
        {
            Console.WriteLine("Неверный номер.");
            return;
        }

        var w = wallets[idx - 1];

        Console.Write("Дата (YYYY-MM-DD), пусто = сегодня: ");
        var sdate = Console.ReadLine()?.Trim();
        DateTime date;

        if (string.IsNullOrEmpty(sdate))
        {
            date = DateTime.Today;
        }
        else if (!DateTime.TryParseExact(sdate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            Console.WriteLine("Неверный формат даты.");
            return;
        }

        Console.Write("Тип (Приход/Расход): ");
        var stype = Console.ReadLine()?.Trim();
        if (!Enum.TryParse(stype, true, out TransactionType type))
        {
            Console.WriteLine("Неверный тип.");
            return;
        }

        var amount = ReadDecimal("Сумма: ");
        Console.Write("Описание: ");
        var desc = Console.ReadLine() ?? string.Empty;

        var tx = new Transaction(date, amount, type, desc);
        if (!w.TryAddTransaction(tx, out var err))
        {
            Console.WriteLine($"Не удалось добавить транзакцию: {err}");
        }
        else
        {
            Console.WriteLine("Транзакция успешно добавлена.");
        }
    }

    private static void SaveToFileInteractive(List<Wallet> wallets)
    {
        Console.Write("Путь для сохранения (например /Users/you/finance.json): ");
        var path = Console.ReadLine() ?? string.Empty;

        try
        {
            var txt = JsonSerializer.Serialize(wallets, s_jsonOptions);
            File.WriteAllText(path, txt);
            Console.WriteLine("Данные сохранены.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Нет доступа к файлу: {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"Каталог не найден: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка ввода/вывода: {ex.Message}");
        }
    }

    private static void BuildMonthlyReport(List<Wallet> wallets)
    {
        if (!wallets.Any())
        {
            Console.WriteLine("Нет данных для отчёта.");
            return;
        }

        Console.Write("Введите год и месяц в формате YYYY-MM (например 2025-11), пусто — текущий месяц: ");
        var input = Console.ReadLine();

        int year, month;
        if (string.IsNullOrWhiteSpace(input))
        {
            var now = DateTime.Now;
            year = now.Year;
            month = now.Month;
        }
        else
        {
            var parts = input.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2
                || !int.TryParse(parts[0], out year)
                || !int.TryParse(parts[1], out month)
                || month < 1 || month > 12)
            {
                Console.WriteLine("Неверный формат. Ожидается YYYY-MM.");
                return;
            }
        }

        Console.Write("Фильтр по валюте (например RUB, USD). Пусто — все валюты: ");
        var currencyFilter = (Console.ReadLine() ?? string.Empty).Trim();
        var hasFilter = !string.IsNullOrEmpty(currencyFilter);

        Console.WriteLine($"\nОтчёт за {year}-{month:00}{(hasFilter ? $" | Валюта: {currencyFilter}" : "")}\n");

        // Транзакции за месяц
        var all = wallets
            .SelectMany(w => w.TransactionsInMonth(year, month)
                .Select(t => new { Wallet = w, Transaction = t }))
            .Where(x => !hasFilter || x.Wallet.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!all.Any())
        {
            Console.WriteLine("За указанный месяц транзакций нет (с учётом фильтра валюты).");
            return;
        }

        var groups = all
            .GroupBy(x => new { x.Wallet.Currency, x.Transaction.Type })
            .Select(g => new
            {
                g.Key.Currency,
                g.Key.Type,
                Total = g.Sum(x => x.Transaction.Amount),
                Items = g.OrderBy(x => x.Transaction.Date).ToList()
            })

            .OrderBy(g => g.Currency)
            .ThenByDescending(g => g.Total)
            .ToList();

        Console.WriteLine("1) Транзакции, сгруппированные по валюте и типу (внутри — по дате ↑):\n");

        string? currentCurrency = null;
        foreach (var g in groups)
        {
            if (currentCurrency != g.Currency)
            {
                currentCurrency = g.Currency;
                Console.WriteLine($"=== Валюта: {currentCurrency} ===");
            }
            Console.WriteLine($"--- {g.Type} | Общая сумма: {g.Total:F2} {g.Currency}");
            foreach (var it in g.Items)
            {
                Console.WriteLine($"{it.Transaction.Date:yyyy-MM-dd} {it.Transaction.Amount,10:F2} {g.Currency}  {it.Wallet.Name} — {it.Transaction.Description}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("2) Топ-3 расходов (Expense) за указанный месяц для каждого кошелька:\n");

        var walletsScope = hasFilter
            ? wallets.Where(w => w.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            : wallets;

        foreach (var w in walletsScope)
        {
            var top = w.TransactionsInMonth(year, month)
                .Where(t => t.Type == TransactionType.Expense)
                .OrderByDescending(t => t.Amount)
                .Take(3)
                .ToList();

            Console.WriteLine($"Кошелёк: {w.Name} (баланс: {w.CurrentBalance:F2} {w.Currency})");
            if (!top.Any())
            {
                Console.WriteLine("  Нет расходов в этом месяце.");
            }
            else
            {
                var rank = 1;
                foreach (var t in top)
                {
                    Console.WriteLine($"  {rank}. {t.Date:yyyy-MM-dd} — {t.Amount:F2} {w.Currency} — {t.Description}");
                    rank++;
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("=== Конец отчёта ===");
    }
}
