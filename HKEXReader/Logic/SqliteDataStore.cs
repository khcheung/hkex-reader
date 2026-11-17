using HKEXReader.ExternalClient;
using Microsoft.Data.Sqlite;

namespace HKEXReader;

public sealed class SqliteDataStore : IDisposable
{
    public const string StockDatabaseFileName = "data.db";
    public const string StockListDatabaseFileName = "stockList.db";

    private readonly SqliteConnection connection;
    private readonly bool stockListMode;
    private bool disposed;

    private SqliteDataStore(string databasePath, bool stockListMode)
    {
        this.stockListMode = stockListMode;

        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        InitializeSchema();
    }

    public static SqliteDataStore OpenStockDatabase(string stockFolder)
        => new(Path.Combine(stockFolder, StockDatabaseFileName), stockListMode: false);

    public static SqliteDataStore OpenStockListDatabase(string outputFolder)
        => new(Path.Combine(outputFolder, StockListDatabaseFileName), stockListMode: true);

    private void InitializeSchema()
    {
        using var command = connection.CreateCommand();
        command.CommandText = stockListMode
            ?
            """
            CREATE TABLE IF NOT EXISTS stock_list (
                stock_code TEXT NOT NULL PRIMARY KEY,
                stock_name TEXT NOT NULL,
                record_date TEXT
            );
            """
            :
            """
            CREATE TABLE IF NOT EXISTS snapshots (
                record_date TEXT NOT NULL PRIMARY KEY,
                stock_name TEXT NOT NULL,
                total_shareholding REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS holdings (
                record_date TEXT NOT NULL,
                participant_id TEXT NOT NULL,
                name TEXT NOT NULL,
                shareholding REAL NOT NULL,
                percentage TEXT NOT NULL,
                cumulative_percentage TEXT NOT NULL,
                PRIMARY KEY (record_date, participant_id)
            );

            CREATE TABLE IF NOT EXISTS summary (
                record_date TEXT NOT NULL,
                category TEXT NOT NULL,
                shareholding REAL NOT NULL,
                participants TEXT NOT NULL,
                percentage TEXT NOT NULL,
                PRIMARY KEY (record_date, category)
            );

            CREATE TABLE IF NOT EXISTS concentration (
                record_date TEXT NOT NULL PRIMARY KEY,
                top1 TEXT NOT NULL,
                top5 TEXT NOT NULL,
                top10 TEXT NOT NULL,
                top10_ncip TEXT NOT NULL,
                stake_in_ccass TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public void SaveSnapshot(
        SearchSDWResultDto data,
        IReadOnlyList<HoldingDataItem> holdings,
        IReadOnlyList<(string Category, decimal Shareholding, string Participants, string Percentage)> summary,
        ConcentrationAnalysisItem concentration)
    {
        if (stockListMode)
        {
            throw new InvalidOperationException("Stock database required for snapshot data.");
        }

        var recordDate = data.RecordDate.ToString("yyyyMMdd");
        var totalHolding = Decimal.TryParse(data.TotalShareholding, out var totalHoldingValue) ? totalHoldingValue : 0m;

        using var transaction = connection.BeginTransaction();

        using (var deleteHoldings = connection.CreateCommand())
        {
            deleteHoldings.Transaction = transaction;
            deleteHoldings.CommandText =
                """
                DELETE FROM holdings WHERE record_date = $record_date;
                DELETE FROM summary WHERE record_date = $record_date;
                """;
            deleteHoldings.Parameters.AddWithValue("$record_date", recordDate);
            deleteHoldings.ExecuteNonQuery();
        }

        using (var upsertSnapshot = connection.CreateCommand())
        {
            upsertSnapshot.Transaction = transaction;
            upsertSnapshot.CommandText =
                """
                INSERT INTO snapshots (record_date, stock_name, total_shareholding)
                VALUES ($record_date, $stock_name, $total_shareholding)
                ON CONFLICT(record_date) DO UPDATE SET
                    stock_name = excluded.stock_name,
                    total_shareholding = excluded.total_shareholding;
                """;
            upsertSnapshot.Parameters.AddWithValue("$record_date", recordDate);
            upsertSnapshot.Parameters.AddWithValue("$stock_name", data.StockName);
            upsertSnapshot.Parameters.AddWithValue("$total_shareholding", totalHolding);
            upsertSnapshot.ExecuteNonQuery();
        }

        using (var insertHolding = connection.CreateCommand())
        {
            insertHolding.Transaction = transaction;
            insertHolding.CommandText =
                """
                INSERT INTO holdings (
                    record_date, participant_id, name,
                    shareholding, percentage, cumulative_percentage)
                VALUES (
                    $record_date, $participant_id, $name,
                    $shareholding, $percentage, $cumulative_percentage);
                """;
            insertHolding.Parameters.Add("$record_date", SqliteType.Text);
            insertHolding.Parameters.Add("$participant_id", SqliteType.Text);
            insertHolding.Parameters.Add("$name", SqliteType.Text);
            insertHolding.Parameters.Add("$shareholding", SqliteType.Real);
            insertHolding.Parameters.Add("$percentage", SqliteType.Text);
            insertHolding.Parameters.Add("$cumulative_percentage", SqliteType.Text);

            foreach (var item in holdings)
            {
                insertHolding.Parameters["$record_date"].Value = recordDate;
                insertHolding.Parameters["$participant_id"].Value = item.ID;
                insertHolding.Parameters["$name"].Value = item.Name;
                insertHolding.Parameters["$shareholding"].Value = item.ShareholdingDecimal;
                insertHolding.Parameters["$percentage"].Value = item.Percentage;
                insertHolding.Parameters["$cumulative_percentage"].Value = item.CumulativePercentage;
                insertHolding.ExecuteNonQuery();
            }
        }

        using (var insertSummary = connection.CreateCommand())
        {
            insertSummary.Transaction = transaction;
            insertSummary.CommandText =
                """
                INSERT INTO summary (
                    record_date, category, shareholding, participants, percentage)
                VALUES (
                    $record_date, $category, $shareholding, $participants, $percentage);
                """;
            insertSummary.Parameters.Add("$record_date", SqliteType.Text);
            insertSummary.Parameters.Add("$category", SqliteType.Text);
            insertSummary.Parameters.Add("$shareholding", SqliteType.Real);
            insertSummary.Parameters.Add("$participants", SqliteType.Text);
            insertSummary.Parameters.Add("$percentage", SqliteType.Text);

            foreach (var item in summary)
            {
                insertSummary.Parameters["$record_date"].Value = recordDate;
                insertSummary.Parameters["$category"].Value = item.Category;
                insertSummary.Parameters["$shareholding"].Value = item.Shareholding;
                insertSummary.Parameters["$participants"].Value = item.Participants;
                insertSummary.Parameters["$percentage"].Value = item.Percentage;
                insertSummary.ExecuteNonQuery();
            }
        }

        using (var upsertConcentration = connection.CreateCommand())
        {
            upsertConcentration.Transaction = transaction;
            upsertConcentration.CommandText =
                """
                INSERT INTO concentration (
                    record_date, top1, top5, top10, top10_ncip, stake_in_ccass)
                VALUES (
                    $record_date, $top1, $top5, $top10, $top10_ncip, $stake_in_ccass)
                ON CONFLICT(record_date) DO UPDATE SET
                    top1 = excluded.top1,
                    top5 = excluded.top5,
                    top10 = excluded.top10,
                    top10_ncip = excluded.top10_ncip,
                    stake_in_ccass = excluded.stake_in_ccass;
                """;
            upsertConcentration.Parameters.AddWithValue("$record_date", recordDate);
            upsertConcentration.Parameters.AddWithValue("$top1", concentration.Top1);
            upsertConcentration.Parameters.AddWithValue("$top5", concentration.Top5);
            upsertConcentration.Parameters.AddWithValue("$top10", concentration.Top10);
            upsertConcentration.Parameters.AddWithValue("$top10_ncip", concentration.Top10NCIP);
            upsertConcentration.Parameters.AddWithValue("$stake_in_ccass", concentration.StakeInCCASS);
            upsertConcentration.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void SaveStockList(IReadOnlyList<StockListItemDto> stockList, DateTime? recordDate)
    {
        if (!stockListMode)
        {
            throw new InvalidOperationException("Stock list database required for stock list data.");
        }

        var recordDateValue = recordDate?.ToString("yyyyMMdd") ?? (object)DBNull.Value;

        using var transaction = connection.BeginTransaction();
        using (var clear = connection.CreateCommand())
        {
            clear.Transaction = transaction;
            clear.CommandText = "DELETE FROM stock_list;";
            clear.ExecuteNonQuery();
        }

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO stock_list (stock_code, stock_name, record_date)
                VALUES ($stock_code, $stock_name, $record_date);
                """;
            insert.Parameters.Add("$stock_code", SqliteType.Text);
            insert.Parameters.Add("$stock_name", SqliteType.Text);
            insert.Parameters.Add("$record_date", SqliteType.Text);

            foreach (var item in stockList)
            {
                insert.Parameters["$stock_code"].Value = item.StockCode;
                insert.Parameters["$stock_name"].Value = item.StockName;
                insert.Parameters["$record_date"].Value = recordDateValue;
                insert.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        connection.Dispose();
        disposed = true;
    }
}
