using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplObjectGeometry : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
            case 1:
                reader.Read<int>();
                break;
            case 2:
                reader.Position += 24;
                break;
            case 3:
                reader.Position += 60;
                break;
            default:
                throw new InvalidDataException($"Unknown object geometry field {index}.");
        }
    }
}
