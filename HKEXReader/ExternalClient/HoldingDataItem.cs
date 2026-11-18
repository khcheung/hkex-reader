namespace HKEXReader.ExternalClient;

/// <summary>
/// Snapshot row written to CSV/Markdown: one CCASS participant with cumulative %.
/// Built from <see cref="ShareholdingItem"/> after parsing numeric shareholding.
/// </summary>
public class HoldingDataItem
{
    /// <summary>CCASS participant ID.</summary>
    public String ID { get; set; } = String.Empty;

    /// <summary>Participant display name.</summary>
    public String Name { get; set; } = String.Empty;

    /// <summary>Shareholding parsed as a decimal (for sorting and cumulative math).</summary>
    public Decimal ShareholdingDecimal { get; set; }

    /// <summary>Shareholding as % of issued shares (string as shown on HKEX).</summary>
    public String Percentage { get; set; } = String.Empty;

    /// <summary>Running cumulative % of issued shares when holders are sorted largest-first.</summary>
    public String CumulativePercentage { get; set; } = String.Empty;
}
