using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplDescriptor : IFioStruct
{
    private const int ResourceHeaderSize		= 0x40;
    private const int DependenciesSizeOffset	= 0x3C;

    public List<string?> Files { get; private set; } = [];

    public static bool IsDescriptor(byte[] data) => data.AsSpan().StartsWith("1SERdsc"u8);

    public static TplDescriptor Read(byte[] data)
    {
        TplDescriptor	descriptor	= new();
        FioReader		reader		= new(data, ResourceHeaderSize + BitConverter.ToInt32(data, DependenciesSizeOffset));

        reader.ReadStruct(descriptor);
        return descriptor;
    }

    public void ReadField(FioReader reader, int index)
    {
        if (index != 0)
        {
            throw new InvalidDataException($"Unknown descriptor field {index}.");
        }

        Files = reader.ReadVector(item => item.ReadString());
    }
}
