using Finance.Application;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Errors;

namespace Finance.ConsoleUI;

internal sealed class Menu
{
    private readonly WalletService _service;
    private List<Wallet> _wallets;

    public Menu(WalletService service, List<Wallet> wallets)
    {
        _service = service;
        _wallets = wallets;
    }

    public void Run()
    {
        while (true)
        {
            Console.WriteLine("\n1) Список кошельков\n2) Добавить транзакцию\n3) Сохранить в JSON\n4) Отчёт за месяц\n5) Загрузить из файла\n6) Выход");
            Console.Write("Выберите: ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": PrintWallets(); break;
                case "2": AddTransaction(); break;
                case "3": Save(); break;
                case "4": Report(); break;
                case "5": Reload(); break;
                case "6": return;
                default: Console.WriteLine("Неверный выбор."); break;
            }
        }
    }

    private void PrintWallets()
    {
        if (!_wallets.Any()) { Console.WriteLine("Кошельков нет."); return; }

        Console.WriteLine("\nСписок кошельков:");
        for (var i = 0; i < _wallets.Count; i++)
        {
            var w = _wallets[i];
            Console.WriteLine($"{i + 1}. {w} (ID: {w.Id})");
            foreach (var t in w.Transactions.OrderBy(t => t.Date))
                Console.WriteLine($"   {t.Date:yyyy-MM-dd} {t.Type} {t.Amount:F2} — {t.Description}");
        }
    }

    private void AddTransaction()
    {
        if (!_wallets.Any()) { Console.WriteLine("Нет кошельков."); return; }

        Console.WriteLine("Выберите кошелёк:");
        for (var i = 0; i < _wallets.Count; i++)
            Console.WriteLine($"{i + 1}) { _wallets[i].Name } ({_wallets[i].Currency}), баланс: {_wallets[i].CurrentBalance:F2}");

        Console.Write("Номер: ");
        if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 1 || idx > _wallets.Count)
        {
            Console.WriteLine("Неверный номер."); return;
        }
        var w = _wallets[idx - 1];

        var date = Input.ReadDateOrToday("Дата (YYYY-MM-DD), пусто = сегодня: ");
        Console.Write("Тип (Income/Expense): ");
        if (!Enum.TryParse<TransactionType>(Console.ReadLine(), true, out var type))
        {
            Console.WriteLine("Неверный тип."); return;
        }
        var amount = Input.ReadDecimal("Сумма: ");
        Console.Write("Описание: ");
        var desc = Console.ReadLine();

        try
        {
            if (type == TransactionType.Income)
                _service.AddIncome(w, date, amount, desc);
            else
                _service.AddExpense(w, date, amount, desc);

            Console.WriteLine("Транзакция добавлена.");
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private void Save()
    {
        Console.Write("Файл (например finance.json): ");
        var path = Console.ReadLine() ?? "finance.json";
        new Infrastructure.JsonWalletRepository(path).Save(_wallets);
        Console.WriteLine("Сохранено.");
    }

    private void Reload()
    {
        Console.Write("Файл для загрузки: ");
        var path = Console.ReadLine() ?? "finance.json";
        _wallets = new Infrastructure.JsonWalletRepository(path).Load();
        Console.WriteLine("Загружено.");
    }

    private void Report()
    {
        Console.Write("Год-месяц (YYYY-MM, пусто — текущий): ");
        var s = Console.ReadLine();
        int year, month;
        if (string.IsNullOrWhiteSpace(s))
        {
            var now = DateTime.Now; year = now.Year; month = now.Month;
        }
        else
        {
            var parts = s.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out year) || !int.TryParse(parts[1], out month) || month is < 1 or > 12)
            { Console.WriteLine("Неверный формат."); return; }
        }

        Console.Write("Фильтр валюты (пусто — все): ");
        var cf = Console.ReadLine();

        var svc = new ReportService();
        var groups = svc.GroupByCurrencyAndType(_wallets, year, month, cf)
                        .OrderBy(g => g.Currency).ThenByDescending(g => g.Total);

        string? cur = null;
        foreach (var g in groups)
        {
            if (cur != g.Currency) { cur = g.Currency; Console.WriteLine($"\n=== Валюта: {cur} ==="); }
            Console.WriteLine($"--- {g.Type} | Итого: {g.Total:F2} {g.Currency}");
            foreach (var (W, T) in g.Items)
                Console.WriteLine($"{T.Date:yyyy-MM-dd} {T.Amount,10:F2} {g.Currency}  {W.Name} — {T.Description}");
        }
    }
}
