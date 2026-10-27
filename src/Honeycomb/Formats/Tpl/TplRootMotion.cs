using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplRootMotion : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.Position += 12;
                break;
            case 1:
            case 3:
                reader.SkipSpline();
                break;
            case 2:
                reader.Position += 16;
                break;
            default:
                throw new InvalidDataException($"Unknown root motion field {index}.");
        }
    }
}
