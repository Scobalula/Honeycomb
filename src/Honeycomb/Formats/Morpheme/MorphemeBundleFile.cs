using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeBundleFile : IFioStruct
{
    public string Name { get; private set; } = string.Empty;

    public int Size { get; private set; }

    public ArraySegment<byte> Data { get; set; }

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                Name = reader.ReadString();
                break;
            case 1:
                Size = reader.Read<int>();
                break;
            default:
                throw new InvalidDataException($"Unknown morpheme bundle file field {index}.");
        }
    }
}
