using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Morpheme;

public sealed class MorphemeBundle : IFioStruct
{
    public const uint Signature = 0x42443353;

    public List<MorphemeBundleFile> Files { get; private set; } = [];

    public static MorphemeBundle Read(byte[] data)
    {
        MorphemeBundle	bundle	= new();
        FioReader		reader	= new(data, 0);

        reader.ReadStruct(bundle, Signature);

        int offset = reader.Position;

        foreach (MorphemeBundleFile file in bundle.Files)
        {
            file.Data	= new ArraySegment<byte>(data, offset, file.Size);
            offset		+= file.Size;
        }

        return bundle;
    }

    public void ReadField(FioReader reader, int index)
    {
        if (index != 0)
        {
            throw new InvalidDataException($"Unknown morpheme bundle field {index}.");
        }

        Files = reader.ReadStructVector<MorphemeBundleFile>();
    }
}
