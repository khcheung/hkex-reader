using HKEXReader.ExternalClient;

namespace HKEXReader;

public class DataProcessor(String outputPath)
{
    private readonly string outputPath = outputPath;

    public async Task ProcessDataAsync(String stockCode, SearchSDWResultDto data)
    {
        var targetFolder = Path.Combine(outputPath, stockCode);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // Create Snapshot
        var snapshotFileName = Path.Combine(targetFolder, $"snapshot_{data.RecordDate:yyyyMMdd}.csv");
        var snapshotMDFileName = Path.Combine(targetFolder, $"snapshot_{data.RecordDate:yyyyMMdd}.md");

        var holdingData = data.ShareholdingList.Select(item => new
        {
            item.ID,
            item.Name,
            item.Address,
            item.Shareholding,
            ShareholdingDecimal = Decimal.TryParse(item.Shareholding, out var shareholdingValue) ? shareholdingValue : 0m,
            item.Percentage
        }).ToList();

        var summaryData = data.shareholdingSummaryList.Select(item => new
        {
            item.Category,
            item.Shareholding,
            ShareholdingDecimal = Decimal.TryParse(item.Shareholding, out var shareholdingValue) ? shareholdingValue : 0m,
            item.Participants,
            item.Percentage
        }).ToList();

        //var totalHolder = holdingData.Sum(r => r.ShareholdingDecimal);
        var totalHolding = Decimal.TryParse(data.TotalShareholding, out var totalHoldingValue) ? totalHoldingValue : 0m;


        using (var writer = new StreamWriter(snapshotFileName))
        {
            // Write Header
            await writer.WriteLineAsync("ID,Name,Shareholding,Percentage,CumulativePercentage");

            var cumulativeShareholding = 0m;

            foreach (var item in holdingData)
            {
                cumulativeShareholding += item.ShareholdingDecimal;
                var cumulativePercentage = totalHolding > 0 ? (cumulativeShareholding / totalHolding) * 100 : 0;
                var line = $"{item.ID},{item.Name},{item.ShareholdingDecimal},{item.Percentage},{cumulativePercentage:0.00}%";
                await writer.WriteLineAsync(line);
            }
            writer.Close();
        }

        using (var writer = new StreamWriter(snapshotMDFileName))
        {
            await writer.WriteLineAsync($"# Snapshot for {stockCode} on {data.RecordDate:yyyy-MM-dd}");

            await writer.WriteLineAsync("## Summary");

            await writer.WriteLineAsync("|Category|Shareholding|Percentage|");
            await writer.WriteLineAsync("|---|---|---|");

            foreach (var item in summaryData)
            {
                await writer.WriteLineAsync($"|{item.Category}|{item.ShareholdingDecimal}|{item.Percentage}|");
            }

            await writer.WriteLineAsync();

            await writer.WriteLineAsync("## Total Shareholding");

            await writer.WriteLineAsync($"Total Shareholding: {totalHolding}");

            await writer.WriteLineAsync();

            await writer.WriteLineAsync("## Detail");

            await writer.WriteLineAsync("|ID|Name|Shareholding|Percentage|CumulativePercentage");
            await writer.WriteLineAsync("|---|---|---|---|---|");

            var cumulativeShareholding = 0m;

            foreach (var item in holdingData)
            {
                cumulativeShareholding += item.ShareholdingDecimal;
                var cumulativePercentage = totalHolding > 0 ? (cumulativeShareholding / totalHolding) * 100 : 0;
                var line = $"|{item.ID}|{item.Name}|{item.ShareholdingDecimal}|{item.Percentage}|{cumulativePercentage:0.00}%|";
                await writer.WriteLineAsync(line);
            }
            writer.Close();
        }

        // Concentration Analysis
        var concentrationFileName = Path.Combine(targetFolder, $"concentration.csv");
        var concentrationMDFileName = Path.Combine(targetFolder, $"concentration.md");

        List<ConcentrationAnalysisItem> records = [];
        if (File.Exists(concentrationFileName))
        {
            using (var reader = new StreamReader(concentrationFileName))
            {
                // Read existing data if needed
                using (var csvReader = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                {
                    records = await csvReader.GetRecordsAsync<ConcentrationAnalysisItem>().ToListAsync();
                }
                reader.Close();
            }
        }

        var totalCCASS = summaryData.Select(r => r.ShareholdingDecimal).Sum();
        var totalIntermediary = summaryData.Where(r => r.Category == "Market Intermediaries").Select(r => r.ShareholdingDecimal).Sum();
        var totalIntermediaryNCIP = totalIntermediary + (summaryData.FirstOrDefault(r => r.Category == "Non-consenting Investor Participants")?.ShareholdingDecimal ?? 0);


        var top1 = holdingData.OrderByDescending(r => r.ShareholdingDecimal).Select(r => r.ShareholdingDecimal).Take(1).Sum();
        var top5 = holdingData.OrderByDescending(r => r.ShareholdingDecimal).Select(r => r.ShareholdingDecimal).Take(5).Sum();
        var top10 = holdingData.OrderByDescending(r => r.ShareholdingDecimal).Select(r => r.ShareholdingDecimal).Take(10).Sum();
        var top10ncip = top10 + (summaryData.FirstOrDefault(r => r.Category == "Non-consenting Investor Participants")?.ShareholdingDecimal ?? 0);
        var stakeInCCASS = totalHolding > 0 ? (summaryData.Select(r => r.ShareholdingDecimal).Sum() / totalHolding) * 100 : 0;

        var todayRecord = records.Where(r => r.Date == data.RecordDate.ToString("yyyyMMdd")).FirstOrDefault();
        if (todayRecord != null)
        {
            // Update existing record
            todayRecord.Top1 = (top1 / totalIntermediary * 100).ToString("0.00");
            todayRecord.Top5 = (top5 / totalIntermediary * 100).ToString("0.00");
            todayRecord.Top10 = (top10 / totalIntermediary * 100).ToString("0.00");
            todayRecord.Top10NCIP = (top10ncip / totalIntermediaryNCIP * 100).ToString("0.00");
            todayRecord.StakeInCCASS = (stakeInCCASS).ToString("0.00");
        }
        else
        {
            // Add new record
            var newRecord = new ConcentrationAnalysisItem()
            {
                Date = data.RecordDate.ToString("yyyyMMdd"),
                Top1 = (top1 / totalIntermediary * 100).ToString("0.00"),
                Top5 = (top5 / totalIntermediary * 100).ToString("0.00"),
                Top10 = (top10 / totalIntermediary * 100).ToString("0.00"),
                Top10NCIP = (top10ncip / totalIntermediaryNCIP * 100).ToString("0.00"),
                StakeInCCASS = (stakeInCCASS).ToString("0.00")
            };
            records.Add(newRecord);
        }

        var orderedRecords = records.OrderByDescending(r => r.Date).ToList();


        using (var writer = new StreamWriter(concentrationFileName))
        {
            using (var csvWriter = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                await csvWriter.WriteRecordsAsync(orderedRecords);
            }
            writer.Close();
        }

        using (var writer = new StreamWriter(concentrationMDFileName))
        {
            await writer.WriteLineAsync($"# Concentration Analysis for {stockCode}");


            await writer.WriteLineAsync("|Date|Top 1%|Top 5%|Top 10%|Top 10% NCIP|Stake in CCASS|");
            await writer.WriteLineAsync("|---|---|---|---|---|---|");

            foreach (var item in orderedRecords)
            {
                var line = $"|{item.Date}|{item.Top1}|{item.Top5}|{item.Top10}|{item.Top10NCIP}|{item.StakeInCCASS}|";
                await writer.WriteLineAsync(line);
            }
            writer.Close();
        }
    }
}
