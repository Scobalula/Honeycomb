using System.Buffers.Binary;
using System.Numerics;

namespace Honeycomb.Formats.Morpheme;

public static class MorphemeAnimationUca
{
    private const int SampleFrequencyOffset			= 0x1C;
    private const int SectionOffset					= 0x20;
    private const int FixedPositionsOffset			= 0x40;
    private const int PositionBonesOffset			= 0x50;
    private const int TrajectoryOffset				= 0x70;
    private const int SectionBoneCountOffset		= 4;
    private const int SectionRotationsOffset		= 8;
    private const int SectionPositionCountOffset	= 16;
    private const int SectionPositionsOffset		= 24;
    private const int TrajectoryFrameCountOffset	= 28;
    private const int TrajectoryRotationsOffset		= 32;
    private const int TrajectoryPositionsOffset		= 40;
    private const int VectorSize					= 16;

    public static MorphemeAnimation Read(ReadOnlySpan<byte> data)
    {
        int section			= ReadOffset(data, SectionOffset);
        int frameCount		= BinaryPrimitives.ReadInt32LittleEndian(data[section..]);
        int boneCount		= BinaryPrimitives.ReadInt32LittleEndian(data[(section + SectionBoneCountOffset)..]);
        int rotations		= section + ReadOffset(data, section + SectionRotationsOffset);
        int positionCount	= BinaryPrimitives.ReadInt32LittleEndian(data[(section + SectionPositionCountOffset)..]);
        int positions		= section + ReadOffset(data, section + SectionPositionsOffset);
        int fixedPositions	= ReadOffset(data, FixedPositionsOffset);
        int positionBones	= ReadOffset(data, PositionBonesOffset);
        int trajectory		= ReadOffset(data, TrajectoryOffset);

        MorphemeAnimation animation = new()
        {
            SampleFrequency	= BinaryPrimitives.ReadSingleLittleEndian(data[SampleFrequencyOffset..]),
            FrameCount		= frameCount,
            Channels		= new MorphemeAnimationChannel[boneCount],
        };

        for (int bone = 0; bone < boneCount; bone++)
        {
            animation.Channels[bone] = new MorphemeAnimationChannel
            {
                Positions = [ReadVector(data, fixedPositions + bone * VectorSize)],
                Rotations = ReadRotations(data, rotations + bone * VectorSize, boneCount * VectorSize, frameCount),
            };
        }

        for (int i = 0; i < positionCount; i++)
        {
            animation.Channels[BinaryPrimitives.ReadUInt16LittleEndian(data[(positionBones + i * 2)..])].Positions = ReadPositions(data, positions + i * VectorSize, positionCount * VectorSize, frameCount);
        }

        if (trajectory != 0)
        {
            int trajectoryFrameCount = BinaryPrimitives.ReadInt32LittleEndian(data[(trajectory + TrajectoryFrameCountOffset)..]);

            animation.Trajectory = new MorphemeAnimationChannel
            {
                Positions = ReadPositions(data, trajectory + ReadOffset(data, trajectory + TrajectoryPositionsOffset), VectorSize, trajectoryFrameCount),
                Rotations = ReadRotations(data, trajectory + ReadOffset(data, trajectory + TrajectoryRotationsOffset), VectorSize, trajectoryFrameCount),
            };
        }

        return animation;
    }

    private static Vector3[] ReadPositions(ReadOnlySpan<byte> data, int offset, int stride, int frameCount)
    {
        Vector3[] positions = new Vector3[frameCount];

        for (int frame = 0; frame < frameCount; frame++)
        {
            positions[frame] = ReadVector(data, offset + frame * stride);
        }

        return positions;
    }

    private static Quaternion[] ReadRotations(ReadOnlySpan<byte> data, int offset, int stride, int frameCount)
    {
        Quaternion[] rotations = new Quaternion[frameCount];

        for (int frame = 0; frame < frameCount; frame++)
        {
            rotations[frame] = new Quaternion(ReadVector(data, offset + frame * stride), BinaryPrimitives.ReadSingleLittleEndian(data[(offset + frame * stride + 12)..]));
        }

        return rotations;
    }

    private static Vector3 ReadVector(ReadOnlySpan<byte> data, int offset)
    {
        return new Vector3(BinaryPrimitives.ReadSingleLittleEndian(data[offset..]), BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 4)..]), BinaryPrimitives.ReadSingleLittleEndian(data[(offset + 8)..]));
    }

    private static int ReadOffset(ReadOnlySpan<byte> data, int offset) => (int)BinaryPrimitives.ReadInt64LittleEndian(data[offset..]);
}
