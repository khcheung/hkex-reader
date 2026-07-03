namespace HKEXReader.ExternalClient;

public class SearchSDWResultDto
{
    public String StockCode { get; set; } = String.Empty;
    public String StockName { get; set; } = String.Empty;
    public List<ShareholdingItem> ShareholdingList { get; set; } = [];    
    public DateTime RecordDate { get;  set; }

    public List<ShareholdingSummaryItemDto> shareholdingSummaryList { get; set; } = [];

    public string TotalShareholding { get; set; } = String.Empty;
}
