using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Honeycomb.Formats.Sound;
using Microsoft.Extensions.Logging;
using RedFox.Audio;
using RedFox.Audio.IO;
using RedFox.Audio.Wwise;
using RedFox.GameExtraction;
using RedFox.GameExtraction.AssetHandlers;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Audio;

public sealed class WwiseSoundHandler : IAssetHandler
{
    private const string BankSuffix			= ".sound_bank.resource";
    private const string BundleSuffix		= ".sound_media_bundle.resource";
    private const string BundleExtension	= ".zip";
    private const string MediaExtension		= ".wem";
    private const string EventSuffix		= ".sound_event.resource";
    private const string EventsDirectory	= "../events";
    private const string ResourcePrefix		= "res://";

    private readonly WwiseMediaTranslator _translator = new();

    public bool CanHandle(Asset asset, GameExtractionConfiguration configuration)
    {
        return asset.Name.EndsWith(BankSuffix, StringComparison.OrdinalIgnoreCase) || asset.Name.EndsWith(BundleSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public Task<AssetReadResult> ReadAsync(Asset asset, AssetReadContext context, CancellationToken cancellationToken)
    {
        VirtualFile			resourceFile	= asset.DataSource as VirtualFile ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not backed by a file.");
        VirtualDirectory	directory		= resourceFile.Parent ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not mounted in a file system.");
        string				text			= ReadText(resourceFile);
        List<AudioClip>		clips			= [];

        if (asset.Name.EndsWith(BankSuffix, StringComparison.OrdinalIgnoreCase))
        {
            ReadBank(resourceFile, GetFile(directory, SoundBankResource.Parse(text).BankFile), clips, context, cancellationToken);
        }
        else
        {
            ReadBundle(GetFile(directory, SoundMediaBundleResource.Parse(text).BundleFile), clips, context, cancellationToken);
        }

        return Task.FromResult(new AssetReadResult
        {
            Asset	= asset,
            Data	= clips.ToArray(),
            Handler	= this,
        });
    }

    public Task<bool> ShouldExportAsync(Asset asset, AssetExportContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task ExportAsync(AssetReadResult result, AssetExportContext context, CancellationToken cancellationToken)
    {
        AudioTranslatorManager	manager			= context.AssetManager.GetRequiredService<AudioTranslatorService>().Manager;
        string[]				formats			= context.Configuration.GetOption("AudioFormats", AudioClipHandler.DefaultFormats);
        bool					skipExisting	= context.Configuration.GetOption("SkipExistingAudio", true);
        string					directory		= Path.Combine(context.ResolveAssetDirectory(result.Asset), GetResourceName(result.Asset));

        foreach (AudioClip clip in result.GetData<AudioClip[]>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            AudioClipHandler.ExportClip(clip, formats, manager, Path.Combine(directory, clip.Name), skipExisting, new AudioTranslatorOptions { SampleFormat = clip.Encoded is null ? null : SampleFormat.Int16 });
        }

        return Task.CompletedTask;
    }

    private void ReadBank(VirtualFile resourceFile, VirtualFile bankFile, List<AudioClip> clips, AssetReadContext context, CancellationToken cancellationToken)
    {
        WwiseBank					bank		= WwiseBank.Read(bankFile.ReadAllBytes());
        Dictionary<uint, byte[]>	media		= bank.Media.ToDictionary(item => item.Id, item => item.Data);
        HashSet<uint>				streamed	= [.. media.Where(item => IsPrefetch(item.Value)).Select(item => item.Key), .. bank.Nodes.SelectMany(node => node.SourceIds).Where(id => !media.ContainsKey(id))];

        foreach (VirtualFile bundleFile in FindBundles(resourceFile, bankFile))
        {
            using ZipArchive bundle = OpenBundle(bundleFile);

            foreach (uint id in streamed.ToArray())
            {
                if (bundle.GetEntry(id + MediaExtension) is ZipArchiveEntry entry)
                {
                    media[id] = ReadEntry(entry);
                    streamed.Remove(id);
                }
            }
        }

        int missing = streamed.Count(id => !media.ContainsKey(id));

        if (missing > 0)
        {
            context.AssetManager.Logger.LogWarning("{Count} media referenced by {Bank} are not in any of its media bundles.", missing, bankFile.Name);
        }

        foreach ((uint id, byte[] data) in media)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddClip(GetMediaName(id.ToString(), context), data, clips, context);
        }
    }

    private static List<VirtualFile> FindBundles(VirtualFile resourceFile, VirtualFile bankFile)
    {
        VirtualDirectory	root		= resourceFile.Parent!;
        string				bankPath	= ResourcePrefix + resourceFile.FullPath;
        HashSet<string>		bundlePaths	= new(StringComparer.OrdinalIgnoreCase);
        List<VirtualFile>	bundles		= [];

        while (root.Parent is not null)
        {
            root = root.Parent;
        }

        if (bankFile.Parent!.TryGetFile(Path.ChangeExtension(bankFile.Name, BundleExtension), out VirtualFile? sameName))
        {
            bundles.Add(sameName);
        }

        if (resourceFile.Parent!.TryGetDirectory(EventsDirectory, out VirtualDirectory? events))
        {
            foreach (VirtualFile eventFile in events.EnumerateFiles("*" + EventSuffix))
            {
                SoundEventResource soundEvent = SoundEventResource.Parse(ReadText(eventFile));

                if (soundEvent.Banks.Contains(bankPath, StringComparer.OrdinalIgnoreCase))
                {
                    bundlePaths.UnionWith(soundEvent.MediaBundles);
                }
            }
        }

        foreach (string bundlePath in bundlePaths)
        {
            if (root.TryGetFile(bundlePath[ResourcePrefix.Length..], out VirtualFile? bundleResource) && bundleResource.Parent!.TryGetFile(SoundMediaBundleResource.Parse(ReadText(bundleResource)).BundleFile, out VirtualFile? bundleFile) && !bundles.Contains(bundleFile))
            {
                bundles.Add(bundleFile);
            }
        }

        return bundles;
    }

    private void ReadBundle(VirtualFile bundleFile, List<AudioClip> clips, AssetReadContext context, CancellationToken cancellationToken)
    {
        using ZipArchive bundle = OpenBundle(bundleFile);

        foreach (ZipArchiveEntry entry in bundle.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddClip(GetMediaName(Path.GetFileNameWithoutExtension(entry.FullName), context), ReadEntry(entry), clips, context);
        }
    }

    private void AddClip(string name, byte[] data, List<AudioClip> clips, AssetReadContext context)
    {
        if (IsPrefetch(data) || !_translator.TryRead(data, out AudioClip? clip))
        {
            context.AssetManager.Logger.LogWarning("Wwise media {Name} cannot be decoded and was skipped.", name);
            return;
        }

        clip.Name = name;
        clips.Add(clip);
    }

    private static string GetMediaName(string name, AssetReadContext context)
    {
        if (uint.TryParse(name, out uint id) && context.GetRequiredService<NameListService>().Manager.TryGetValue(WwiseMediaNameResolver.TableName, id, out string? label))
        {
            return $"{label}_{id}";
        }

        return name;
    }

    private static bool IsPrefetch(byte[] data)
    {
        return data.AsSpan().StartsWith("RIFF"u8) && BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)) + 8L > data.Length;
    }

    private static string ReadText(VirtualFile file) => Encoding.UTF8.GetString(file.ReadAllBytes());

    private static ZipArchive OpenBundle(VirtualFile file)
    {
        return new ZipArchive(new MemoryStream(file.ReadAllBytes()), ZipArchiveMode.Read);
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using Stream		stream	= entry.Open();
        using MemoryStream	data	= new();

        stream.CopyTo(data);
        return data.ToArray();
    }

    private static string GetResourceName(Asset asset)
    {
        string fileName = Path.GetFileName(asset.Name);

        return fileName[..fileName.IndexOf('.')];
    }

    private static VirtualFile GetFile(VirtualDirectory directory, string path)
    {
        if (!directory.TryGetFile(path, out VirtualFile? file))
        {
            throw new FileNotFoundException($"'{directory.FullPath}/{path}' was not found, load the pak that contains it.", path);
        }

        return file;
    }
}
