using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Application;

internal sealed class ReportService
{
    public IEnumerable<(string Currency, TransactionType Type, decimal Total, IEnumerable<(Wallet W, Transaction T)> Items)>
        GroupByCurrencyAndType(IEnumerable<Wallet> wallets, int year, int month, string? currencyFilter)
    {
        var hasFilter = !string.IsNullOrWhiteSpace(currencyFilter);

        var all = wallets
            .SelectMany(w => w.TransactionsInMonth(year, month).Select(t => (W: w, T: t)))
            .Where(x => !hasFilter || x.W.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return all
            .GroupBy(x => (x.W.Currency, x.T.Type))
            .Select(g => (g.Key.Currency, g.Key.Type, g.Sum(x => x.T.Amount),
                          g.OrderBy(x => x.T.Date).AsEnumerable()));
    }
}
