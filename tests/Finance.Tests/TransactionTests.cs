using System;
using System.Globalization;
using FluentAssertions;
using Xunit;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Errors;

namespace Finance.Tests;

public class TransactionTests
{
    [Fact]
    public void Factory_ShouldThrow_WhenAmountNegative()
    {
        Action act = () => Transaction.CreateExpense(DateTime.Today, -1m, "bad");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ToString_ShouldContainKeyParts()
    {
        var t = Transaction.CreateIncome(new DateTime(2025, 11, 03), 123.45m, "Bonus");
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
        var t = Transaction.CreateExpense(DateTime.Today, 10m);
        t.Description.Should().BeEmpty();
    }
}
