using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplLodRoot : IFioStruct
{
    public int[] ObjectIds { get; private set; } = [];

    public int[] LodEnds { get; private set; } = [];

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                ObjectIds = reader.ReadPrimitiveVector<int>();
                break;
            case 1:
                LodEnds = reader.ReadPrimitiveVector<int>();
                break;
            case 2:
                reader.ReadStructVector<TplLodDistance>();
                break;
            case 3:
                reader.Position += 24;
                break;
            case 5:
                reader.Read<byte>();
                break;
            default:
                throw new InvalidDataException($"Unknown lod root field {index}.");
        }
    }
}
