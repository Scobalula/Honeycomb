using Honeycomb.Formats.Morpheme;
using Microsoft.Extensions.Logging;
using RedFox.GameExtraction;
using RedFox.GameExtraction.AssetHandlers;
using RedFox.Graphics3D;
using RedFox.Graphics3D.IO;
using RedFox.Graphics3D.Skeletal;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Animations;

public sealed class MorphemeAnimationHandler : IAssetHandler
{
    private const string BundleSuffix		= ".mrbundle";
    private const string NetworkExtension	= ".nmb";
    private const string NsaExtension		= ".nsa";
    private const string UcaExtension		= ".uca";
    private const string MbaExtension		= ".mba";

    public bool CanHandle(Asset asset, GameExtractionConfiguration configuration)
    {
        return asset.Name.EndsWith(BundleSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public Task<AssetReadResult> ReadAsync(Asset asset, AssetReadContext context, CancellationToken cancellationToken)
    {
        VirtualFile									bundleFile	= asset.DataSource as VirtualFile ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not backed by a file.");
        MorphemeBundle								bundle		= MorphemeBundle.Read(bundleFile.ReadAllBytes());
        Dictionary<string, MorphemeBundleFile>		files		= bundle.Files.ToDictionary(file => file.Name, StringComparer.OrdinalIgnoreCase);
        MorphemeNetwork								network		= MorphemeNetwork.Read(bundle.Files.First(file => file.Name.EndsWith(NetworkExtension, StringComparison.OrdinalIgnoreCase)).Data);
        List<Scene>									scenes		= [];

        for (int id = 0; id < network.AnimationFiles.Length; id++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string					animationFile	= network.AnimationFiles[id];
            string					extension		= Path.GetExtension(animationFile);
            string					name			= Path.GetFileNameWithoutExtension(animationFile);
            MorphemeRigToAnimMap	map				= network.AnimationMaps[id].Select(mapId => network.RigToAnimMaps[mapId]).MaxBy(map => map.RigBoneCount)!;
            MorphemeRig				rig				= network.Rigs.Values.First(rig => rig.BoneNames.Length == map.RigBoneCount);

            if (extension.Equals(NsaExtension, StringComparison.OrdinalIgnoreCase))
            {
                scenes.Add(CreateScene(name, MorphemeAnimationNsa.Read(files[animationFile].Data), map.AnimToRig, rig));
            }
            else if (extension.Equals(MbaExtension, StringComparison.OrdinalIgnoreCase))
            {
                scenes.Add(CreateScene(name, MorphemeAnimationMba.Read(files[animationFile].Data), map.AnimToRig, rig));
            }
            else if (extension.Equals(UcaExtension, StringComparison.OrdinalIgnoreCase))
            {
                scenes.Add(CreateScene(name, MorphemeAnimationUca.Read(files[animationFile].Data), [.. Enumerable.Range(0, rig.BoneNames.Length)], rig));
            }
            else
            {
                context.AssetManager.Logger.LogWarning("Morpheme animation {Name} uses an unsupported format and was skipped.", animationFile);
            }
        }

        return Task.FromResult(new AssetReadResult
        {
            Asset	= asset,
            Data	= scenes.ToArray(),
            Handler	= this,
        });
    }

    public Task<bool> ShouldExportAsync(Asset asset, AssetExportContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public async Task ExportAsync(AssetReadResult result, AssetExportContext context, CancellationToken cancellationToken)
    {
        SceneTranslatorManager	manager			= context.AssetManager.GetRequiredService<SceneTranslatorService>().Manager;
        string[]				formats			= context.Configuration.GetOption("AnimationFormats", AnimationHandler.DefaultFormats);
        bool					skipExisting	= context.Configuration.GetOption("SkipExistingAnimations", true);
        string					directory		= Path.Combine(context.ResolveAssetDirectory(result.Asset), Path.GetFileNameWithoutExtension(result.Asset.Name));

        Directory.CreateDirectory(directory);

        foreach (Scene scene in result.GetData<Scene[]>())
        {
            foreach (string format in formats)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string path = Path.Combine(directory, scene.Name + format);

                if (!skipExisting || !File.Exists(path))
                {
                    await manager.WriteAsync(path, scene, new SceneTranslatorOptions(), cancellationToken);
                }
            }
        }
    }

    private static Scene CreateScene(string name, MorphemeAnimation source, int[] channelBones, MorphemeRig rig)
    {
        Scene scene = new(name);

        SkeletonAnimation animation = scene.AddNode(new SkeletonAnimation(name, source.Channels.Length, TransformType.Absolute)
        {
            Framerate		= source.SampleFrequency,
            TransformSpace	= TransformSpace.Local,
        });

        for (int channel = 0; channel < source.Channels.Length && channel < channelBones.Length; channel++)
        {
            int bone = channelBones[channel];

            if (bone >= 0 && (bone != rig.TrajectoryBoneIndex || source.Trajectory is null))
            {
                animation.Tracks.Add(CreateTrack(rig.BoneNames[bone], source.Channels[channel]));
            }
        }

        if (source.Trajectory is not null)
        {
            animation.Tracks.Add(CreateTrack(rig.BoneNames[rig.TrajectoryBoneIndex], source.Trajectory));
        }

        return scene;
    }

    private static SkeletonAnimationTrack CreateTrack(string name, MorphemeAnimationChannel channel)
    {
        SkeletonAnimationTrack track = new(name)
        {
            TransformSpace	= TransformSpace.Local,
            TransformType	= TransformType.Absolute,
        };

        for (int frame = 0; frame < channel.Positions.Length; frame++)
        {
            track.AddTranslationFrame(frame, channel.Positions[frame]);
        }

        for (int frame = 0; frame < channel.Rotations.Length; frame++)
        {
            track.AddRotationFrame(frame, channel.Rotations[frame]);
        }

        return track;
    }
}
