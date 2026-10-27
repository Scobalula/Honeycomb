using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplSkin
{
    private const int InverseBindMatricesChunk = 0;

    public int BoneCount { get; private set; }

    public bool HasInverseBindMatrices { get; private set; }

    public static TplSkin Read(FioReader reader)
    {
        TplSkin skin = new()
        {
            BoneCount = reader.Read<int>(),
        };

        reader.ReadFlags();

        FioChunk chunk = reader.ReadChunk();

        while (!chunk.IsTerminator)
        {
            skin.HasInverseBindMatrices	|= chunk.Id == InverseBindMatricesChunk;
            reader.Position				= chunk.End;
            chunk						= reader.ReadChunk();
        }

        reader.Position = chunk.End;
        return skin;
    }
}
