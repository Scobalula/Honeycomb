using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplExternData : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        if (index != 0)
        {
            throw new InvalidDataException($"Unknown extern data field {index}.");
        }

        reader.ReadStructVector<TplExternDataEntry>();
    }
}
