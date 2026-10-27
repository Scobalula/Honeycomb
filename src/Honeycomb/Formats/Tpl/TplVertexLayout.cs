using System.Numerics;

namespace Honeycomb.Formats.Tpl;

public static class TplVertexLayout
{
    public const int TexCoordLayerCount = 6;

    private const TplVertexFormat Weights	= TplVertexFormat.Weight4 | TplVertexFormat.Weight8;
    private const TplVertexFormat Indices	= TplVertexFormat.Indices | TplVertexFormat.Indices16;

    private static readonly TplVertexFormat[] Parts =
    [
        TplVertexFormat.Vertex,
        Weights,
        Indices,
        TplVertexFormat.MaskingFlags,
        TplVertexFormat.Normal,
        TplVertexFormat.Tangent0,
        TplVertexFormat.Tangent1,
        TplVertexFormat.Tangent2,
        TplVertexFormat.Tangent3,
        TplVertexFormat.Tangent4,
        TplVertexFormat.Color0,
        TplVertexFormat.Color1,
        TplVertexFormat.Color2,
        TplVertexFormat.Color3,
        TplVertexFormat.Color4,
        TplVertexFormat.Color5,
        TplVertexFormat.BlendShapeInfo,
        TplVertexFormat.TexCoord0,
        TplVertexFormat.TexCoord1,
        TplVertexFormat.TexCoord2,
        TplVertexFormat.TexCoord3,
        TplVertexFormat.TexCoord4,
        TplVertexFormat.TexCoord5,
    ];

    public static int GetOffset(TplVertexFormat format, TplVertexFormat component)
    {
        int offset = 0;

        foreach (TplVertexFormat part in Parts)
        {
            if ((part & component) != 0)
            {
                return offset;
            }

            if ((format & part) != 0)
            {
                offset += GetSize(format, part);
            }
        }

        return offset;
    }

    public static int GetInfluenceCount(TplVertexFormat format) => format.HasFlag(TplVertexFormat.Weight8) ? 8 : 4;

    public static TplVertexFormat GetLayerFlag(TplVertexFormat first, int layer) => (TplVertexFormat)((ulong)first << layer);

    private static int GetSize(TplVertexFormat format, TplVertexFormat part) => part switch
    {
        TplVertexFormat.Vertex when format.HasFlag(TplVertexFormat.VertexCompressed)	=> 8,
        TplVertexFormat.Vertex when format.HasFlag(TplVertexFormat.NormalInVertex4)		=> 16,
        TplVertexFormat.Vertex															=> 12,
        Weights																			=> GetInfluenceCount(format),
        Indices when format.HasFlag(TplVertexFormat.Indices16)							=> GetInfluenceCount(format) * 2,
        Indices																			=> GetInfluenceCount(format),
        TplVertexFormat.MaskingFlags													=> 4,
        TplVertexFormat.Normal when format.HasFlag(TplVertexFormat.NormalInVertex4)		=> 0,
        TplVertexFormat.Normal when format.HasFlag(TplVertexFormat.NormalCompressed)	=> 4,
        TplVertexFormat.Normal															=> 12,
        TplVertexFormat.BlendShapeInfo													=> 16,
        >= TplVertexFormat.Tangent0 and <= TplVertexFormat.Tangent4						=> format.HasFlag(TplVertexFormat.TangentCompressed) ? 4 : 16,
        >= TplVertexFormat.Color0 and <= TplVertexFormat.Color5							=> 4,
        _																				=> GetTexCoordSize(format, part),
    };

    private static int GetTexCoordSize(TplVertexFormat format, TplVertexFormat part)
    {
        int layer		= BitOperations.Log2((ulong)part) - BitOperations.Log2((ulong)TplVertexFormat.TexCoord0);
        int dimensions	= format.HasFlag(GetLayerFlag(TplVertexFormat.TexCoord0Vector4, layer)) ? 2 : 1;

        if (!format.HasFlag(GetLayerFlag(TplVertexFormat.TexCoord0Compressed, layer)))
        {
            return 8 * dimensions;
        }

        if (dimensions == 2 && format.HasFlag(GetLayerFlag(TplVertexFormat.TexCoord0Vector4Byte, layer)))
        {
            return 4;
        }

        return 4 * dimensions;
    }
}
