using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplStreamReference : IFioStruct
{
    public long Offset { get; private set; }

    public long Size { get; private set; }

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                Offset = reader.Read<long>();
                break;
            case 1:
                Size = reader.Read<long>();
                break;
            default:
                throw new InvalidDataException($"Unknown stream reference field {index}.");
        }
    }
}
