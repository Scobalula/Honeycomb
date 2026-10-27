using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using RedFox.GameExtraction;

namespace Honeycomb.Sources.Pak;

public sealed class PakAssetSource : IAssetSource
{
    private readonly ZipArchive					_archive;
    private readonly SemaphoreSlim				_archiveLock;
    private readonly Dictionary<string, Asset>	_assetsByName;
    private bool								_disposed;

    public string Name { get; }

    public IReadOnlyList<Asset> Assets { get; }

    internal PakAssetSource(string name, ZipArchive archive, IReadOnlyList<Asset> assets, SemaphoreSlim archiveLock)
    {
        Name			= name;
        Assets			= assets;
        _archive		= archive;
        _archiveLock	= archiveLock;
        _assetsByName	= assets.ToDictionary(asset => asset.Name, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetAsset(string path, [NotNullWhen(true)] out Asset? asset)
    {
        return _assetsByName.TryGetValue(path, out asset);
    }

    public void Dispose()
    {
        _archiveLock.Wait();

        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _archive.Dispose();
        }
        finally
        {
            _archiveLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
