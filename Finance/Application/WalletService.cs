using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Application;

internal sealed class WalletService
{
    private readonly IWalletRepository _repo;
    public WalletService(IWalletRepository repo) => _repo = repo;

    public List<Wallet> Load() => _repo.Load();
    public void Save(List<Wallet> wallets) => _repo.Save(wallets);

    public void AddIncome(Wallet w, DateTime date, decimal amount, string? desc)
        => w.AddTransaction(Transaction.CreateIncome(date, amount, desc));

    public void AddExpense(Wallet w, DateTime date, decimal amount, string? desc)
        => w.AddTransaction(Transaction.CreateExpense(date, amount, desc));
}
