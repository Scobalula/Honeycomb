using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplAnimationTrack : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                reader.ReadStructVector<TplAnimationSequence>();
                break;
            case 1:
                reader.ReadStructVector<TplObjectAnimation>();
                break;
            case 2:
                reader.ReadPrimitiveVector<short>();
                break;
            case 3:
                reader.ReadStruct(new TplRootMotion());
                break;
            default:
                throw new InvalidDataException($"Unknown animation track field {index}.");
        }
    }
}
