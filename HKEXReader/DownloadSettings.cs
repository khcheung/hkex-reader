using Spectre.Console.Cli;


namespace HKEXReader;

/// <summary>
/// CLI options for the <c>download</c> command.
/// Bound automatically by Spectre.Console from argv.
/// </summary>
public class DownloadSettings : CommandSettings
{
    /// <summary>
    /// Stock code(s) to download. Special values:
    /// <list type="bullet">
    /// <item><c>STOCKCODE</c> — export the full HKEX stock list only</item>
    /// <item><c>ALL</c> — download every stock whose code starts with "0"</item>
    /// <item>comma-separated codes — e.g. <c>00001,00005,00008</c></item>
    /// <item>a single numeric code — e.g. <c>00005</c></item>
    /// </list>
    /// </summary>
    [CommandArgument(0, "<StockCode>")]
    public string StockCode { get; set; } = null!;

    /// <summary>Output folder for CSV/Markdown files. Defaults to ./data under the current directory.</summary>
    [CommandOption("-o|--output")]
    public string OutputPath { get; set; } = null!;

    /// <summary>Shareholding record date in yyyy-MM-dd. When omitted, HKEX's latest available date is used.</summary>
    [CommandOption("-d|--date")]
    public string TargetDate { get; set; } = null!;

    /// <summary>When set, also persist snapshot / concentration / stock-list data to SQLite.</summary>
    [CommandOption("-s|--sqlite")]
    public bool UseSqlite { get; set; }
}
