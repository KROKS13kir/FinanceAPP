using System.Globalization;

namespace Finance.ConsoleUI;

internal static class Input
{
    public static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine();

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return v;

            Console.WriteLine("Неверная сумма. Разделитель: точка или запятая.");
        }
    }

    public static DateTime ReadDateOrToday(string prompt)
    {
        Console.Write(prompt);
        var s = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(s)) return DateTime.Today;

        if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;

        Console.WriteLine("Неверный формат даты.");
        return ReadDateOrToday(prompt);
    }
}
