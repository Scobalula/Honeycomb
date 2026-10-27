using System.IO.Compression;
using RedFox.GameExtraction;
using RedFox.IO.FileSystem;

namespace Honeycomb.Sources.Pak;

public sealed class PakAssetSourceReader : IAssetSourceReader
{
    private static readonly char[] PathSeparators = ['/', '\\'];

    public bool CanOpen(AssetSourceRequest request)
    {
        return request.Kind == AssetSourceKind.File && string.Equals(Path.GetExtension(request.Location), ".pak", StringComparison.OrdinalIgnoreCase) && request.HeaderSpan.StartsWith("PK"u8);
    }

    public Task<IAssetSource> OpenAsync(AssetSourceRequest request, AssetManager assetManager, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        string location = request.Location ?? throw new InvalidOperationException("Pak requests must provide a file location.");

        FileStream		stream		= File.OpenRead(location);
        SemaphoreSlim	archiveLock	= new(1, 1);
        ZipArchive?		archive		= null;

        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, false);

            AssetFileSystemService	fileSystem	= assetManager.GetRequiredService<AssetFileSystemService>();
            List<Asset>				assets		= [];

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = entry.FullName[(entry.FullName.LastIndexOfAny(PathSeparators) + 1)..];

                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                PakVirtualFile	file			= new(entry, archiveLock);
                int				extensionStart	= fileName.IndexOf('.');
                string			type			= extensionStart < 0 ? "Binary" : fileName[extensionStart..];

                Asset asset = new(entry.FullName, type, file, $"{entry.Length:N0} bytes", new Dictionary<string, object?> { ["ArchivePath"] = location });

                file.Tag = asset;
                fileSystem.AddFile(entry.FullName, file, FileCollisionBehavior.Override);
                assets.Add(asset);
            }

            progress?.Report($"Mounted {assets.Count:N0} entries from {request.DisplayName}");

            IAssetSource source = new PakAssetSource(request.DisplayName, archive, assets, archiveLock);
            return Task.FromResult(source);
        }
        catch
        {
            archive?.Dispose();
            stream.Dispose();
            archiveLock.Dispose();
            throw;
        }
    }
}
