using System.Numerics;
using System.Text;
using Honeycomb.Formats.Sani;
using RedFox.GameExtraction;
using RedFox.GameExtraction.AssetHandlers;
using RedFox.Graphics3D;
using RedFox.Graphics3D.Skeletal;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Animations;

public sealed class SaniAnimationHandler : AnimationHandler
{
    private const string ResourceSuffix		= ".sani.resource";
    private const string CentreChannel		= "CENTRE";
    private const string TrajectoryChannel	= "Trajectory";

    public override bool CanHandle(Asset asset, GameExtractionConfiguration configuration)
    {
        return asset.Name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public override Task<AssetReadResult> ReadAsync(Asset asset, AssetReadContext context, CancellationToken cancellationToken)
    {
        VirtualFile			resourceFile	= asset.DataSource as VirtualFile ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not backed by a file.");
        VirtualDirectory	directory		= resourceFile.Parent ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not mounted in a file system.");
        SaniResource		resource		= SaniResource.Parse(Encoding.UTF8.GetString(resourceFile.ReadAllBytes()));
        byte[]?				animationData	= resource.AnimationDataFile is null ? null : GetFile(directory, resource.AnimationDataFile).ReadAllBytes();
        SaniAnimation		sani			= SaniAnimation.Read(GetFile(directory, resource.AnimationFile).ReadAllBytes(), animationData);
        SaniChannel?		root			= sani.Channels.Find(channel => channel.Name == CentreChannel) ?? sani.Channels.Find(channel => channel.Name == TrajectoryChannel);
        Scene				scene			= new(Path.GetFileName(resource.Name));

        SkeletonAnimation animation = scene.AddNode(new SkeletonAnimation(scene.Name, sani.Channels.Count, TransformType.Absolute)
        {
            Framerate		= sani.Framerate,
            TransformSpace	= TransformSpace.Local,
        });

        if (root is not null && sani.TrajectoryPositions.Length > 0)
        {
            ApplyTrajectory(root, sani);
        }

        foreach (SaniChannel channel in sani.Channels)
        {
            animation.Tracks.Add(CreateTrack(channel));
        }

        foreach (SaniEvent animationEvent in sani.Events)
        {
            animation.CreateAction(animationEvent.Name).KeyFrames.Add(new(MathF.Round(animationEvent.Time * sani.Framerate), null));
        }

        return Task.FromResult(new AssetReadResult
        {
            Asset	= asset,
            Data	= scene,
            Handler	= this,
        });
    }

    private static void ApplyTrajectory(SaniChannel channel, SaniAnimation sani)
    {
        Vector3[]		positions	= new Vector3[sani.FrameCount];
        Quaternion[]	rotations	= new Quaternion[sani.FrameCount];

        for (int frame = 0; frame < sani.FrameCount; frame++)
        {
            Quaternion trajectoryRotation = sani.TrajectoryRotations[frame];

            positions[frame] = sani.TrajectoryPositions[frame] + Vector3.Transform(channel.Positions[Math.Min(frame, channel.Positions.Length - 1)], trajectoryRotation);
            rotations[frame] = Quaternion.Concatenate(channel.Rotations[Math.Min(frame, channel.Rotations.Length - 1)], trajectoryRotation);
        }

        channel.Positions = positions;
        channel.Rotations = rotations;
    }

    private static SkeletonAnimationTrack CreateTrack(SaniChannel channel)
    {
        SkeletonAnimationTrack track = new(channel.Name)
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

        for (int frame = 0; frame < channel.Scales.Length; frame++)
        {
            track.AddScaleFrame(frame, channel.Scales[frame]);
        }

        return track;
    }

    private static VirtualFile GetFile(VirtualDirectory directory, string name)
    {
        if (!directory.TryGetFile(name, out VirtualFile? file))
        {
            throw new FileNotFoundException($"'{directory.FullPath}/{name}' was not found, load the pak that contains it.", name);
        }

        return file;
    }
}
