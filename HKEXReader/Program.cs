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
        }

        List<String> stockCodeList = [];

        // ToDo: Multiple Stock Code Support
        if (stockCode.Contains(","))
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

        if (outputPath == null)
        {
            outputPath = Environment.CurrentDirectory;
            outputPath = Path.Combine(outputPath, "data");
        }

        Console.WriteLine($"File Output Path: {outputPath}");

        HKEXCCASSReader reader = new HKEXCCASSReader();

        foreach (var stockCodeItem in stockCodeList)
        {
            Console.WriteLine($"Processing Stock Code: {stockCodeItem}");

            var stockResult = await reader.GetSearchSDWAsync(stockCodeItem);

            DataProcessor processor = new DataProcessor(outputPath);
            await processor.ProcessDataAsync(stockCodeItem, stockResult);

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

