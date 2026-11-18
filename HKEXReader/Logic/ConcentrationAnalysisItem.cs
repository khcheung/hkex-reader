namespace HKEXReader;

/// <summary>
/// One day's concentration metrics for a stock, persisted in concentration.csv / .md.
/// Percentage fields are stored as formatted strings (e.g. "12.34").
/// </summary>
public class ConcentrationAnalysisItem
{
    /// <summary>Record date in yyyyMMdd form.</summary>
    public String Date { get; set; } = String.Empty;

    /// <summary>Largest participant holding as % of Market Intermediaries shareholding.</summary>
    public String Top1 { get; set; } = String.Empty;

    /// <summary>Top 5 participants as % of Market Intermediaries shareholding.</summary>
    public String Top5 { get; set; } = String.Empty;

    /// <summary>Top 10 participants as % of Market Intermediaries shareholding.</summary>
    public String Top10 { get; set; } = String.Empty;

    /// <summary>
    /// Top 10 participants plus Non-consenting Investor Participants (NCIP),
    /// as % of Market Intermediaries + NCIP.
    /// </summary>
    public String Top10NCIP { get; set; } = String.Empty;

    /// <summary>Total CCASS-held shares as % of total issued shares.</summary>
    public String StakeInCCASS { get; set; } = String.Empty;

}
