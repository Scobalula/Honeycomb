using System.Buffers.Binary;

namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeRig
{
    private const int TrajectoryBoneOffset	= 0x38;
    private const int NamesOffset			= 0x40;

    public int TrajectoryBoneIndex { get; private set; }

    public string[] BoneNames { get; private set; } = [];

    public static MorphemeRig Read(ReadOnlySpan<byte> data)
    {
        return new MorphemeRig
        {
            TrajectoryBoneIndex	= BinaryPrimitives.ReadInt32LittleEndian(data[TrajectoryBoneOffset..]),
            BoneNames			= MorphemeStringTable.Read(data, (int)BinaryPrimitives.ReadInt64LittleEndian(data[NamesOffset..])),
        };
    }
}
