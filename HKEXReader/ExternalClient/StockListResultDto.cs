namespace HKEXReader.ExternalClient;

/// <summary>
/// Wrapper for a stock list download: the date queried and the ordered list of stocks.
/// </summary>
public class StockListResultDto
{
    /// <summary>Shareholding date used for the stock list query.</summary>
    public DateTime RecordDate { get; set; }

    /// <summary>Stocks available in CCASS on <see cref="RecordDate"/>.</summary>
    public List<StockListItemDto> StockList { get; set; } = [];
}
