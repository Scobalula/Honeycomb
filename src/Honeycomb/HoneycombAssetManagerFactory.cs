using RedFox.GameExtraction;
using Honeycomb.Handlers.Animations;
using Honeycomb.Handlers.Audio;
using Honeycomb.Handlers.Images;
using Honeycomb.Handlers.Models;
using Honeycomb.Sources.Pak;

namespace Honeycomb;

/// <summary>
/// Creates preconfigured <see cref="AssetManager"/> instances for Honeycomb applications.
/// </summary>
public static class HoneycombAssetManagerFactory
{
    public const string NameTablesDirectory = "NameTables";

    /// <summary>
    /// Creates an <see cref="AssetManager"/> configured with ZIP mounting, the enabled asset handlers, and a shared virtual file system service.
    /// </summary>
    /// <returns>A configured asset manager.</returns>
    public static AssetManager Create()
    {
        AssetManager manager = new();

        manager.RegisterService(new AssetFileSystemService(manager));
        manager.RegisterService<ImageTranslatorService>();
        manager.RegisterService<SceneTranslatorService>();
        manager.RegisterService<AudioTranslatorService>();
        manager.RegisterService(NameListService.CreateFromDirectory(NameTablesDirectory));
        manager.RegisterSourceReader(new PakAssetSourceReader());
        manager.RegisterHandler(new PctImageHandler());
        manager.RegisterHandler(new TplModelHandler());
        manager.RegisterHandler(new SaniAnimationHandler());
        manager.RegisterHandler(new MorphemeAnimationHandler());
        manager.RegisterHandler(new WwiseSoundHandler());
        manager.RegisterHandler(new FmodSoundHandler());
        return manager;
    }
}
