using HKEXReader.ExternalClient;
using Spectre.Console.Cli;


namespace HKEXReader;

/// <summary>
/// Orchestrates downloading CCASS shareholding data from HKEX and writing
/// snapshot / concentration reports via <see cref="DataProcessor"/>.
/// </summary>
public class DownloadCommand : AsyncCommand<DownloadSettings>
{

    protected override async Task<int> ExecuteAsync(CommandContext context, DownloadSettings settings, CancellationToken cancellationToken)
    {
        String outputPath = null!;
        String targetDate = null!;
        DateTime targetDateTime = DateTime.Now;

        var stockCode = settings.StockCode;

        // Validate optional output path; create is deferred to DataProcessor per stock.
        outputPath = settings.OutputPath;
        if (outputPath != null && !Directory.Exists(outputPath))
        {
            Console.WriteLine("Output Path Error");
            return 1;
        }

        // Optional target date must be yyyy-MM-dd (local calendar date).
        targetDate = settings.TargetDate;
        if (targetDate != null)
        {
            if (!DateTime.TryParseExact(targetDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out targetDateTime))
            {
                Console.WriteLine("Target Date Error");
                return 1;
            }
        }

        // Fall back to <cwd>/data when -o is not supplied.
        if (outputPath == null)
        {
            outputPath = Environment.CurrentDirectory;
            outputPath = Path.Combine(outputPath, "data");
        }

        Console.WriteLine($"File Output Path: {outputPath}");
        if (settings.UseSqlite)
        {
            Console.WriteLine($"SQLite enabled: one database per stock folder ({SqliteDataStore.StockDatabaseFileName})");
        }

        HKEXCCASSReader reader = new HKEXCCASSReader();
        DataProcessor processor = new DataProcessor(outputPath, settings.UseSqlite);

        List<String> stockCodeList = [];

        // Resolve which stock codes to process based on the StockCode argument.
        if (stockCode == "STOCKCODE")
        {
            // Special mode: only write the HKEX stock list; do not download holdings.
            var stockListResult = await reader.GetStockListAsync();
            await processor.SaveStockListAsync(stockListResult.StockList, stockListResult.RecordDate);
            return 0;

        }
        else if (stockCode == "ALL")
        {
            // Special mode: every equity whose code starts with "0" (main-board style codes).
            var stockListResult = await reader.GetStockListAsync();
            stockCodeList = (from s in stockListResult.StockList
                             where s.StockCode.StartsWith("0")
                             select s.StockCode).ToList();
        }
        else if (stockCode.Contains(","))
        {
            // Comma-separated list of stock codes.
            var stockCodes = stockCode.Split(",");
            foreach (var code in stockCodes)
            {
                if (!Int32.TryParse(code, out var _))
                {
                    Console.WriteLine("Stock Code Error");
                    return 1;
                }
                stockCodeList.Add(code);
            }
        }
        else
        {
            // Single stock code.
            if (!Int32.TryParse(stockCode, out var _))
            {
                Console.WriteLine("Stock Code Error");
                return 1;
            }
            stockCodeList.Add(stockCode);
        }

        Random random = new Random();

        // Download each stock with light retries and a random delay to avoid hammering HKEX.
        foreach (var stockCodeItem in stockCodeList)
        {
            Console.WriteLine($"Processing Stock Code: {stockCodeItem}");
            var dataRetrieved = false;
            var tryCount = 0;
            while (!dataRetrieved && tryCount < 5)
            {
                var stockResult = await reader.GetSearchSDWAsync(stockCodeItem, targetDate != null ? targetDateTime : (DateTime?)null);

                if (stockResult.ShareholdingList.Count == 0)
                {
                    // Empty result is often transient (session/date window); retry up to 5 times.
                    Console.WriteLine($"No Data Found for Stock Code: {stockCodeItem}");
                    tryCount++;
                }
                else
                {
                    await processor.ProcessDataAsync(stockCodeItem, stockResult);
                    // Throttle between successful requests (2–5 seconds).
                    await Task.Delay(random.Next(2_000, 5_000));
                    dataRetrieved = true;
                }
            }

        }
        return 0;
    }
}
