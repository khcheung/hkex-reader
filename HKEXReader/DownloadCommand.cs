using HKEXReader.ExternalClient;
using Spectre.Console;
using Spectre.Console.Cli;


namespace HKEXReader;

public class DownloadCommand : AsyncCommand<DownloadSettings>
{

    protected override async Task<int> ExecuteAsync(CommandContext context, DownloadSettings settings, CancellationToken cancellationToken)
    {

        // ToDo: For Development
        var stockCode = "00005";

        String outputPath = null!;
        String targetDate = null!;
        DateTime targetDateTime = DateTime.Now;


        stockCode = settings.StockCode;

        outputPath = settings.OutputPath;
        if (outputPath != null && !Directory.Exists(outputPath))
        {
            Console.WriteLine("Output Path Error");
            return 1;
        }
        //Environment.CurrentDirectory = outputPath;


        targetDate = settings.TargetDate;
        if (targetDate != null)
        {
            if (!DateTime.TryParseExact(targetDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out targetDateTime))
            {
                Console.WriteLine("Target Date Error");
                return 1;
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
            return 0;

        }
        else if (stockCode == "ALL")
        {
            var stockListResult = await reader.GetStockListAsync();
            stockCodeList = (from s in stockListResult.StockList
                             where s.StockCode.StartsWith("0")
                             select s.StockCode).ToList();
        }
        else if (stockCode.Contains(","))
        {
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
            if (!Int32.TryParse(stockCode, out var _))
            {
                Console.WriteLine("Stock Code Error");
                return 1;
            }
            stockCodeList.Add(stockCode);
        }
        //HKEXCCASSReader reader = new HKEXCCASSReader();

        Random random = new Random();

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
                    Console.WriteLine($"No Data Found for Stock Code: {stockCodeItem}");
                    tryCount++;
                }
                else
                {
                    await processor.ProcessDataAsync(stockCodeItem, stockResult);
                    await Task.Delay(random.Next(2_000, 5_000));
                    dataRetrieved = true;
                }
            }

        }
        return 0;
    }
}