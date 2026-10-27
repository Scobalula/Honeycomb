using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplGeometry : IFioStruct
{
    public const uint Signature = 0x314D474F;

    private const ushort StreamsChunk			= 2;
    private const ushort GeomsChunk				= 3;
    private const ushort SplitsChunk			= 4;
    private const ushort StreamReferencesChunk	= 5;

    private const TplVertexFormat SkinIndices = TplVertexFormat.Indices | TplVertexFormat.Indices16;

    private const TplVertexFormat CompressedTexCoords = TplVertexFormat.TexCoord0Compressed | TplVertexFormat.TexCoord1Compressed | TplVertexFormat.TexCoord2Compressed | TplVertexFormat.TexCoord3Compressed | TplVertexFormat.TexCoord4Compressed | TplVertexFormat.TexCoord5Compressed;

    public List<TplObject> Objects { get; private set; } = [];

    public List<TplLodRoot> LodRoots { get; private set; } = [];

    public List<string?> Names { get; private set; } = [];

    public ushort[] ObjectIds { get; private set; } = [];

    public Matrix4x4[] ObjectMatrices { get; private set; } = [];

    public Matrix4x4[] LocalMatrices { get; private set; } = [];

    public List<TplStream> Streams { get; } = [];

    public List<TplGeomStreamReference[]> Geoms { get; } = [];

    public List<TplSplit> Splits { get; } = [];

    public List<TplStreamReference> StreamReferences { get; private set; } = [];

    public bool HasExternalStreams => Streams.Any(stream => stream.Data is null);

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                Objects = reader.ReadStructVector<TplObject>();
                break;
            case 1:
                reader.ReadVector(ReadPropertyStorage);
                break;
            case 2:
                reader.ReadVector(ReadSection);
                break;
            case 3:
                LodRoots = reader.ReadStructVector<TplLodRoot>();
                break;
            case 5:
                Names = reader.ReadVector(item => item.ReadString());
                break;
            case 6:
                ObjectIds = reader.ReadPrimitiveVector<ushort>();
                break;
            case 7:
                ObjectMatrices = reader.ReadPrimitiveVector<Matrix4x4>();
                break;
            case 8:
                LocalMatrices = reader.ReadPrimitiveVector<Matrix4x4>();
                break;
            case 9:
                reader.ReadStructVector<TplSplitRange>();
                break;
            default:
                throw new InvalidDataException($"Unsupported geometry field {index}.");
        }
    }

    public void ReadChunks(FioReader reader, int version)
    {
        if (version <= 0)
        {
            reader.Read<uint>();
        }

        ReadInfo(reader);

        FioChunk chunk = reader.ReadChunk();

        while (!chunk.IsTerminator)
        {
            switch (chunk.Id)
            {
                case StreamsChunk:
                    ReadStreams(reader, chunk);
                    break;
                case GeomsChunk:
                    ReadGeoms(reader, chunk);
                    break;
                case SplitsChunk:
                    ReadSplits(reader, chunk, version);
                    break;
                case StreamReferencesChunk:
                    ReadStreamReferences(reader, chunk);
                    break;
            }

            reader.Position	= chunk.End;
            chunk			= reader.ReadChunk();
        }

        reader.Position = chunk.End;
    }

    public void LoadStreamData(byte[] tplData)
    {
        for (int i = 0; i < Streams.Count; i++)
        {
            Streams[i].Data ??= tplData.AsSpan((int)StreamReferences[i].Offset, (int)StreamReferences[i].Size).ToArray();
        }
    }

    public Matrix4x4 GetObjectMatrix(int objectIndex)
    {
        if (objectIndex < ObjectMatrices.Length)
        {
            return ObjectMatrices[objectIndex];
        }

        return Objects[objectIndex].WorldMatrix ?? Matrix4x4.Identity;
    }

    public Matrix4x4 GetLocalMatrix(int objectIndex)
    {
        if (objectIndex < LocalMatrices.Length)
        {
            return LocalMatrices[objectIndex];
        }

        return Objects[objectIndex].LocalMatrix ?? Matrix4x4.Identity;
    }

    public TplVertexFormat GetGeomFormat(int geomIndex)
    {
        TplVertexFormat format = TplVertexFormat.None;

        foreach (TplGeomStreamReference reference in Geoms[geomIndex])
        {
            format |= Streams[reference.StreamIndex].Format;
        }

        return format;
    }

    private void ReadInfo(FioReader reader)
    {
        FioChunk chunk = reader.ReadChunk();

        reader.Read<ushort>();
        reader.Read<int>();

        int streamCount	= reader.Read<int>();
        int geomCount	= reader.Read<int>();
        int splitCount	= reader.Read<int>();

        for (int i = 0; i < streamCount; i++)
        {
            Streams.Add(new TplStream());
        }

        for (int i = 0; i < geomCount; i++)
        {
            Geoms.Add([]);
        }

        for (int i = 0; i < splitCount; i++)
        {
            Splits.Add(new TplSplit());
        }

        reader.Position = chunk.End;
    }

    private void ReadStreams(FioReader reader, FioChunk chunk)
    {
        while (reader.Position < chunk.End)
        {
            FioChunk child = reader.ReadChunk();

            foreach (TplStream stream in Streams)
            {
                switch (child.Id)
                {
                    case 0:
                        stream.Format = ReadVertexFormat(reader);
                        break;
                    case 1:
                        stream.Stride = reader.Read<ushort>();
                        break;
                    case 2:
                        stream.Size = reader.Read<int>();
                        break;
                    case 3:
                        stream.Data = reader.ReadBytes(stream.Size);
                        break;
                }
            }

            reader.Position = child.End;
        }
    }

    private void ReadGeoms(FioReader reader, FioChunk chunk)
    {
        while (reader.Position < chunk.End)
        {
            FioChunk child = reader.ReadChunk();

            if (child.Id == 2)
            {
                for (int i = 0; i < Geoms.Count; i++)
                {
                    TplGeomStreamReference[] references = new TplGeomStreamReference[reader.Read<byte>()];

                    for (int j = 0; j < references.Length; j++)
                    {
                        int streamIndex	= reader.Read<int>();
                        int offset		= reader.Read<int>();

                        references[j] = new TplGeomStreamReference(streamIndex, offset);
                    }

                    Geoms[i] = references;
                }
            }

            reader.Position = child.End;
        }
    }

    private void ReadSplits(FioReader reader, FioChunk chunk, int version)
    {
        while (reader.Position < chunk.End)
        {
            FioChunk child = reader.ReadChunk();

            foreach (TplSplit split in Splits)
            {
                switch (child.Id)
                {
                    case 0:
                        ReadSplitRange(reader, split, version);
                        break;
                    case 1:
                        split.GeomIndex = reader.Read<int>();
                        break;
                    case 3 when (GetGeomFormat(split.GeomIndex) & SkinIndices) != 0:
                        split.BonePalette = reader.ReadArray<ushort>(reader.Read<byte>());
                        break;
                    case 4:
                        ReadTexCoordMaxTiles(reader, split);
                        break;
                    case 5:
                        ReadCompression(reader, split);
                        break;
                    case 6:
                        split.ObjectIndex = reader.Read<short>();
                        reader.ReadString();
                        break;
                    case 8:
                        split.ObjectIndex	= reader.Read<short>();
                        split.Material		= FioPropertySection.Read(reader);
                        break;
                }
            }

            reader.Position = child.End;
        }
    }

    private void ReadTexCoordMaxTiles(FioReader reader, TplSplit split)
    {
        if ((GetGeomFormat(split.GeomIndex) & CompressedTexCoords) == 0)
        {
            return;
        }

        int count = reader.Read<byte>();

        for (int i = 0; i < count; i++)
        {
            int layer = reader.Read<byte>();

            split.TexCoordMaxTiles[layer] = reader.Read<ushort>();
        }
    }

    private void ReadCompression(FioReader reader, TplSplit split)
    {
        if (!GetGeomFormat(split.GeomIndex).HasFlag(TplVertexFormat.VertexCompressed))
        {
            return;
        }

        split.CompressionOffset	= new Vector3(reader.Read<short>(), reader.Read<short>(), reader.Read<short>());
        split.CompressionScale	= new Vector3(reader.Read<ushort>(), reader.Read<ushort>(), reader.Read<ushort>());
    }

    private void ReadStreamReferences(FioReader reader, FioChunk chunk)
    {
        while (reader.Position < chunk.End)
        {
            FioChunk child = reader.ReadChunk();

            if (child.Id == 0)
            {
                reader.ReadStructVector<TplStreamSet>();
                StreamReferences = reader.ReadStructVector<TplStreamReference>();
            }

            reader.Position = child.End;
        }
    }

    private static void ReadSplitRange(FioReader reader, TplSplit split, int version)
    {
        split.StartVertex		= reader.Read<ushort>();
        split.VertexCount		= reader.Read<ushort>();
        split.StartFace			= reader.Read<ushort>();
        split.FaceCount			= reader.Read<ushort>();

        reader.Read<ushort>();

        split.SkinCompoundId	= reader.Read<ushort>();

        if (version < 3)
        {
            return;
        }

        uint densityMask = reader.Read<uint>();

        reader.Position += BitOperations.PopCount(densityMask) * 8;
    }

    private static bool ReadPropertyStorage(FioReader reader)
    {
        reader.Read<int>();
        reader.ReadStruct(new TplPropertyStorage());
        return true;
    }

    private static string ReadSection(FioReader reader)
    {
        reader.Read<int>();
        return reader.ReadString();
    }

    private static TplVertexFormat ReadVertexFormat(FioReader reader)
    {
        bool[]			flags	= reader.ReadFlags();
        TplVertexFormat	format	= TplVertexFormat.None;

        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i])
            {
                format |= GetSerializedVertexFormat(i);
            }
        }

        return format;
    }

    private static TplVertexFormat GetSerializedVertexFormat(int bit) => bit switch
    {
        0	=> TplVertexFormat.Vertex,
        1	=> TplVertexFormat.Vertex4D,
        2	=> TplVertexFormat.Vertex2D,
        3	=> TplVertexFormat.VertexCompressed,
        7	=> TplVertexFormat.Weight4,
        9	=> TplVertexFormat.Indices,
        10	=> TplVertexFormat.Normal,
        11	=> TplVertexFormat.NormalCompressed,
        12	=> TplVertexFormat.Tangent0,
        13	=> TplVertexFormat.Tangent1,
        14	=> TplVertexFormat.Tangent2,
        15	=> TplVertexFormat.Tangent3,
        16	=> TplVertexFormat.Tangent4,
        17	=> TplVertexFormat.TangentCompressed,
        22	=> TplVertexFormat.Color0,
        23	=> TplVertexFormat.Color1,
        24	=> TplVertexFormat.Color2,
        25	=> TplVertexFormat.TexCoord0,
        26	=> TplVertexFormat.TexCoord1,
        27	=> TplVertexFormat.TexCoord2,
        28	=> TplVertexFormat.TexCoord3,
        29	=> TplVertexFormat.TexCoord4,
        30	=> TplVertexFormat.TexCoord0Compressed,
        31	=> TplVertexFormat.TexCoord1Compressed,
        32	=> TplVertexFormat.TexCoord2Compressed,
        33	=> TplVertexFormat.TexCoord3Compressed,
        34	=> TplVertexFormat.TexCoord4Compressed,
        35	=> TplVertexFormat.TexCoord0Vector4,
        36	=> TplVertexFormat.TexCoord1Vector4,
        37	=> TplVertexFormat.TexCoord2Vector4,
        38	=> TplVertexFormat.TexCoord3Vector4,
        39	=> TplVertexFormat.TexCoord4Vector4,
        40	=> TplVertexFormat.TexCoord0Vector4Byte,
        41	=> TplVertexFormat.TexCoord1Vector4Byte,
        42	=> TplVertexFormat.TexCoord2Vector4Byte,
        43	=> TplVertexFormat.TexCoord3Vector4Byte,
        44	=> TplVertexFormat.TexCoord4Vector4Byte,
        45	=> TplVertexFormat.NormalInVertex4,
        46	=> TplVertexFormat.Color3,
        47	=> TplVertexFormat.Color4,
        59	=> TplVertexFormat.TexCoord5,
        60	=> TplVertexFormat.TexCoord5Compressed,
        61	=> TplVertexFormat.TexCoord5Vector4,
        62	=> TplVertexFormat.TexCoord5Vector4Byte,
        63	=> TplVertexFormat.Color5,
        67	=> TplVertexFormat.MaskingFlags,
        68	=> TplVertexFormat.BlendShapeInfo,
        69	=> TplVertexFormat.Weight8,
        70	=> TplVertexFormat.Indices16,
        _	=> TplVertexFormat.None,
    };
}
