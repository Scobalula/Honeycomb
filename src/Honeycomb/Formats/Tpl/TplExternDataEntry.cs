using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplExternDataEntry : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.ReadString();
                break;
            case 1:
                reader.ReadPrimitiveVector<byte>();
                break;
            default:
                throw new InvalidDataException($"Unknown extern data entry field {index}.");
        }
    }
}
