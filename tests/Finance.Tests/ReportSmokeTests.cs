using System.Linq;
using FluentAssertions;
using Xunit;
using Finance.Domain.Entities;
using Finance.Application;
using Finance.Domain.Enums;

namespace Finance.Tests;

public class ReportServiceTests
{
    [Fact]
    public void GroupByCurrencyAndType_ShouldAggregateTotals()
    {
        var w = Wallet.Create("Основной", "RUB", 1000m);
        w.AddTransaction(Transaction.CreateIncome (new(2025, 11, 1), 500m, "Зарплата"));
        w.AddTransaction(Transaction.CreateExpense(new(2025, 11, 2), 120m, "Магазин"));

        var svc = new ReportService();
        var groups = svc.GroupByCurrencyAndType(new[] { w }, 2025, 11, currencyFilter: null).ToList();

        groups.Should().Contain(g => g.Currency == "RUB" && g.Type == TransactionType.Income  && g.Total == 500m);
        groups.Should().Contain(g => g.Currency == "RUB" && g.Type == TransactionType.Expense && g.Total == 120m);
    }
}
