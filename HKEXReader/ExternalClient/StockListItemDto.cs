using System.Text.Json.Serialization;

namespace HKEXReader.ExternalClient;

public class StockListItemDto
{
    [JsonPropertyName("c")]
    public String StockCode { get; set; } = String.Empty;
    [JsonPropertyName("n")]public String StockName { get; set; } = String.Empty;
}
