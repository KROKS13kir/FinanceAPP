using System.Text.Json.Serialization;
using WEBFinanceApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapGet("/api/wallets", () =>
{
    var wallets = Repo.Load();
    return Results.Ok(wallets);
});

app.MapPost("/api/wallets", (WalletDto dto) =>
{
    var wallets = Repo.Load();
    var w = new Wallet(dto.Name, dto.Currency, dto.InitialBalance);
    wallets.Add(w);
    Repo.Save(wallets);
    return Results.Ok(w);
});


app.MapPost("/api/wallets/{id:guid}/transactions", (Guid id, TransactionDto dto) =>
{
    var wallets = Repo.Load();
    var w = wallets.FirstOrDefault(x => x.Id == id);
    if (w is null) return Results.NotFound(new { error = "Wallet not found" });

    var tx = dto.Type == TransactionType.Income
        ? Transaction.CreateIncome(dto.Date, dto.Amount, dto.Description)
        : Transaction.CreateExpense(dto.Date, dto.Amount, dto.Description);

    try
    {
        w.AddTransaction(tx);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    Repo.Save(wallets);
    return Results.Ok(tx);
});

app.MapGet("/api/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));

app.MapMethods("/api/sample", new[] { "POST", "GET" }, () =>
{
    var wallets = Repo.GenerateSample();
    Repo.Save(wallets);
    return Results.Ok(wallets);
});

app.MapGet("/api/report", (int year, int month, string? currency) =>
{
    var wallets = Repo.Load();

    if (!string.IsNullOrWhiteSpace(currency))
    {
        var cur = currency.Trim().ToUpperInvariant();
        wallets = wallets.Where(w => string.Equals(w.Currency, cur, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    var all = wallets
        .SelectMany(w => w.TransactionsInMonth(year, month)
            .Select(t => new { Wallet = w, Transaction = t }))
        .ToList();

    var groups = all
        .GroupBy(x => new { x.Transaction.Type, x.Wallet.Currency })
        .Select(g => new
        {
            Type = g.Key.Type,
            Currency = g.Key.Currency,
            Total = g.Sum(x => x.Transaction.Amount),
            Items = g.OrderBy(x => x.Transaction.Date).Select(x => new
            {
                x.Transaction.Id,
                Date = x.Transaction.Date,
                Amount = x.Transaction.Amount,
                WalletName = x.Wallet.Name,
                Currency = x.Wallet.Currency,
                Description = x.Transaction.Description
            })
            .ToList()
        })
        .OrderByDescending(g => g.Total)
        .ToList();

    var topByWallet = wallets.Select(w => new
    {
        Wallet = new { w.Id, w.Name, w.Currency, CurrentBalance = w.CurrentBalance },
        Top = w.TransactionsInMonth(year, month)
            .Where(t => t.Type == TransactionType.Expense)
            .OrderByDescending(t => t.Amount)
            .Take(3)
            .Select(t => new { t.Id, t.Date, t.Amount, t.Description })
    });

    return Results.Ok(new { year, month, groups, topByWallet });
});

app.Run();

public sealed record WalletDto(string Name, string Currency, decimal InitialBalance);
public sealed record TransactionDto(DateTime Date, decimal Amount, TransactionType Type, string? Description);
