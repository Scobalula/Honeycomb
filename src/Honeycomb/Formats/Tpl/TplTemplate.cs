using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplTemplate : IFioStruct
{
    public const uint Signature = 0x314C5054;

    private const int ResourceHeaderSize		= 0x40;
    private const int DependenciesSizeOffset	= 0x3C;

    public TplSkin? Skin { get; private set; }

    public List<TplLodDefinition> LodDefinitions { get; private set; } = [];

    public TplGeometry Geometry { get; } = new();

    public static TplTemplate Read(byte[] data)
    {
        TplTemplate	template	= new();
        FioReader	reader		= new(data, ResourceHeaderSize + BitConverter.ToInt32(data, DependenciesSizeOffset));

        reader.ReadStruct(template, Signature);
        return template;
    }

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
            case 1:
            case 3:
            case 4:
                reader.ReadString();
                break;
            case 2:
                reader.ReadFlags();
                break;
            case 5:
                Skin = TplSkin.Read(reader);
                break;
            case 6:
                reader.ReadStruct(new TplAnimationTrack());
                break;
            case 8:
                reader.Position += 24;
                break;
            case 9:
                LodDefinitions = reader.ReadStructVector<TplLodDefinition>();
                break;
            case 10:
                reader.Read<uint>();
                reader.SkipChunks();
                break;
            case 11:
                Geometry.ReadChunks(reader, reader.ReadStruct(Geometry, TplGeometry.Signature));
                break;
            case 12:
                reader.ReadStruct(new TplExternData());
                break;
            default:
                throw new InvalidDataException($"Unsupported template field {index}.");
        }
    }
}
