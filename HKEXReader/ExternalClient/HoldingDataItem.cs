namespace HKEXReader.ExternalClient;

public class HoldingDataItem
{
    public String ID { get; set; } = String.Empty;
    public String Name { get; set; } = String.Empty;
    public Decimal ShareholdingDecimal { get; set; }
    public String Percentage { get; set; } = String.Empty;
    public String CumulativePercentage { get; set; } = String.Empty;
}