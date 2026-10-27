using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplLodDefinition : IFioStruct
{
    public ushort ObjectId { get; private set; }

    public int Lod { get; private set; }

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                ObjectId = reader.Read<ushort>();
                break;
            case 1:
                Lod = reader.Read<byte>();
                break;
            case 2:
                reader.Read<byte>();
                break;
            default:
                throw new InvalidDataException($"Unknown lod definition field {index}.");
        }
    }
}
