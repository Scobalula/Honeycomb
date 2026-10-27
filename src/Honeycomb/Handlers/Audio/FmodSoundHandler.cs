using Honeycomb.Formats.Fmod;
using Microsoft.Extensions.Logging;
using RedFox.GameExtraction;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Audio;

public sealed class FmodSoundHandler : IAssetHandler
{
    private const string BankSuffix			= ".bank";
    private const string SoundBankExtension	= ".fsb";

    public bool CanHandle(Asset asset, GameExtractionConfiguration configuration)
    {
        return asset.Name.EndsWith(BankSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public Task<AssetReadResult> ReadAsync(Asset asset, AssetReadContext context, CancellationToken cancellationToken)
    {
        VirtualFile bankFile = asset.DataSource as VirtualFile ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not backed by a file.");

        return Task.FromResult(new AssetReadResult
        {
            Asset	= asset,
            Data	= FmodBank.Read(bankFile.ReadAllBytes()),
            Handler	= this,
        });
    }

    public Task<bool> ShouldExportAsync(Asset asset, AssetExportContext context, CancellationToken cancellationToken)
    {
        bool skipExisting = context.Configuration.GetOption("SkipExistingAudio", true);

        return Task.FromResult(!skipExisting || !File.Exists(context.ResolveAssetPath(asset, SoundBankExtension)));
    }

    public async Task ExportAsync(AssetReadResult result, AssetExportContext context, CancellationToken cancellationToken)
    {
        FmodBank	bank	= result.GetData<FmodBank>();
        string		path	= context.ResolveAssetPath(result.Asset, SoundBankExtension);

        if (bank.SoundBank is null)
        {
            context.AssetManager.Logger.LogWarning("{Bank} contains no sound data and was skipped.", result.Asset.Name);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bank.SoundBank, cancellationToken);
    }
}
