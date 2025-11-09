using System.Text.Json;
using Finance.Application;
using Finance.Domain.Entities;

namespace Finance.Infrastructure;

internal sealed class JsonWalletRepository : IWalletRepository
{
    private readonly string _path;

    public JsonWalletRepository(string path) => _path = path;

    public List<Wallet> Load()
    {
        if (!File.Exists(_path)) return new List<Wallet>();
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<List<Wallet>>(json, JsonOptions.Options) ?? new List<Wallet>();
    }

    public void Save(List<Wallet> wallets)
    {
        var json = JsonSerializer.Serialize(wallets, JsonOptions.Options);
        File.WriteAllText(_path, json);
    }
}
