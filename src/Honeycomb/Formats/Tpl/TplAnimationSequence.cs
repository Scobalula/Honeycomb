using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplAnimationSequence : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.ReadString();
                break;
            case >= 1 and <= 6:
                reader.Read<int>();
                break;
            case 7:
                reader.ReadStructVector<TplAnimationEvent>();
                break;
            case 8:
                reader.Position += 24;
                break;
            default:
                throw new InvalidDataException($"Unknown animation sequence field {index}.");
        }
    }
}
