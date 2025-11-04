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

app.MapPost("/api/wallets", (List<Wallet> wallets) =>
{
    Repo.Save(wallets);
    return Results.Ok(new { ok = true, count = wallets.Count });
});

app.MapPost("/api/wallets/create", (Wallet w) =>
{
    var wallets = Repo.Load();
    if (w.Id == Guid.Empty) w.Id = Guid.NewGuid();
    w.Currency = (w.Currency ?? string.Empty).Trim().ToUpperInvariant();
    wallets.Add(w);
    Repo.Save(wallets);
    return Results.Ok(w);
});

app.MapPost("/api/wallets/{id:guid}/transactions", (Guid id, Transaction tx) =>
{
    var wallets = Repo.Load();
    var w = wallets.FirstOrDefault(x => x.Id == id);
    if (w is null) return Results.NotFound(new { error = "Wallet not found" });

    if (tx.Id == Guid.Empty) tx.Id = Guid.NewGuid();
    if (!w.TryAddTransaction(tx, out var err))
        return Results.BadRequest(new { error = err });

    Repo.Save(wallets);
    return Results.Ok(tx);
});

app.MapPost("/api/sample", () =>
{
    var wallets = Repo.GenerateSample();
    foreach (var w in wallets) w.Currency = (w.Currency ?? string.Empty).Trim().ToUpperInvariant();
    Repo.Save(wallets);
    return Results.Ok(new { ok = true, count = wallets.Count });
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
