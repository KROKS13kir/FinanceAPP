using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;
using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Tests;

public class JsonSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Wallet_Roundtrip_SerializesEnumAsString()
    {
        var w = Wallet.Create("Карта", "RUB", 1000m);

        w.AddTransaction(Transaction.CreateIncome (new(2025, 11, 3), 500m, "Зачисление"));
        w.AddTransaction(Transaction.CreateExpense(new(2025, 11, 4), 200m, "Покупка"));

        var list = new List<Wallet> { w };

        var json = JsonSerializer.Serialize(list, JsonOptions);

        json.Should().Contain("\"Type\": \"Income\"");
        json.Should().Contain("\"Type\": \"Expense\"");
        json.Should().NotContain("\"Type\": 0");
        json.Should().NotContain("\"Type\": 1");

        var back = JsonSerializer.Deserialize<List<Wallet>>(json, JsonOptions);
        back.Should().NotBeNull().And.HaveCount(1);

        var w2 = back![0];
        w2.Id.Should().NotBe(Guid.Empty);
        w2.Name.Should().Be("Карта");
        w2.Currency.Should().Be("RUB");
        w2.InitialBalance.Should().Be(1000m);
        w2.Transactions.Count.Should().Be(2);
        w2.Transactions.ElementAt(0).Type.Should().Be(TransactionType.Income);
        w2.Transactions.ElementAt(1).Type.Should().Be(TransactionType.Expense);
        w2.CurrentBalance.Should().Be(1300m);
    }
}
