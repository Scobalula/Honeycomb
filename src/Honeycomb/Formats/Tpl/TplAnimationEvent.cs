using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplAnimationEvent : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.Read<int>();
                break;
            case 1:
                reader.ReadString();
                break;
            default:
                throw new InvalidDataException($"Unknown animation event field {index}.");
        }
    }
}
