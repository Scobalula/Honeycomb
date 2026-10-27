using RedFox.GameExtraction;
using RedFox.GameExtraction.UI;
using RedFox.GameExtraction.UI.ViewModels;
using Honeycomb;

namespace Honeycomb.UI;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        GameExtractionConfig config = new()
        {
            AssetManagerFactory = HoneycombAssetManagerFactory.Create,
            WindowTitle = "Honeycomb",
            SidebarTitle = "Honeycomb",
            Description = "Swarm Engine Extractor",
            AppName = "Honeycomb",
            IconPath = Path.Combine(AppContext.BaseDirectory, "Icon.png"),
            SidebarIconPath = Path.Combine(AppContext.BaseDirectory, "Icon.png"),
            SkyboxPath = Path.Combine(AppContext.BaseDirectory, "Skybox", "tree_lined_driveway_8k.hdr"),
            Version = HoneycombBuildInfo.DisplayVersion,
            AccentColor = "#c68c32",
            FileFilter = "Swarm Paks|*.pak|All Files|*.*",
            SupportsFileSources = true,
            MetadataColumns = ["ArchivePath"],
            Settings = HoneycombSettings.CreateDefaults(),
            SettingDefinitions = [.. HoneycombSettings.Definitions, .. PreviewSettings.SettingDefinitions],
            About = new AboutConfig
            {
                Description = "Swarm Engine Extractor",
                Links =
                [
                    new AboutLink { Label = "Github", Url = "https://github.com/" },
                    new AboutLink { Label = "Donate", Url = "https://ko-fi.com/" },
                ],
            },
            Donation = new DonationConfig
            {
                Url = "https://ko-fi.com/",
                Message = "Honeycomb is free and always will be. If it saved you some time, consider buying me a coffee!",
            },
        };

        GameExtractionApp.Run(config, args);
    }
}
