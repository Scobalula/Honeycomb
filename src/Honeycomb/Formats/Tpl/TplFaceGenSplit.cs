using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplFaceGenSplit : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.ReadString();
                break;
            case 1:
                reader.ReadPrimitiveVector<short>();
                break;
            case 2:
                reader.Position += 64;
                break;
            default:
                throw new InvalidDataException($"Unknown face gen split field {index}.");
        }
    }
}
