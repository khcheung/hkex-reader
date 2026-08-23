using Spectre.Console.Cli;


namespace HKEXReader;

public class DownloadSettings : CommandSettings
{
    [CommandArgument(0, "<StockCode>")]
    public string StockCode { get; set; } = null!;

    [CommandOption("-o|--output")]
    public string OutputPath { get; set; } = null!;

    [CommandOption("-d|--date")]
    public string TargetDate { get; set; } = null!;
}
