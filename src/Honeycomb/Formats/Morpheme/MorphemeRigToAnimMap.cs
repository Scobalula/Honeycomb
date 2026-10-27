using System.Buffers.Binary;

namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeRigToAnimMap
{
    private const int UsedFlagsOffset	= 0x08;
    private const int DataOffset		= 0x20;
    private const int MapPairsType		= 0;
    private const int AnimToRigType		= 1;

    public int RigBoneCount { get; private set; }

    public int[] AnimToRig { get; private set; } = [];

    public static MorphemeRigToAnimMap Read(ReadOnlySpan<byte> data)
    {
        int					type		= BinaryPrimitives.ReadInt32LittleEndian(data);
        int					usedFlags	= (int)BinaryPrimitives.ReadInt64LittleEndian(data[UsedFlagsOffset..]);
        int					mapData		= (int)BinaryPrimitives.ReadInt64LittleEndian(data[DataOffset..]);
        int					count		= BinaryPrimitives.ReadInt32LittleEndian(data[mapData..]);
        ReadOnlySpan<byte>	entries		= data[(mapData + (int)BinaryPrimitives.ReadInt64LittleEndian(data[(mapData + 8)..]))..];

        return new MorphemeRigToAnimMap
        {
            RigBoneCount	= BinaryPrimitives.ReadInt32LittleEndian(data[usedFlags..]),
            AnimToRig		= type switch
            {
                AnimToRigType	=> ReadAnimToRig(entries, count),
                MapPairsType	=> ReadMapPairs(entries, count),
                _				=> throw new InvalidDataException($"Unsupported rig to anim map type {type}."),
            },
        };
    }

    private static int[] ReadAnimToRig(ReadOnlySpan<byte> entries, int count)
    {
        int[] animToRig = new int[count];

        for (int i = 0; i < count; i++)
        {
            animToRig[i] = BinaryPrimitives.ReadUInt16LittleEndian(entries[(i * 2)..]);
        }

        return animToRig;
    }

    private static int[] ReadMapPairs(ReadOnlySpan<byte> entries, int count)
    {
        int channelCount = 0;

        for (int i = 0; i < count; i++)
        {
            channelCount = Math.Max(channelCount, BinaryPrimitives.ReadUInt16LittleEndian(entries[(i * 4 + 2)..]) + 1);
        }

        int[] animToRig = new int[channelCount];

        Array.Fill(animToRig, -1);

        for (int i = 0; i < count; i++)
        {
            animToRig[BinaryPrimitives.ReadUInt16LittleEndian(entries[(i * 4 + 2)..])] = BinaryPrimitives.ReadUInt16LittleEndian(entries[(i * 4)..]);
        }

        return animToRig;
    }
}
