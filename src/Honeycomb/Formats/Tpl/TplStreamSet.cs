using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplStreamSet : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        if (index != 0)
        {
            throw new InvalidDataException($"Unknown stream set field {index}.");
        }

        reader.ReadPrimitiveVector<int>();
    }
}
