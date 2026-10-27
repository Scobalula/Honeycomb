using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplBlendShapes : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.ReadVector(item => item.ReadString());
                break;
            case 1:
            case 2:
                reader.ReadVector(SkipSpline);
                break;
            default:
                throw new InvalidDataException($"Unknown blend shapes field {index}.");
        }
    }

    private static bool SkipSpline(FioReader reader)
    {
        reader.SkipSpline();
        return true;
    }
}
