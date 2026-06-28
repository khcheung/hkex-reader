using HKEXReader.ExternalClient;
using Spectre.Console;

namespace HKEXReader;

public static class Program
{
    public static async Task Main(string[] args)
    {

        // ToDo: For Development
        var stockCode = "00005";

        // Just Simple For Testing
        if (args.Length > 0)
        {
            stockCode = args[0];
        }

        if (!Int32.TryParse(stockCode, out var _))
        {
            Console.WriteLine("Stock Code Error");
            return;
        }

        HKEXCCASSReader reader = new HKEXCCASSReader();
        var result = await reader.GetSearchSDWAsync(stockCode);

        var table = new Table();

        // Add columns
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Address");
        table.AddColumn("Shareholding");
        table.AddColumn("Percentage");

        result.ForEach(r =>
        {
            // Add rows
            table.AddRow(r.ID, r.Name, r.Address, r.Shareholding, r.Percentage);
        });

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine($"Row Count: {result.Count}");
    }
}

