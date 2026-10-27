using System.Buffers.Binary;
using System.Text;

namespace Honeycomb.Formats.Morpheme;

public static class MorphemeStringTable
{
    public static string[] Read(ReadOnlySpan<byte> data, int offset)
    {
        int					count	= BinaryPrimitives.ReadInt32LittleEndian(data[offset..]);
        ReadOnlySpan<byte>	offsets	= data[(offset + (int)BinaryPrimitives.ReadInt64LittleEndian(data[(offset + 8)..]))..];
        ReadOnlySpan<byte>	strings	= data[(offset + (int)BinaryPrimitives.ReadInt64LittleEndian(data[(offset + 16)..]))..];
        string[]			values	= new string[count];

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> value = strings[BinaryPrimitives.ReadInt32LittleEndian(offsets[(i * 4)..])..];

            values[i] = Encoding.UTF8.GetString(value[..value.IndexOf((byte)0)]);
        }

        return values;
    }
}
