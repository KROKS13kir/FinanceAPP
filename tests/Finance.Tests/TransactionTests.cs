using System;
using System.Globalization;
using FluentAssertions;
using Xunit;
using Finance;

namespace Finance.Tests;

public class TransactionTests
{
    [Fact]
    public void Ctor_ShouldThrow_WhenAmountNegative()
    {
        Action act = () => new Transaction(DateTime.Today, -1m, TransactionType.Expense, "bad");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToString_ShouldContainKeyParts()
    {
        var t = new Transaction(new DateTime(2025, 11, 03), 123.45m, TransactionType.Income, "Bonus");
        var s = t.ToString();
        var expectedAmount = t.Amount.ToString("F2", CultureInfo.CurrentCulture);
        s.Should().Contain("2025-11-03")
                 .And.Contain($"+{expectedAmount}")
                 .And.Contain("(Income)")
                 .And.Contain("Bonus");
    }

    [Fact]
    public void Description_Defaults_ToEmpty()
    {
        var t = new Transaction(DateTime.Today, 10m, TransactionType.Expense);
        t.Description.Should().BeEmpty();
    }
}
