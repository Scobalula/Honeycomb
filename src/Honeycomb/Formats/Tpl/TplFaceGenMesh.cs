using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplFaceGenMesh : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.ReadStructVector<TplFaceGenSplit>();
                break;
            case >= 1 and <= 3:
                reader.ReadString();
                break;
            case 4:
                reader.ReadPrimitiveVector<short>();
                break;
            case 5:
                reader.Position += 64;
                break;
            default:
                throw new InvalidDataException($"Unknown face gen mesh field {index}.");
        }
    }
}
