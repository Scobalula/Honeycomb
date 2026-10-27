using System.Buffers.Binary;
using System.Numerics;

namespace Honeycomb.Formats.Morpheme;

public static class MorphemeAnimationMba
{
    private const int SampleFrequencyOffset		= 0x1C;
    private const int SectionOffset				= 0x20;
    private const int ChannelCountOffset		= 0x28;
    private const int ChannelDefaultsOffset		= 0x30;
    private const int TrajectoryOffset			= 0x40;
    private const int SectionChannelsOffset		= 8;
    private const int TrajectoryDefaultsOffset	= 32;
    private const int TrajectoryChannelOffset	= 64;
    private const int ChannelSetSize			= 32;
    private const int ChannelPosOffset			= 16;
    private const int ChannelDataOffset			= 8;
    private const int VectorSize				= 16;

    public static MorphemeAnimation Read(ReadOnlySpan<byte> data)
    {
        int section		= ReadOffset(data, SectionOffset);
        int channels	= section + ReadOffset(data, section + SectionChannelsOffset);
        int defaults	= ReadOffset(data, ChannelDefaultsOffset);
        int trajectory	= ReadOffset(data, TrajectoryOffset);

        MorphemeAnimation animation = new()
        {
            SampleFrequency	= BinaryPrimitives.ReadSingleLittleEndian(data[SampleFrequencyOffset..]),
            Channels		= new MorphemeAnimationChannel[BinaryPrimitives.ReadInt32LittleEndian(data[ChannelCountOffset..])],
        };

        for (int i = 0; i < animation.Channels.Length; i++)
        {
            animation.Channels[i]	= ReadChannelSet(data, channels + i * ChannelSetSize, defaults + i * ChannelSetSize);
            animation.FrameCount	= Math.Max(animation.FrameCount, Math.Max(animation.Channels[i].Positions.Length, animation.Channels[i].Rotations.Length));
        }

        if (trajectory != 0)
        {
            animation.Trajectory = ReadChannelSet(data, trajectory + TrajectoryChannelOffset, trajectory + TrajectoryDefaultsOffset);
        }

        return animation;
    }

    private static MorphemeAnimationChannel ReadChannelSet(ReadOnlySpan<byte> data, int channelSet, int defaults)
    {
        int quatChannel	= channelSet;
        int posChannel	= channelSet + ChannelPosOffset;
        int quatCount	= BinaryPrimitives.ReadInt32LittleEndian(data[quatChannel..]);
        int posCount	= BinaryPrimitives.ReadInt32LittleEndian(data[posChannel..]);

        MorphemeAnimationChannel channel = new()
        {
            Positions = [ReadVector(data, defaults)],
            Rotations = [ReadQuaternion(data, defaults + VectorSize)],
        };

        if (posCount > 0)
        {
            int keys = posChannel + ReadOffset(data, posChannel + ChannelDataOffset);

            channel.Positions = new Vector3[posCount];

            for (int frame = 0; frame < posCount; frame++)
            {
                channel.Positions[frame] = ReadVector(data, keys + frame * VectorSize);
            }
        }

        if (quatCount > 0)
        {
            int keys = quatChannel + ReadOffset(data, quatChannel + ChannelDataOffset);

            channel.Rotations = new Quaternion[quatCount];

            for (int frame = 0; frame < quatCount; frame++)
            {
                channel.Rotations[frame] = ReadQuaternion(data, keys + frame * VectorSize);
            }
        }

        return channel;
    }

    private static Vector3 ReadVector(ReadOnlySpan<byte> data, int offset)
    {
        return new Vector3(BinaryPrimitives.ReadSingleLittleEndian(data[offset..]), BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]), BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]));
    }

    private static Quaternion ReadQuaternion(ReadOnlySpan<byte> data, int offset)
    {
        return new Quaternion(ReadVector(data, offset), BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 12)..]));
    }

    private static int ReadOffset(ReadOnlySpan<byte> data, int offset) => (int)BinaryPrimitives.ReadInt64LittleEndian(data[offset..]);
}
