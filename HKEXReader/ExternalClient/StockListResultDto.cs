namespace HKEXReader.ExternalClient;

public class StockListResultDto
{
    public DateTime RecordDate { get; set; }
    public List<StockListItemDto> StockList { get; set; } = [];
}