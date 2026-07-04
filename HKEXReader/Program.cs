using System.Diagnostics;
using HKEXReader.ExternalClient;
using Spectre.Console;

namespace HKEXReader;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // ToDo: For Development
        var stockCode = "00005";

        String outputPath = null!;
        String targetDate = null!;
        DateTime targetDateTime = DateTime.Now;

        // Just Simple For Testing
        if (args.Length > 0)
        {
            stockCode = args[0];
            if (args.Length > 1)
            {
                outputPath = args[1];
                if (!Directory.Exists(outputPath))
                {
                    Console.WriteLine("Output Path Error");
                    return;
                }
                Environment.CurrentDirectory = outputPath;
            }

            if (args.Length > 2)
            {
                targetDate = args[2];
                if (!DateTime.TryParseExact(targetDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out targetDateTime))
                {
                    Console.WriteLine("Target Date Error");
                    return;
                }
            }
        }

        if (outputPath == null)
        {
            outputPath = Environment.CurrentDirectory;
            outputPath = Path.Combine(outputPath, "data");
        }

        Console.WriteLine($"File Output Path: {outputPath}");

        HKEXCCASSReader reader = new HKEXCCASSReader();
        DataProcessor processor = new DataProcessor(outputPath);

        List<String> stockCodeList = [];

        if (stockCode == "STOCKCODE")
        {
            var stockListResult = await reader.GetStockListAsync();
            await processor.SaveStockListAsync(stockListResult.StockList, stockListResult.RecordDate);
            return;

        }
        else if (stockCode.Contains(","))
        {
            var stockCodes = stockCode.Split(",");
            foreach (var code in stockCodes)
            {
                if (!Int32.TryParse(code, out var _))
                {
                    Console.WriteLine("Stock Code Error");
                    return;
                }
                stockCodeList.Add(code);
            }
        }
        else
        {
            if (!Int32.TryParse(stockCode, out var _))
            {
                Console.WriteLine("Stock Code Error");
                return;
            }
            stockCodeList.Add(stockCode);
        }



        //HKEXCCASSReader reader = new HKEXCCASSReader();

        Random random = new Random();

        foreach (var stockCodeItem in stockCodeList)
        {
            Console.WriteLine($"Processing Stock Code: {stockCodeItem}");

            var stockResult = await reader.GetSearchSDWAsync(stockCodeItem, targetDate != null ? targetDateTime : (DateTime?)null);

            await processor.ProcessDataAsync(stockCodeItem, stockResult);

            await Task.Delay(random.Next(2_000, 5_000));

        }



        // var table = new Table();
        // // Add columns
        // table.AddColumn("ID");
        // table.AddColumn("Name");
        // table.AddColumn("Address");
        // table.AddColumn("Shareholding");
        // table.AddColumn("Percentage");

        // stockResult.ShareholdingList.ForEach(r =>
        // {
        //     // Add rows
        //     table.AddRow(r.ID, r.Name, r.Address, r.Shareholding, r.Percentage);
        // });

        // AnsiConsole.Write(table);
        // AnsiConsole.WriteLine($"Row Count: {stockResult.ShareholdingList.Count}");


    }
}

