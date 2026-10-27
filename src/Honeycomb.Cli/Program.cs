using RedFox.GameExtraction;
using RedFox.GameExtraction.CommandLine;
using Honeycomb;

namespace Honeycomb.Cli;

internal static class Program
{
    private static Task<int> Main(string[] args) => GameExtractionCommandLineApp.RunAsync(new GameExtractionCommandLineConfig
    {
        AssetManagerFactory = HoneycombAssetManagerFactory.Create,
        Settings = HoneycombSettings.CreateDefaults(),
        SettingDefinitions = HoneycombSettings.Definitions,
        Commands = [new NamesCommand()],
        Title = "Honeycomb",
        Description = "Swarm Engine Extractor",
        AppName = "Honeycomb",
        Version = HoneycombBuildInfo.DisplayVersion,
        Theme = new CommandLineTheme
        {
            Accent = "#4AC7EE",
        },
        Donation = new DonationConfig
        {
            Url = "https://ko-fi.com/",
            Message = "Honeycomb is free and always will be. If it saved you some time, consider buying me a coffee!",
        },
    }, args);
}
