using System.Text.Json;
using System.Text.Json.Serialization;

namespace Finance.Infrastructure;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
