using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplSplitRange : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        if (index > 1)
        {
            throw new InvalidDataException($"Unknown split range field {index}.");
        }

        reader.Read<ushort>();
    }
}
