#nullable enable
using Finance.Application;
using Finance.ConsoleUI;
using Finance.Domain.Entities;
using Finance.Infrastructure;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Учёт личных финансов — консольное приложение\n");

static List<Wallet> GenerateSample()
{
    var w1 = Wallet.Create("Наличные", "RUB", 5000m);
    var w2 = Wallet.Create("Карта зарплата", "RUB", 20000m);
    var w3 = Wallet.Create("USD счёт", "USD", 150m);

    var now = DateTime.Now; var y = now.Year; var m = now.Month;

    w1.AddTransaction(Transaction.CreateIncome(new DateTime(y, m, 2), 1000m, "Подработка"));
    w1.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 3), 200m, "Кофе"));
    w1.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 10), 1200m, "Одежда"));
    w1.AddTransaction(Transaction.CreateExpense(new DateTime(y, Math.Max(1, m-1), 20), 500m, "Прошлый месяц"));

    w2.AddTransaction(Transaction.CreateIncome(new DateTime(y, m, 1), 50000m, "Зарплата"));
    w2.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 5), 10000m, "Аренда"));
    w2.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 20), 8000m, "Ремонт"));

    w3.AddTransaction(Transaction.CreateIncome(new DateTime(y, m, 3), 50m, "Фриланс USD"));
    w3.AddTransaction(Transaction.CreateExpense(new DateTime(y, m, 12), 75m, "Магазин"));

    return new() { w1, w2, w3 };
}

List<Wallet> wallets = GenerateSample(); // либо загрузка из файла
var repo = new JsonWalletRepository("finance.json");
var svc  = new WalletService(repo);
new Menu(svc, wallets).Run();
