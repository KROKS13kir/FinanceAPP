using Finance.Domain.Entities;

namespace Finance.Application;

internal interface IWalletRepository
{
    List<Wallet> Load();
    void Save(List<Wallet> wallets);
}
