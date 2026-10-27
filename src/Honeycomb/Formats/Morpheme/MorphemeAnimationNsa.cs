using System.Buffers.Binary;
using System.Numerics;

namespace Honeycomb.Formats.Morpheme;

public static class MorphemeAnimationNsa
{
    private const int	SampleFrequencyOffset		= 0x1C;
    private const int	ChannelCountOffset			= 0x20;
    private const int	UnchangingPosMapOffset		= 0x28;
    private const int	UnchangingQuatMapOffset		= 0x30;
    private const int	SampledPosMapOffset			= 0x40;
    private const int	SampledQuatMapOffset		= 0x48;
    private const int	PosMeansQuantisationOffset	= 0x58;
    private const int	PosQuantisationSetsOffset	= 0x88;
    private const int	QuatQuantisationSetsOffset	= 0x90;
    private const int	UnchangingDataOffset		= 0xA0;
    private const int	SectionDataOffset			= 0xA8;
    private const int	TrajectoryOffset			= 0xB0;
    private const int	UnchangingQuatCountOffset	= 4;
    private const int	UnchangingPosInfoOffset		= 12;
    private const int	UnchangingQuatInfoOffset	= 36;
    private const int	UnchangingPosDataOffset		= 72;
    private const int	UnchangingQuatDataOffset	= 80;
    private const int	SectionPosCountOffset		= 4;
    private const int	SectionQuatCountOffset		= 8;
    private const int	SectionPosDataOffset		= 16;
    private const int	SectionPosMeansOffset		= 24;
    private const int	SectionQuatDataOffset		= 32;
    private const int	SectionQuatMeansOffset		= 40;
    private const int	TrajectoryFrameCountOffset	= 28;
    private const int	TrajectoryPosInfoOffset		= 32;
    private const int	TrajectoryQuatInfoOffset	= 56;
    private const int	TrajectoryPosKeysOffset		= 80;
    private const int	TrajectoryQuatKeysOffset	= 88;
    private const int	QuantisationSetSize			= 24;
    private const int	MeanAndSetSize				= 6;
    private const float	QuatMeanScale				= 2.0f / 255.0f;

    public static MorphemeAnimation Read(ReadOnlySpan<byte> data)
    {
        MorphemeAnimation animation = new()
        {
            SampleFrequency	= BinaryPrimitives.ReadSingleLittleEndian(data[SampleFrequencyOffset..]),
            Channels		= new MorphemeAnimationChannel[BinaryPrimitives.ReadInt32LittleEndian(data[ChannelCountOffset..])],
        };

        for (int i = 0; i < animation.Channels.Length; i++)
        {
            animation.Channels[i] = new MorphemeAnimationChannel();
        }

        ReadUnchanging(animation, data, ReadOffset(data, UnchangingDataOffset));

        int section = ReadOffset(data, SectionDataOffset);

        if (section != 0)
        {
            ReadSection(animation, data, section);
        }

        int trajectory = ReadOffset(data, TrajectoryOffset);

        if (trajectory != 0)
        {
            animation.Trajectory = ReadTrajectory(data[trajectory..]);
        }

        return animation;
    }

    private static void ReadUnchanging(MorphemeAnimation animation, ReadOnlySpan<byte> data, int unchanging)
    {
        ushort[]	posChannels		= ReadChannelMap(data, ReadOffset(data, UnchangingPosMapOffset));
        ushort[]	quatChannels	= ReadChannelMap(data, ReadOffset(data, UnchangingQuatMapOffset));
        int			posData			= unchanging + ReadOffset(data, unchanging + UnchangingPosDataOffset);
        int			quatData		= unchanging + ReadOffset(data, unchanging + UnchangingQuatDataOffset);
        int			posCount		= BinaryPrimitives.ReadInt32LittleEndian(data[unchanging..]);
        int			quatCount		= BinaryPrimitives.ReadInt32LittleEndian(data[(unchanging + UnchangingQuatCountOffset)..]);

        for (int i = 0; i < posCount; i++)
        {
            animation.Channels[posChannels[i]].Positions = [Dequantise(data, unchanging + UnchangingPosInfoOffset, ReadVector16(data, posData + i * 6))];
        }

        for (int i = 0; i < quatCount; i++)
        {
            animation.Channels[quatChannels[i]].Rotations = [FromTanQuarterAngle(Dequantise(data, unchanging + UnchangingQuatInfoOffset, ReadVector16(data, quatData + i * 6)))];
        }
    }

    private static void ReadSection(MorphemeAnimation animation, ReadOnlySpan<byte> data, int section)
    {
        ushort[]	posChannels		= ReadChannelMap(data, ReadOffset(data, SampledPosMapOffset));
        ushort[]	quatChannels	= ReadChannelMap(data, ReadOffset(data, SampledQuatMapOffset));
        int			posSets			= ReadOffset(data, PosQuantisationSetsOffset);
        int			quatSets		= ReadOffset(data, QuatQuantisationSetsOffset);
        int			posCount		= BinaryPrimitives.ReadInt32LittleEndian(data[(section + SectionPosCountOffset)..]);
        int			quatCount		= BinaryPrimitives.ReadInt32LittleEndian(data[(section + SectionQuatCountOffset)..]);
        int			posData			= section + ReadOffset(data, section + SectionPosDataOffset);
        int			posMeans		= section + ReadOffset(data, section + SectionPosMeansOffset);
        int			quatData		= section + ReadOffset(data, section + SectionQuatDataOffset);
        int			quatMeans		= section + ReadOffset(data, section + SectionQuatMeansOffset);
        int			quatStride		= (quatCount * 6 + 3) & ~3;

        animation.FrameCount = BinaryPrimitives.ReadInt32LittleEndian(data[section..]);

        for (int i = 0; i < posCount; i++)
        {
            Vector3		mean		= Dequantise(data, PosMeansQuantisationOffset, ReadVector8(data, posMeans + i * MeanAndSetSize));
            Vector3[]	positions	= new Vector3[animation.FrameCount];

            for (int frame = 0; frame < animation.FrameCount; frame++)
            {
                Vector3 value = ReadPosKey(data, posData + frame * posCount * 4 + i * 4);

                positions[frame] = DequantiseBySet(data, posSets, posMeans + i * MeanAndSetSize + 3, value) + mean;
            }

            animation.Channels[posChannels[i]].Positions = positions;
        }

        for (int i = 0; i < quatCount; i++)
        {
            Quaternion		mean		= FromTanQuarterAngle(ReadVector8(data, quatMeans + i * MeanAndSetSize) * QuatMeanScale - Vector3.One);
            Quaternion[]	rotations	= new Quaternion[animation.FrameCount];

            for (int frame = 0; frame < animation.FrameCount; frame++)
            {
                Vector3 value = ReadVector16(data, quatData + frame * quatStride + i * 6);

                rotations[frame] = mean * FromTanQuarterAngle(DequantiseBySet(data, quatSets, quatMeans + i * MeanAndSetSize + 3, value));
            }

            animation.Channels[quatChannels[i]].Rotations = rotations;
        }
    }

    private static MorphemeAnimationChannel ReadTrajectory(ReadOnlySpan<byte> data)
    {
        int							frameCount	= BinaryPrimitives.ReadInt32LittleEndian(data[TrajectoryFrameCountOffset..]);
        int							posKeys		= ReadOffset(data, TrajectoryPosKeysOffset);
        int							quatKeys	= ReadOffset(data, TrajectoryQuatKeysOffset);
        MorphemeAnimationChannel	trajectory	= new()
        {
            Positions = [ReadVector32(data, TrajectoryPosInfoOffset)],
            Rotations = [new Quaternion(ReadVector32(data, TrajectoryQuatInfoOffset), ReadSingle(data, TrajectoryQuatInfoOffset + 12))],
        };

        if (posKeys != 0)
        {
            trajectory.Positions = new Vector3[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                trajectory.Positions[frame] = Dequantise(data, TrajectoryPosInfoOffset, ReadPosKey(data, posKeys + frame * 4));
            }
        }

        if (quatKeys != 0)
        {
            trajectory.Rotations = new Quaternion[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                trajectory.Rotations[frame] = FromTanQuarterAngle(Dequantise(data, TrajectoryQuatInfoOffset, ReadVector16(data, quatKeys + frame * 6)));
            }
        }

        return trajectory;
    }

    private static Vector3 Dequantise(ReadOnlySpan<byte> data, int quantisation, Vector3 value)
    {
        return value * ReadVector32(data, quantisation + 12) + ReadVector32(data, quantisation);
    }

    private static Vector3 DequantiseBySet(ReadOnlySpan<byte> data, int sets, int setIndices, Vector3 value)
    {
        int x = sets + data[setIndices] * QuantisationSetSize;
        int y = sets + data[setIndices + 1] * QuantisationSetSize;
        int z = sets + data[setIndices + 2] * QuantisationSetSize;

        Vector3 scale	= new(ReadSingle(data, x + 12), ReadSingle(data, y + 16), ReadSingle(data, z + 20));
        Vector3 offset	= new(ReadSingle(data, x), ReadSingle(data, y + 4), ReadSingle(data, z + 8));

        return value * scale + offset;
    }

    private static Quaternion FromTanQuarterAngle(Vector3 value)
    {
        float	magnitudeSquared	= value.LengthSquared();
        float	onePlusMagnitude	= 1.0f + magnitudeSquared;
        Vector3	vector				= value * (2.0f / onePlusMagnitude);

        return new Quaternion(vector, (1.0f - magnitudeSquared) / onePlusMagnitude);
    }

    private static ushort[] ReadChannelMap(ReadOnlySpan<byte> data, int offset)
    {
        ushort[] channels = new ushort[BinaryPrimitives.ReadUInt16LittleEndian(data[offset..])];

        for (int i = 0; i < channels.Length; i++)
        {
            channels[i] = BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2 + i * 2)..]);
        }

        return channels;
    }

    private static Vector3 ReadPosKey(ReadOnlySpan<byte> data, int offset)
    {
        uint key = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);

        return new Vector3((key >> 21) & 0x7FF, (key >> 10) & 0x7FF, key & 0x3FF);
    }

    private static Vector3 ReadVector8(ReadOnlySpan<byte> data, int offset) => new(data[offset], data[offset + 1], data[offset + 2]);

    private static Vector3 ReadVector16(ReadOnlySpan<byte> data, int offset)
    {
        return new Vector3(BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]), BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 2)..]), BinaryPrimitives.ReadUInt16LittleEndian(data[(offset + 4)..]));
    }

    private static Vector3 ReadVector32(ReadOnlySpan<byte> data, int offset) => new(ReadSingle(data, offset), ReadSingle(data, offset + 4), ReadSingle(data, offset + 8));

    private static float ReadSingle(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadSingleLittleEndian(data[offset..]);

    private static int ReadOffset(ReadOnlySpan<byte> data, int offset) => (int)BinaryPrimitives.ReadInt64LittleEndian(data[offset..]);
}
