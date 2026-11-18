using Spectre.Console.Cli;


namespace HKEXReader;

/// <summary>
/// Entry point for the HKEX CCASS shareholding downloader CLI.
/// Registers Spectre.Console commands and dispatches to the selected subcommand.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("HKEXReader");

            // Primary command: fetch CCASS shareholding data for one or more stock codes.
            // Example: HKEXReader download 00005 -o C:\Data -d 2026-06-30
            config.AddCommand<DownloadCommand>("download")
                .WithDescription("Download CCASS Data from HKEX")
                .WithExample(new[] { "download", "00005", "-o", "C:\\Data", "-d", DateTime.Today.ToString("yyyy-MM-dd") })
                .WithExample(new[] { "download", "00005", "-o", "C:\\Data", "-d", DateTime.Today.ToString("yyyy-MM-dd"), "-s" });
        });

        await app.RunAsync(args);
    }
}
