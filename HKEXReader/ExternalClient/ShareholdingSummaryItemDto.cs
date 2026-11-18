namespace HKEXReader.ExternalClient;

/// <summary>
/// One category row from the CCASS search summary block
/// (e.g. Market Intermediaries, Non-consenting Investor Participants).
/// </summary>
public class ShareholdingSummaryItemDto
{

    /// <summary>Summary category label from HKEX.</summary>
    public String Category { get; set; } = String.Empty;

    /// <summary>Aggregate shareholding for this category.</summary>
    public String Shareholding { get; set; } = String.Empty;

    /// <summary>Number of participants in this category.</summary>
    public String Participants { get; set; } = String.Empty;

    /// <summary>% of total issued shares / warrants / units.</summary>
    public String Percentage { get; set; } = String.Empty;
}
