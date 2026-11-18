namespace HKEXReader.ExternalClient;

/// <summary>
/// Result of a single Search SDW query: summary categories, participant list,
/// total issued shares, and the record date that was actually used.
/// </summary>
public class SearchSDWResultDto
{
    public String StockCode { get; set; } = String.Empty;
    public String StockName { get; set; } = String.Empty;

    /// <summary>Per-participant holdings scraped from the detail table.</summary>
    public List<ShareholdingItem> ShareholdingList { get; set; } = [];

    /// <summary>Shareholding date applied after clamping to the HKEX min/max window.</summary>
    public DateTime RecordDate { get;  set; }

    /// <summary>High-level CCASS categories (e.g. Market Intermediaries, NCIP).</summary>
    public List<ShareholdingSummaryItemDto> shareholdingSummaryList { get; set; } = [];

    /// <summary>Total issued shares / warrants / units from the summary footer.</summary>
    public string TotalShareholding { get; set; } = String.Empty;
}
