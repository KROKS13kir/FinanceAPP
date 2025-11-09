using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Errors;

namespace Finance.Tests;

public class WalletTests
{
    private static Wallet Make(decimal initial, params Transaction[] tx)
    {
        var w = Wallet.Create("Test", "RUB", initial);
        foreach (var t in tx) w.AddTransaction(t);
        return w;
    }

    [Fact]
    public void CurrentBalance_ComputesIncomeMinusExpense()
    {
        var w = Make(100m,
            Transaction.CreateIncome (new(2025, 1, 1), 50m, "inc"),
            Transaction.CreateExpense(new(2025, 1, 2), 20m, "exp"));

        w.CurrentBalance.Should().Be(130m);
    }

    [Fact]
    public void AddTransaction_BlocksExpense_WhenInsufficientFunds()
    {
        var w = Make(100m);
        var fixedDate = new DateTime(2025, 11, 15);
        var big = Transaction.CreateExpense(fixedDate, 101m, "too much");

        Action act = () => w.AddTransaction(big);
        act.Should().Throw<DomainException>();

        w.Transactions.Should().BeEmpty();
        w.CurrentBalance.Should().Be(100m);
    }

    [Fact]
    public void TransactionsInMonth_FiltersCorrectly()
    {
        var w = Make(0m,
            Transaction.CreateIncome(new(2025, 11, 1), 10m),
            Transaction.CreateIncome(new(2025, 10, 31), 20m));

        var nov = w.TransactionsInMonth(2025, 11).ToList();
        nov.Should().HaveCount(1);
        nov[0].Amount.Should().Be(10m);
    }

    [Fact]
    public void MonthlySums_ReturnsIncomeAndExpenseTotals()
    {
        var w = Make(0m,
            Transaction.CreateIncome (new(2025, 11, 2), 100m),
            Transaction.CreateExpense(new(2025, 11, 3),  40m),
            Transaction.CreateExpense(new(2025, 11, 10),  5m),
            Transaction.CreateIncome (new(2025, 10, 1), 999m));

        var (income, expense) = w.MonthlySums(2025, 11);
        income.Should().Be(100m);
        expense.Should().Be(45m);
    }
}
