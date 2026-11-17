using HKEXReader.ExternalClient;
using Microsoft.Data.Sqlite;

namespace HKEXReader;

public sealed class SqliteDataStore : IDisposable
{
    private readonly SqliteConnection connection;
    private bool disposed;

    public SqliteDataStore(string databasePath)
    {
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

    private void InitializeSchema()
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS snapshots (
                stock_code TEXT NOT NULL,
                record_date TEXT NOT NULL,
                stock_name TEXT NOT NULL,
                total_shareholding REAL NOT NULL,
                PRIMARY KEY (stock_code, record_date)
            );

            CREATE TABLE IF NOT EXISTS holdings (
                stock_code TEXT NOT NULL,
                record_date TEXT NOT NULL,
                participant_id TEXT NOT NULL,
                name TEXT NOT NULL,
                shareholding REAL NOT NULL,
                percentage TEXT NOT NULL,
                cumulative_percentage TEXT NOT NULL,
                PRIMARY KEY (stock_code, record_date, participant_id)
            );

            CREATE TABLE IF NOT EXISTS summary (
                stock_code TEXT NOT NULL,
                record_date TEXT NOT NULL,
                category TEXT NOT NULL,
                shareholding REAL NOT NULL,
                participants TEXT NOT NULL,
                percentage TEXT NOT NULL,
                PRIMARY KEY (stock_code, record_date, category)
            );

            CREATE TABLE IF NOT EXISTS concentration (
                stock_code TEXT NOT NULL,
                record_date TEXT NOT NULL,
                top1 TEXT NOT NULL,
                top5 TEXT NOT NULL,
                top10 TEXT NOT NULL,
                top10_ncip TEXT NOT NULL,
                stake_in_ccass TEXT NOT NULL,
                PRIMARY KEY (stock_code, record_date)
            );

            CREATE TABLE IF NOT EXISTS stock_list (
                stock_code TEXT NOT NULL PRIMARY KEY,
                stock_name TEXT NOT NULL,
                record_date TEXT
            );
            """;
        command.ExecuteNonQuery();
    }

    public void SaveSnapshot(
        string stockCode,
        SearchSDWResultDto data,
        IReadOnlyList<HoldingDataItem> holdings,
        IReadOnlyList<(string Category, decimal Shareholding, string Participants, string Percentage)> summary,
        ConcentrationAnalysisItem concentration)
    {
        var recordDate = data.RecordDate.ToString("yyyyMMdd");
        var totalHolding = Decimal.TryParse(data.TotalShareholding, out var totalHoldingValue) ? totalHoldingValue : 0m;

        using var transaction = connection.BeginTransaction();

        using (var deleteHoldings = connection.CreateCommand())
        {
            deleteHoldings.Transaction = transaction;
            deleteHoldings.CommandText =
                """
                DELETE FROM holdings WHERE stock_code = $stock_code AND record_date = $record_date;
                DELETE FROM summary WHERE stock_code = $stock_code AND record_date = $record_date;
                """;
            deleteHoldings.Parameters.AddWithValue("$stock_code", stockCode);
            deleteHoldings.Parameters.AddWithValue("$record_date", recordDate);
            deleteHoldings.ExecuteNonQuery();
        }

        using (var upsertSnapshot = connection.CreateCommand())
        {
            upsertSnapshot.Transaction = transaction;
            upsertSnapshot.CommandText =
                """
                INSERT INTO snapshots (stock_code, record_date, stock_name, total_shareholding)
                VALUES ($stock_code, $record_date, $stock_name, $total_shareholding)
                ON CONFLICT(stock_code, record_date) DO UPDATE SET
                    stock_name = excluded.stock_name,
                    total_shareholding = excluded.total_shareholding;
                """;
            upsertSnapshot.Parameters.AddWithValue("$stock_code", stockCode);
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
                    stock_code, record_date, participant_id, name,
                    shareholding, percentage, cumulative_percentage)
                VALUES (
                    $stock_code, $record_date, $participant_id, $name,
                    $shareholding, $percentage, $cumulative_percentage);
                """;
            insertHolding.Parameters.Add("$stock_code", SqliteType.Text);
            insertHolding.Parameters.Add("$record_date", SqliteType.Text);
            insertHolding.Parameters.Add("$participant_id", SqliteType.Text);
            insertHolding.Parameters.Add("$name", SqliteType.Text);
            insertHolding.Parameters.Add("$shareholding", SqliteType.Real);
            insertHolding.Parameters.Add("$percentage", SqliteType.Text);
            insertHolding.Parameters.Add("$cumulative_percentage", SqliteType.Text);

            foreach (var item in holdings)
            {
                insertHolding.Parameters["$stock_code"].Value = stockCode;
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
                    stock_code, record_date, category, shareholding, participants, percentage)
                VALUES (
                    $stock_code, $record_date, $category, $shareholding, $participants, $percentage);
                """;
            insertSummary.Parameters.Add("$stock_code", SqliteType.Text);
            insertSummary.Parameters.Add("$record_date", SqliteType.Text);
            insertSummary.Parameters.Add("$category", SqliteType.Text);
            insertSummary.Parameters.Add("$shareholding", SqliteType.Real);
            insertSummary.Parameters.Add("$participants", SqliteType.Text);
            insertSummary.Parameters.Add("$percentage", SqliteType.Text);

            foreach (var item in summary)
            {
                insertSummary.Parameters["$stock_code"].Value = stockCode;
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
                    stock_code, record_date, top1, top5, top10, top10_ncip, stake_in_ccass)
                VALUES (
                    $stock_code, $record_date, $top1, $top5, $top10, $top10_ncip, $stake_in_ccass)
                ON CONFLICT(stock_code, record_date) DO UPDATE SET
                    top1 = excluded.top1,
                    top5 = excluded.top5,
                    top10 = excluded.top10,
                    top10_ncip = excluded.top10_ncip,
                    stake_in_ccass = excluded.stake_in_ccass;
                """;
            upsertConcentration.Parameters.AddWithValue("$stock_code", stockCode);
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
