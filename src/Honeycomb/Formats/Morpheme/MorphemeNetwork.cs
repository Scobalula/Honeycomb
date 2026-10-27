using System.Buffers.Binary;

namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeNetwork
{
    private const int RecordMagic				= 25;
    private const int RecordHeaderSize			= 40;
    private const int RecordTypeOffset			= 8;
    private const int RecordIdOffset			= 12;
    private const int RecordSizeOffset			= 32;
    private const int RecordAlignmentOffset		= 36;
    private const int RigAsset					= 1;
    private const int RigToAnimMapAsset			= 2;
    private const int NetworkDefAsset			= 10;
    private const int AnimationLookupAsset		= 13;
    private const int NodeCountOffset			= 168;
    private const int NodesOffset				= 176;
    private const int AttribCountOffset			= 26;
    private const int AttribsOffset				= 72;
    private const int AttribHandleSize			= 16;
    private const int AttribTypeOffset			= 10;
    private const int SourceAnimType			= 23;
    private const int SourceAnimIdOffset		= 116;
    private const int SourceAnimMapOffset		= 120;

    public Dictionary<uint, MorphemeRig> Rigs { get; } = [];

    public Dictionary<uint, MorphemeRigToAnimMap> RigToAnimMaps { get; } = [];

    public Dictionary<int, HashSet<uint>> AnimationMaps { get; } = [];

    public string[] AnimationFiles { get; private set; } = [];

    public static MorphemeNetwork Read(ReadOnlySpan<byte> data)
    {
        MorphemeNetwork	network		= new();
        int				position	= 0;

        while (position + RecordHeaderSize <= data.Length && BinaryPrimitives.ReadInt32LittleEndian(data[position..]) == RecordMagic)
        {
            int					type		= BinaryPrimitives.ReadInt32LittleEndian(data[(position + RecordTypeOffset)..]);
            uint				id			= BinaryPrimitives.ReadUInt32LittleEndian(data[(position + RecordIdOffset)..]);
            int					size		= BinaryPrimitives.ReadInt32LittleEndian(data[(position + RecordSizeOffset)..]);
            int					alignment	= BinaryPrimitives.ReadInt32LittleEndian(data[(position + RecordAlignmentOffset)..]);
            int					start		= Align(position + RecordHeaderSize, alignment);
            ReadOnlySpan<byte>	record		= data.Slice(start, size);

            switch (type)
            {
                case RigAsset:
                    network.Rigs[id] = MorphemeRig.Read(record);
                    break;
                case RigToAnimMapAsset:
                    network.RigToAnimMaps[id] = MorphemeRigToAnimMap.Read(record);
                    break;
                case NetworkDefAsset:
                    network.ReadNetworkDef(record);
                    break;
                case AnimationLookupAsset:
                    network.AnimationFiles = MorphemeStringTable.Read(record, (int)BinaryPrimitives.ReadInt64LittleEndian(record));
                    break;
            }

            position = Align(start + size, 4);
        }

        return network;
    }

    private void ReadNetworkDef(ReadOnlySpan<byte> data)
    {
        int nodeCount	= BinaryPrimitives.ReadInt32LittleEndian(data[NodeCountOffset..]);
        int nodes		= (int)BinaryPrimitives.ReadInt64LittleEndian(data[NodesOffset..]);

        for (int i = 0; i < nodeCount; i++)
        {
            int node	= (int)BinaryPrimitives.ReadInt64LittleEndian(data[(nodes + i * 8)..]);
            int count	= BinaryPrimitives.ReadUInt16LittleEndian(data[(node + AttribCountOffset)..]);
            int attribs	= node + (int)BinaryPrimitives.ReadInt64LittleEndian(data[(node + AttribsOffset)..]);

            for (int j = 0; j < count; j++)
            {
                int handle = attribs + j * AttribHandleSize;
                int offset = (int)BinaryPrimitives.ReadInt64LittleEndian(data[handle..]);

                if (offset == 0 || BinaryPrimitives.ReadUInt16LittleEndian(data[(handle + offset + AttribTypeOffset)..]) != SourceAnimType)
                {
                    continue;
                }

                int		animId	= BinaryPrimitives.ReadInt32LittleEndian(data[(handle + offset + SourceAnimIdOffset)..]);
                uint	mapId	= BinaryPrimitives.ReadUInt32LittleEndian(data[(handle + offset + SourceAnimMapOffset)..]);

                if (!AnimationMaps.TryGetValue(animId, out HashSet<uint>? maps))
                {
                    maps					= [];
                    AnimationMaps[animId]	= maps;
                }

                maps.Add(mapId);
            }
        }
    }

    private static int Align(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
}
