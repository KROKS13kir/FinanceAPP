using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;
using Finance;

namespace Finance.Tests;

[Collection("Console I/O")]
public class ReportSmokeTests
{
    [Fact]
    public void BuildMonthlyReport_PrintsExpectedFragments()
    {
        var w = new Wallet("Основной", "RUB", 1000m);
        _ = w.TryAddTransaction(new Transaction(new(2025, 11, 1), 500m, TransactionType.Income,  "Зарплата"), out _);
        _ = w.TryAddTransaction(new Transaction(new(2025, 11, 2), 120m, TransactionType.Expense, "Магазин"),  out _);

        var wallets = new System.Collections.Generic.List<Wallet> { w };

        using var input  = new StringReader("2025-11\n\n");
        using var output = new StringWriter();

        var oldIn  = Console.In;
        var oldOut = Console.Out;
        Console.SetIn(input);
        Console.SetOut(output);

        try
        {
            var method = typeof(Program).GetMethod(
                "BuildMonthlyReport",
                BindingFlags.NonPublic | BindingFlags.Static);

            method.Should().NotBeNull("метод BuildMonthlyReport должен существовать и быть private static");

            method!.Invoke(null, new object[] { wallets });

            var text = output.ToString();
            text.Should().Contain("Отчёт за 2025-11");
            text.Should().Contain("Транзакции, сгруппированные по валюте и типу");
            text.Should().Contain("Валюта: RUB");
            text.Should().Contain("Income");
            text.Should().Contain("Expense");
            text.Should().Contain("Топ-3 расходов");
        }
        finally
        {
            Console.SetIn(oldIn);
            Console.SetOut(oldOut);
        }
    }
}
