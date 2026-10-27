using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplPropertyStorage : IFioStruct
{
    private const byte FaceGenMeshType = 0;

    public void ReadField(FioReader reader, int index)
    {
        if (index != 0)
        {
            throw new InvalidDataException($"Unknown property storage field {index}.");
        }

        int count = reader.Read<short>();

        for (int i = 0; i < count; i++)
        {
            IFioStruct property = reader.Read<byte>() == FaceGenMeshType ? new TplFaceGenMesh() : new TplBlendShapes();

            reader.ReadStruct(property);
        }
    }
}
