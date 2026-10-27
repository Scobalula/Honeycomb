using System.Numerics;
using Honeycomb.Formats.Fio;

namespace Honeycomb.Formats.Tpl;

public sealed class TplObject : IFioStruct
{
    public const ushort NoObject = 0xFFFF;

    public ushort Id { get; private set; }

    public ushort Parent { get; private set; } = NoObject;

    public string Affixes { get; private set; } = string.Empty;

    public Matrix4x4? WorldMatrix { get; private set; }

    public Matrix4x4? LocalMatrix { get; private set; }

    public void ReadField(FioReader reader, int index)
    {
        switch (index)
        {
            case 0:
                Id = reader.Read<ushort>();
                break;
            case 1:
            case 8:
            case 12:
            case 16:
                reader.ReadString();
                break;
            case 2:
                reader.ReadFlags();
                break;
            case 3:
                Parent = reader.Read<ushort>();
                break;
            case >= 4 and <= 7:
                reader.Read<ushort>();
                break;
            case 9:
                WorldMatrix = reader.Read<Matrix4x4>();
                break;
            case 10:
                LocalMatrix = reader.Read<Matrix4x4>();
                break;
            case 11:
                reader.ReadStruct(new TplObjectGeometry());
                break;
            case 13:
                reader.Position += 60;
                break;
            case 14:
            case 15:
                reader.ReadShortString();
                break;
            case 17:
                Affixes = reader.ReadString();
                break;
            default:
                throw new InvalidDataException($"Unknown object field {index}.");
        }
    }
}
