using System.Diagnostics;
using Spectre.Console.Cli;


namespace HKEXReader;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("HKEXReader");
            config.AddCommand<DownloadCommand>("download")
                .WithDescription("Download CCASS Data from HKEX")
                .WithExample(new[] { "download", "00005", "-o", "C:\\Data", "-d", DateTime.Today.ToString("yyyy-MM-dd") })
                .WithExample(new[] { "download", "00005", "-o", "C:\\Data", "-d", DateTime.Today.ToString("yyyy-MM-dd"), "-s", "C:\\Data\\hkex.db" });
        });

        await app.RunAsync(args);
    }
}
