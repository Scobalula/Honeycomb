using System.Buffers.Binary;
using System.Numerics;

namespace Honeycomb.Formats.Tpl;

public sealed class TplSplitGeometry
{
    private const float	PositionScale		= 32500.0f;
    private const float	TexCoordScale		= 32767.0f;
    private const float	PackedNormalScale	= 181.0f;
    private const float	FloatNormalScale	= 1.0f / 256.0f;
    private const int	TriangleSize		= 6;

    private readonly List<int> _vertices = [];

    public Vector3[] Positions { get; private set; } = [];

    public Vector3[]? Normals { get; private set; }

    public List<Vector2[]> TexCoordLayers { get; } = [];

    public int InfluenceCount { get; private set; }

    public int[] BoneIndices { get; private set; } = [];

    public float[] BoneWeights { get; private set; } = [];

    public int[] Faces { get; }

    private TplSplitGeometry(int faceCount)
    {
        Faces = new int[faceCount * 3];
    }

    public static TplSplitGeometry Read(TplGeometry geometry, TplSplit split)
    {
        TplVertexFormat		format		= geometry.GetGeomFormat(split.GeomIndex);
        TplSplitGeometry	result		= new(split.FaceCount);

        result.ReadFaces(geometry, split);
        result.ReadPositions(geometry, split, format);

        if ((format & (TplVertexFormat.Normal | TplVertexFormat.NormalInVertex4)) == TplVertexFormat.Normal)
        {
            result.ReadNormals(geometry, split, format);
        }

        for (int layer = 0; layer < TplVertexLayout.TexCoordLayerCount; layer++)
        {
            TplVertexFormat texCoord = TplVertexLayout.GetLayerFlag(TplVertexFormat.TexCoord0, layer);

            if (format.HasFlag(texCoord) && result.IsPerVertex(geometry, split, texCoord))
            {
                result.TexCoordLayers.Add(result.ReadTexCoords(geometry, split, format, layer));
            }
        }

        if ((format & (TplVertexFormat.Indices | TplVertexFormat.Indices16)) != 0)
        {
            result.ReadSkin(geometry, split, format);
        }

        return result;
    }

    private void ReadFaces(TplGeometry geometry, TplSplit split)
    {
        (byte[] data, int offset, _) = FindComponent(geometry, split, TplVertexFormat.None);

        ReadOnlySpan<byte>		indices			= data.AsSpan(offset + split.StartFace * TriangleSize, Faces.Length * 2);
        Dictionary<int, int>	localIndices	= [];

        for (int i = 0; i < Faces.Length; i++)
        {
            int vertex = BinaryPrimitives.ReadUInt16LittleEndian(indices[(i * 2)..]);

            if (!localIndices.TryGetValue(vertex, out int localIndex))
            {
                localIndex				= _vertices.Count;
                localIndices[vertex]	= localIndex;

                _vertices.Add(vertex);
            }

            Faces[i] = localIndex;
        }

        Positions = new Vector3[_vertices.Count];
    }

    private bool IsPerVertex(TplGeometry geometry, TplSplit split, TplVertexFormat component)
    {
        (byte[] data, int offset, int stride) = FindComponent(geometry, split, component);

        return offset + _vertices.Max() * stride < data.Length;
    }

    private void ReadPositions(TplGeometry geometry, TplSplit split, TplVertexFormat format)
    {
        (byte[] data, int offset, int stride) = FindComponent(geometry, split, TplVertexFormat.Vertex);

        bool	compressed		= format.HasFlag(TplVertexFormat.VertexCompressed);
        bool	packedNormals	= format.HasFlag(TplVertexFormat.NormalInVertex4);

        if (packedNormals)
        {
            Normals = new Vector3[Positions.Length];
        }

        for (int i = 0; i < Positions.Length; i++)
        {
            ReadOnlySpan<byte> vertex = data.AsSpan(offset + _vertices[i] * stride);

            if (compressed)
            {
                Vector3 value = new(BinaryPrimitives.ReadInt16LittleEndian(vertex), BinaryPrimitives.ReadInt16LittleEndian(vertex[2..]), BinaryPrimitives.ReadInt16LittleEndian(vertex[4..]));

                Positions[i] = split.CompressionOffset + value * split.CompressionScale / PositionScale;
            }
            else
            {
                Positions[i] = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(vertex), BinaryPrimitives.ReadSingleLittleEndian(vertex[4..]), BinaryPrimitives.ReadSingleLittleEndian(vertex[8..]));
            }

            if (packedNormals && compressed)
            {
                Normals![i] = DecodePackedNormal(BinaryPrimitives.ReadInt16LittleEndian(vertex[6..]));
            }
            else if (packedNormals)
            {
                Normals![i] = DecodeFloatNormal(BinaryPrimitives.ReadSingleLittleEndian(vertex[12..]));
            }
        }
    }

    private void ReadNormals(TplGeometry geometry, TplSplit split, TplVertexFormat format)
    {
        (byte[] data, int offset, int stride) = FindComponent(geometry, split, TplVertexFormat.Normal);

        bool	compressed	= format.HasFlag(TplVertexFormat.NormalCompressed);

        Normals = new Vector3[Positions.Length];

        for (int i = 0; i < Normals.Length; i++)
        {
            ReadOnlySpan<byte> normal = data.AsSpan(offset + _vertices[i] * stride);

            if (compressed)
            {
                Normals[i] = new Vector3(normal[0], normal[1], normal[2]) * 2.0f / 255.0f - Vector3.One;
            }
            else
            {
                Normals[i] = new Vector3(BinaryPrimitives.ReadSingleLittleEndian(normal), BinaryPrimitives.ReadSingleLittleEndian(normal[4..]), BinaryPrimitives.ReadSingleLittleEndian(normal[8..]));
            }
        }
    }

    private Vector2[] ReadTexCoords(TplGeometry geometry, TplSplit split, TplVertexFormat format, int layer)
    {
        (byte[] data, int offset, int stride) = FindComponent(geometry, split, TplVertexLayout.GetLayerFlag(TplVertexFormat.TexCoord0, layer));

        bool		compressed	= format.HasFlag(TplVertexLayout.GetLayerFlag(TplVertexFormat.TexCoord0Compressed, layer));
        float		maxTile		= split.TexCoordMaxTiles[layer];
        Vector2[]	texCoords	= new Vector2[Positions.Length];

        for (int i = 0; i < texCoords.Length; i++)
        {
            ReadOnlySpan<byte> texCoord = data.AsSpan(offset + _vertices[i] * stride);

            if (compressed)
            {
                texCoords[i] = new Vector2(BinaryPrimitives.ReadInt16LittleEndian(texCoord), BinaryPrimitives.ReadInt16LittleEndian(texCoord[2..])) * maxTile / TexCoordScale;
            }
            else
            {
                texCoords[i] = new Vector2(BinaryPrimitives.ReadSingleLittleEndian(texCoord), BinaryPrimitives.ReadSingleLittleEndian(texCoord[4..]));
            }
        }

        return texCoords;
    }

    private void ReadSkin(TplGeometry geometry, TplSplit split, TplVertexFormat format)
    {
        (byte[] indexData, int indexOffset, int indexStride) = FindComponent(geometry, split, TplVertexFormat.Indices | TplVertexFormat.Indices16);

        bool	weighted	= (format & (TplVertexFormat.Weight4 | TplVertexFormat.Weight8)) != 0;
        bool	wideIndices	= format.HasFlag(TplVertexFormat.Indices16);

        InfluenceCount	= weighted ? TplVertexLayout.GetInfluenceCount(format) : 1;
        BoneIndices		= new int[Positions.Length * InfluenceCount];
        BoneWeights		= new float[Positions.Length * InfluenceCount];

        for (int i = 0; i < Positions.Length; i++)
        {
            ReadOnlySpan<byte> indices = indexData.AsSpan(indexOffset + _vertices[i] * indexStride);

            for (int j = 0; j < InfluenceCount; j++)
            {
                int index = wideIndices ? BinaryPrimitives.ReadUInt16LittleEndian(indices[(j * 2)..]) : indices[j];

                BoneIndices[i * InfluenceCount + j] = split.BonePalette.Length > 0 ? split.BonePalette[index] : index;
            }
        }

        if (!weighted)
        {
            Array.Fill(BoneWeights, 1.0f);
            return;
        }

        (byte[] weightData, int weightOffset, int weightStride) = FindComponent(geometry, split, TplVertexFormat.Weight4 | TplVertexFormat.Weight8);

        for (int i = 0; i < Positions.Length; i++)
        {
            ReadOnlySpan<byte>	weights	= weightData.AsSpan(weightOffset + _vertices[i] * weightStride);
            float				total	= 0.0f;

            for (int j = 0; j < InfluenceCount - 1; j++)
            {
                BoneWeights[i * InfluenceCount + j]	= weights[j] / 255.0f;
                total								+= BoneWeights[i * InfluenceCount + j];
            }

            BoneWeights[(i + 1) * InfluenceCount - 1] = MathF.Max(0.0f, 1.0f - total);
        }
    }

    private static (byte[] Data, int Offset, int Stride) FindComponent(TplGeometry geometry, TplSplit split, TplVertexFormat component)
    {
        foreach (TplGeomStreamReference reference in geometry.Geoms[split.GeomIndex])
        {
            TplStream stream = geometry.Streams[reference.StreamIndex];

            if ((stream.Format & component) != 0 || stream.Format == component)
            {
                return (stream.Data!, reference.Offset + TplVertexLayout.GetOffset(stream.Format, component), stream.Stride);
            }
        }

        throw new InvalidDataException($"Geom {split.GeomIndex} has no stream containing {component}.");
    }

    private static Vector3 DecodePackedNormal(short packed)
    {
        float	magnitude	= Math.Abs((int)packed);
        float	x			= (Fraction(magnitude / PackedNormalScale) * 2.0f - 1.0f) * PackedNormalScale / 179.0f;
        float	z			= (Fraction(magnitude / PackedNormalScale / PackedNormalScale) * 2.0f - 1.0f) * PackedNormalScale / 180.0f;
        float	y			= MathF.Sqrt(MathF.Max(0.0f, 1.0f - MathF.Min(x * x + z * z, 1.0f)));

        return new Vector3(x, packed < 0 ? -y : y, z);
    }

    private static Vector3 DecodeFloatNormal(float packed)
    {
        float x = Fraction(packed * FloatNormalScale) * 2.0f - 1.0f;
        float y = Fraction(packed * FloatNormalScale * FloatNormalScale) * 2.0f - 1.0f;
        float z = Fraction(packed * FloatNormalScale * FloatNormalScale * FloatNormalScale) * 2.0f - 1.0f;

        return new Vector3(x, y, z);
    }

    private static float Fraction(float value) => value - MathF.Truncate(value);
}
