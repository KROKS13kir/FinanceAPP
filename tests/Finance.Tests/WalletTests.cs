using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Finance;

namespace Finance.Tests;

public class WalletTests
{
    private static Wallet Make(decimal initial, params Transaction[] tx)
    {
        var w = new Wallet("Test", "RUB", initial);
        foreach (var t in tx) w.TryAddTransaction(t, out _);
        return w;
    }

    [Fact]
    public void CurrentBalance_ComputesIncomeMinusExpense()
    {
        var w = Make(100m,
            new(new(2025, 1, 1), 50m, TransactionType.Income,  "inc"),
            new(new(2025, 1, 2), 20m, TransactionType.Expense, "exp"));

        w.CurrentBalance.Should().Be(130m);
    }

    [Fact]
    public void TryAddTransaction_BlocksExpense_WhenInsufficientFunds()
    {
        var w = Make(100m);
        var fixedDate = new DateTime(2025, 11, 15);
        var big = new Transaction(fixedDate, 101m, TransactionType.Expense, "too much");

        w.TryAddTransaction(big, out var error).Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
        w.Transactions.Should().BeEmpty();
        w.CurrentBalance.Should().Be(100m);
    }

    [Fact]
    public void TransactionsInMonth_FiltersCorrectly()
    {
        var w = Make(0m,
            new(new(2025, 11, 1), 10m, TransactionType.Income),
            new(new(2025, 10, 31), 20m, TransactionType.Income));

        var nov = w.TransactionsInMonth(2025, 11).ToList();
        nov.Should().HaveCount(1);
        nov[0].Amount.Should().Be(10m);
    }

    [Fact]
    public void MonthlySums_ReturnsIncomeAndExpenseTotals()
    {
        var w = Make(0m,
            new(new(2025, 11, 2), 100m, TransactionType.Income),
            new(new(2025, 11, 3),  40m, TransactionType.Expense),
            new(new(2025, 11, 10),  5m, TransactionType.Expense),
            new(new(2025, 10, 1), 999m, TransactionType.Income));

        var (income, expense) = w.MonthlySums(2025, 11);
        income.Should().Be(100m);
        expense.Should().Be(45m);
    }
}
