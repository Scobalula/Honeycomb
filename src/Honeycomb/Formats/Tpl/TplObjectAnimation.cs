using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplObjectAnimation : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
            case 4:
                reader.Position += 12;
                break;
            case 1:
            case 3:
            case 5:
            case 7:
                reader.SkipSpline();
                break;
            case 2:
                reader.Position += 16;
                break;
            case 6:
                reader.Position += 4;
                break;
            default:
                throw new InvalidDataException($"Unknown object animation field {index}.");
        }
    }
}
