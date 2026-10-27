using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplLodDistance : IFioStruct
{
    public void ReadField(FioReader reader, int index)
    {
        if (index != 0)
        {
            throw new InvalidDataException($"Unknown lod distance field {index}.");
        }

        reader.Read<float>();
    }
}
