using System.Text.Json.Serialization;

namespace HKEXReader.ExternalClient;

/// <summary>
/// One stock from the HKEX stocklist.aspx JSON API.
/// JSON property names are abbreviated (<c>c</c>/<c>n</c>).
/// </summary>
public class StockListItemDto
{
    [JsonPropertyName("c")]
    public String StockCode { get; set; } = String.Empty;

    [JsonPropertyName("n")]
    public String StockName { get; set; } = String.Empty;
}
