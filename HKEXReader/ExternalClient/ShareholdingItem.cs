namespace HKEXReader.ExternalClient;

/// <summary>
/// One CCASS participant row from the Search SDW detail table.
/// Numeric fields remain strings as scraped from HTML.
/// </summary>
public class ShareholdingItem
{
    /// <summary>CCASS participant ID.</summary>
    public String ID { get; set; } = String.Empty;

    /// <summary>Participant name.</summary>
    public String Name { get; set; } = String.Empty;

    /// <summary>Registered address (when present on the page).</summary>
    public String Address { get; set; } = String.Empty;

    /// <summary>Number of shares held (formatted string, e.g. with commas).</summary>
    public String Shareholding { get; set; } = String.Empty;

    /// <summary>% of issued shares held by this participant.</summary>
    public String Percentage { get; set; } = String.Empty;
}
