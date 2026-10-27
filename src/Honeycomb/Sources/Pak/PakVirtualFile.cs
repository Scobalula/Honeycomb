using System.IO.Compression;
using RedFox.IO.FileSystem;

namespace Honeycomb.Sources.Pak;

public sealed class PakVirtualFile : VirtualFile
{
    private readonly ZipArchiveEntry	_entry;
    private readonly SemaphoreSlim		_archiveLock;

    internal PakVirtualFile(ZipArchiveEntry entry, SemaphoreSlim archiveLock) : base(entry.Length)
    {
        _entry			= entry;
        _archiveLock	= archiveLock;
    }

    public override Stream Open()
    {
        _archiveLock.Wait();

        try
        {
            return new PakEntryStream(_entry.Open(), _archiveLock);
        }
        catch
        {
            _archiveLock.Release();
            throw;
        }
    }
}
