namespace Honeycomb.Formats.Tpl;

public sealed class TplStream
{
    public TplVertexFormat Format { get; set; }

    public int Stride { get; set; }

    public int Size { get; set; }

    public byte[]? Data { get; set; }
}
