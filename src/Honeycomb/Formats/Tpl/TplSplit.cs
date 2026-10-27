using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplSplit
{
    public int StartVertex { get; set; }

    public int VertexCount { get; set; }

    public int StartFace { get; set; }

    public int FaceCount { get; set; }

    public ushort SkinCompoundId { get; set; } = TplObject.NoObject;

    public int GeomIndex { get; set; }

    public int ObjectIndex { get; set; }

    public Vector3 CompressionOffset { get; set; }

    public Vector3 CompressionScale { get; set; } = Vector3.One;

    public ushort[] TexCoordMaxTiles { get; } = new ushort[TplVertexLayout.TexCoordLayerCount];

    public ushort[] BonePalette { get; set; } = [];

    public FioPropertySection? Material { get; set; }
}
